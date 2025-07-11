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

                if (CurrentLevelData != null)
                {
                    LoadLevel(CurrentLevelData);
                }
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
            LevelData levelData,
            GameStateManager gameStateManager)
        {
            this._signalBus = signalBus;
            GameConfig = gameConfig;
            CurrentLevelData = levelData;
            _gameStateManager = gameStateManager;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
            _signalBus.Subscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus.Subscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
        }

        public void LoadLevel(LevelData levelData)
        {
            CurrentLevelData = levelData;
            _signalBus.Fire(new LevelLoadedSignal(levelData));
        }

        public void StartLevel()
        {
            if (CurrentLevelData != null) 
            {
                Debug.Log($"[GAME MANAGER] Starting level: {CurrentLevelData.name}");
                _signalBus.Fire(new LevelStartedSignal(CurrentLevelData));
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
            if (CurrentLevelData != null)
            {
                LoadLevel(CurrentLevelData);
                StartLevel();
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

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelCompletedSignal>(OnLevelCompleted);
            _signalBus?.TryUnsubscribe<LevelFailedSignal>(OnLevelFailed);
            _signalBus?.TryUnsubscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus?.TryUnsubscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
        }
    }
}