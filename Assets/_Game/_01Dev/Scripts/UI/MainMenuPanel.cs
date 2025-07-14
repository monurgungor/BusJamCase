using BusJam.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace BusJam.UI
{
    public class MainMenuPanel : UIPanel
    {
        [SerializeField] private Transform levelButtonContainer;
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private Button levelButtonPrefab;

        private LevelManager _levelManager;
        private GameStateManager _gameStateManager;
        
        [Inject]
        public void Construct(LevelManager levelManager, GameStateManager gameStateManager)
        {
            _levelManager = levelManager;
            _gameStateManager = gameStateManager;
        }

        protected override void OnShow()
        {
            base.OnShow();
            if (levelButtonContainer != null && levelButtonContainer.childCount == 0)
            {
                CreateLevelButtons();
            }
        }

        private void Start()
        {
            if (gameObject.activeInHierarchy && _levelManager != null)
            {
                CreateLevelButtons();
            }
        }

        private void CreateLevelButtons()
        {
            if (_levelManager == null || levelButtonContainer == null || levelButtonPrefab == null)
                return;

            ClearExistingButtons();

            for (var i = 0; i < _levelManager.TotalLevelsCount; i++)
            {
                CreateLevelButton(i);
            }

            PositionButtonsVertically();
        }
        
        private void ClearExistingButtons()
        {
            foreach (Transform child in levelButtonContainer)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void CreateLevelButton(int levelIndex)
        {
            var levelData = _levelManager.GetLevel(levelIndex);
            if (levelData == null) 
            {
                return;
            }
            
            var button = Instantiate(levelButtonPrefab, levelButtonContainer);
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                var levelText = $"Level {levelIndex + 1}";
                
                buttonText.text = levelText;
            }

            button.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
        }

        private void PositionButtonsVertically()
        {
            if (levelButtonContainer == null || levelButtonContainer.childCount == 0)
                return;

            var buttonCount = levelButtonContainer.childCount;
            var containerHeight = containerRect.rect.height;
            var stepSize = containerHeight / (buttonCount + 1);
            var startY = containerHeight * 0.5f;

            for (var i = 0; i < buttonCount; i++)
            {
                var button = levelButtonContainer.GetChild(i);
                var rectTransform = button.GetComponent<RectTransform>();

                if (rectTransform == null) continue;
                var position = rectTransform.localPosition;
                position.y = startY - (i + 1) * stepSize;
                rectTransform.localPosition = position;
            }
        }

        private void OnLevelButtonClicked(int levelIndex)
        {
            if (_gameStateManager != null)
            {
                _gameStateManager.LoadAndStartLevel(levelIndex);
            }
        }
    }
}