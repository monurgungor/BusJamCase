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

            for (int i = 0; i < _levelManager.TotalLevelsCount; i++)
            {
                CreateLevelButton(i);
            }
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
            if (levelData == null) return;

            var button = Instantiate(levelButtonPrefab, levelButtonContainer);
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = $"Level {levelIndex + 1}";
            }

            button.onClick.AddListener(() => OnLevelButtonClicked(levelIndex));
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