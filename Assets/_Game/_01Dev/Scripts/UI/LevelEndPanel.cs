using BusJam.Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;

namespace BusJam.UI
{
    public class LevelEndPanel : UIPanel
    {
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private LevelManager _levelManager;
        private GameStateManager _gameStateManager;
        private bool _isSuccess;

        [Inject]
        public void Construct(LevelManager levelManager, GameStateManager gameStateManager)
        {
            _levelManager = levelManager;
            _gameStateManager = gameStateManager;
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextButtonClicked);
            
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartButtonClicked);
            
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuButtonClicked);
        }

        public void SetSuccess(bool success)
        {
            _isSuccess = success;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (titleText != null)
            {
                titleText.text = _isSuccess ? "Level Complete!" : "Level Failed";
            }

            if (nextButton != null)
            {
                var hasNextLevel = _levelManager != null && _levelManager.HasNextLevel;
                nextButton.gameObject.SetActive(_isSuccess && hasNextLevel);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(!_isSuccess);
            }
        }

        private void OnNextButtonClicked()
        {
            if (_levelManager != null && _levelManager.LoadNextLevel())
            {
                if (_gameStateManager != null)
                {
                    _gameStateManager.StartGame();
                }
            }
        }

        private void OnRestartButtonClicked()
        {
            if (_gameStateManager != null)
            {
                _gameStateManager.RestartGame();
            }
        }

        private void OnMenuButtonClicked()
        {
            if (_gameStateManager != null)
            {
                _gameStateManager.GoToMainMenu();
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            UpdateUI();
        }
    }
}