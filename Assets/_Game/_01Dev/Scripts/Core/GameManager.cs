using BusJam.Data;
using BusJam.Events;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class GameManager : MonoBehaviour
    {
        private SignalBus _signalBus;
        private GameStateManager _gameStateManager;
        private LevelManager _levelManager;

        private static GameManager Instance { get; set; }
        private LevelData CurrentLevelData { get; set; }

        private GameConfig GameConfig { get; set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (_signalBus != null && GameConfig != null)
            {
                SubscribeToEvents();
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            DOTween.Kill(gameObject);

            if (Instance == this) Instance = null;

            UnsubscribeFromEvents();
        }

        [Inject]
        public void Construct(
            SignalBus signalBus,
            GameConfig gameConfig,
            GameStateManager gameStateManager,
            LevelManager levelManager)
        {
            this._signalBus = signalBus;
            GameConfig = gameConfig;
            _gameStateManager = gameStateManager;
            _levelManager = levelManager;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
            _signalBus.Subscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus.Subscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
            _signalBus.Subscribe<LevelChangedSignal>(OnLevelChanged);
            _signalBus.Subscribe<LevelRestartedSignal>(OnLevelRestarted);
        }

        public void LoadLevel(LevelData levelData)
        {
            CurrentLevelData = levelData;
            _signalBus.Fire(new LevelLoadedSignal(levelData));
        }

        public void StartLevel()
        {
            if (_levelManager != null && _levelManager.CurrentLevel != null)
            {
                CurrentLevelData = _levelManager.CurrentLevel;
                Debug.Log($"[GAME MANAGER] Starting level: {CurrentLevelData.name} with {CurrentLevelData.GetTotalPassengerCount()} passengers");
                _signalBus.Fire(new LevelStartedSignal(CurrentLevelData));
            }
            else
            {
                Debug.LogError("[GAME MANAGER] Cannot start level - no current level set in LevelManager");
            }
        }

        public void PauseGame()
        {
            _signalBus.Fire<GamePausedSignal>();
        }

        public void ResumeGame()
        {
            _signalBus.Fire<GameResumedSignal>();
        }

        public void RestartLevel()
        {
            if (_levelManager != null)
            {
                _levelManager.RestartCurrentLevel();
            }
        }

        private void OnLevelCompleted()
        {
            Debug.Log("[GAME MANAGER] Level completed, transitioning to level complete state");
        }

        private void OnLevelFailed()
        {
            Debug.Log("[GAME MANAGER] Level failed, transitioning to level failed state");
        }

        private void OnAllBusesCompleted()
        {
            _signalBus.Fire<LevelCompletedSignal>();
        }

        private void OnAllPassengersRemoved()
        {
            _signalBus.Fire<LevelCompletedSignal>();
        }

        private void OnLevelChanged(LevelChangedSignal signal)
        {
            CurrentLevelData = signal.NewLevelData;
            LoadLevel(CurrentLevelData);
            StartLevel();
        }

        private void OnLevelRestarted(LevelRestartedSignal signal)
        {
            CurrentLevelData = signal.LevelData;
            LoadLevel(CurrentLevelData);
            StartLevel();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus?.TryUnsubscribe<LevelFailedSignal>(OnLevelFailed);
            _signalBus?.TryUnsubscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus?.TryUnsubscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
            _signalBus?.TryUnsubscribe<LevelChangedSignal>(OnLevelChanged);
            _signalBus?.TryUnsubscribe<LevelRestartedSignal>(OnLevelRestarted);
        }
    }
}