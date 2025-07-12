using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class GameStateManager : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private GameManager _gameManager;
        private LevelManager _levelManager;
        private GameState _currentState = GameState.MainMenu;

        public GameState CurrentState => _currentState;

        public void Initialize()
        {
            SubscribeToEvents();
            ChangeState(GameState.MainMenu);
        }


        [Inject]
        public void Construct(SignalBus signalBus, GameManager gameManager, LevelManager levelManager)
        {
            _signalBus = signalBus;
            _gameManager = gameManager;
            _levelManager = levelManager;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
            
            _signalBus.Subscribe<LoadLevelRequestedSignal>(OnLoadLevelRequested);
            _signalBus.Subscribe<RestartLevelRequestedSignal>(OnRestartLevelRequested);
        }

        private void OnRestartLevelRequested()
        {
            RestartGame();
        }

        private void OnLoadLevelRequested(LoadLevelRequestedSignal signal)
        {
            if (_levelManager.TotalLevelsCount > signal.LevelIndex)
            {
                LoadAndStartLevel(signal.LevelIndex);
            }
            else
            {
                Debug.LogWarning($"[GAME STATE] Level {signal.LevelIndex} does not exist");
            }
        }

        public void StartGame()
        {
            if (_gameManager != null)
            {
                _gameManager.StartLevel();
            }
        }

        public void LoadAndStartLevel(int levelIndex)
        {
            if (_levelManager != null)
            {
                _levelManager.LoadLevel(levelIndex);
            }
        }

        public void RestartGame()
        {
            if (_gameManager != null)
            {
                _gameManager.RestartLevel();
            }
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            var previousState = _currentState;
            _currentState = newState;

            _signalBus.Fire(new GameStateChangedSignal(previousState, newState));

            switch (newState)
            {
                case GameState.MainMenu:
                    _signalBus.Fire<EnterMainMenuSignal>();
                    break;
                case GameState.LevelSelect:
                    _signalBus.Fire<EnterLevelSelectSignal>();
                    break;
                case GameState.Playing:
                    _signalBus.Fire<EnterPlayingSignal>();
                    break;
                case GameState.LevelComplete:
                    _signalBus.Fire<EnterLevelCompleteSignal>();
                    break;
                case GameState.LevelFailed:
                    _signalBus.Fire<EnterLevelFailedSignal>();
                    break;
            }
        }

        public void GoToMainMenu()
        {
            ChangeState(GameState.MainMenu);
        }

        public void GoToLevelSelect()
        {
            ChangeState(GameState.LevelSelect);
        }

        public void StartPlaying()
        {
            ChangeState(GameState.Playing);
        }

        public void CompleteLevel()
        {
            ChangeState(GameState.LevelComplete);
        }

        public void FailLevel()
        {
            ChangeState(GameState.LevelFailed);
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            StartPlaying();
        }

        private void OnLevelCompleted()
        {
            CompleteLevel();
        }

        private void OnLevelFailed()
        {
            FailLevel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus?.TryUnsubscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus?.TryUnsubscribe<LevelFailedSignal>(OnLevelFailed);
            
            _signalBus?.TryUnsubscribe<LoadLevelRequestedSignal>(OnLoadLevelRequested);
            _signalBus?.TryUnsubscribe<RestartLevelRequestedSignal>(OnRestartLevelRequested);
        }
    }
}