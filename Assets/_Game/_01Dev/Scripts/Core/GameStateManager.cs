using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class GameStateManager : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private GameManager _gameManager;
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
                    StartGame();
                }
                else if (_currentState == GameState.LevelComplete || _currentState == GameState.LevelFailed)
                {
                    RestartGame();
                }
            }
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameManager gameManager)
        {
            _signalBus = signalBus;
            _gameManager = gameManager;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
        }

        public void StartGame()
        {
            Debug.Log("[GAME STATE] Starting game from main menu");
            if (_gameManager != null)
            {
                _gameManager.StartLevel();
            }
        }

        public void RestartGame()
        {
            Debug.Log("[GAME STATE] Restarting game");
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