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

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_currentState == GameState.MainMenu)
                {
                    LoadAndStartLevel(0);
                }
                else if (_currentState == GameState.LevelComplete || _currentState == GameState.LevelFailed)
                {
                    RestartGame();
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_currentState == GameState.Playing)
                {
                    if (_gameManager != null)
                    {
                        _gameManager.PauseGame();
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_currentState == GameState.Playing)
                {
                    if (_gameManager != null)
                    {
                        _gameManager.ResumeGame();
                    }
                }
            }

            if (_currentState == GameState.MainMenu)
            {
                HandleLevelSelection();
            }

            if (_currentState == GameState.LevelComplete)
            {
                HandlePostLevelInput();
            }
        }

        private void HandleLevelSelection()
        {
            if (_levelManager == null) return;

            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    int levelIndex = i == 0 ? 9 : i - 1;
                    if (levelIndex < _levelManager.TotalLevelsCount)
                    {
                        LoadAndStartLevel(levelIndex);
                    }
                    else
                    {
                        Debug.LogWarning($"[GAME STATE] Level {levelIndex} does not exist");
                    }
                    break;
                }
            }
        }

        private void HandlePostLevelInput()
        {
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (_levelManager != null && _levelManager.HasNextLevel)
                {
                    _levelManager.LoadNextLevel();
                    StartGame();
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                GoToMainMenu();
            }
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
        }
    }
}