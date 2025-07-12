using BusJam.Data;
using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class LevelTimer : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private GameStateManager _gameStateManager;
        
        private float _remainingTime;
        private float _totalTime;
        private bool _isRunning;
        private bool _isPaused;

        public float RemainingTime => _remainingTime;
        public float TotalTime => _totalTime;
        public float ElapsedTime => _totalTime - _remainingTime;
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;

        public void Initialize()
        {
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameStateManager gameStateManager)
        {
            _signalBus = signalBus;
            _gameStateManager = gameStateManager;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_signalBus != null)
            {
                _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
                _signalBus.Subscribe<LevelCompletedSignal>(OnLevelCompleted);
                _signalBus.Subscribe<LevelFailedSignal>(OnLevelFailed);
                _signalBus.Subscribe<GamePausedSignal>(OnGamePaused);
                _signalBus.Subscribe<GameResumedSignal>(OnGameResumed);
                _signalBus.Subscribe<LevelChangedSignal>(OnLevelChanged);
            }
        }

        private void Update()
        {
            if (!_isRunning || _isPaused) return;

            _remainingTime -= Time.deltaTime;

            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                TimerExpired();
            }

            if (_signalBus != null)
            {
                _signalBus.Fire(new TimerUpdatedSignal(_remainingTime, _totalTime, ElapsedTime));
            }
        }

        public void StartTimer(float timeLimit)
        {
            UnityEngine.Debug.Assert(timeLimit > 0f, "[LEVEL TIMER] Time limit must be greater than 0");
            UnityEngine.Debug.Assert(_signalBus != null, "[LEVEL TIMER] SignalBus is null");
            
            _totalTime = timeLimit;
            _remainingTime = timeLimit;
            _isRunning = true;
            _isPaused = false;

            Debug.Log($"[LEVEL TIMER] Timer started - Total: {_totalTime}s, Running: {_isRunning}");
            _signalBus.Fire(new TimerStartedSignal(_totalTime));
        }

        public void StopTimer()
        {
            _isRunning = false;
            _isPaused = false;
            _remainingTime = 0f;

            _signalBus.Fire<TimerStoppedSignal>();
        }

        public void PauseTimer()
        {
            if (!_isRunning) return;

            _isPaused = true;
            _signalBus.Fire<TimerPausedSignal>();
        }

        public void ResumeTimer()
        {
            if (!_isRunning) return;

            _isPaused = false;
            _signalBus.Fire<TimerResumedSignal>();
        }

        public void SetRemainingTime(float time)
        {
            UnityEngine.Debug.Assert(time >= 0f, "[LEVEL TIMER] Remaining time cannot be negative");
            
            _remainingTime = Mathf.Max(0f, time);
            _signalBus.Fire(new TimerUpdatedSignal(_remainingTime, _totalTime, ElapsedTime));
        }

        private void TimerExpired()
        {
            _isRunning = false;
            _signalBus.Fire<TimerExpiredSignal>();
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            if (signal.LevelData != null && signal.LevelData.timeLimit > 0f)
            {
                StartTimer(signal.LevelData.timeLimit);
            }
        }

        private void OnLevelCompleted()
        {
            StopTimer();
        }

        private void OnLevelFailed()
        {
            StopTimer();
        }

        private void OnGamePaused()
        {
            PauseTimer();
        }

        private void OnGameResumed()
        {
            ResumeTimer();
        }

        private void OnLevelChanged(LevelChangedSignal signal)
        {
            StopTimer();
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
            _signalBus?.TryUnsubscribe<GamePausedSignal>(OnGamePaused);
            _signalBus?.TryUnsubscribe<GameResumedSignal>(OnGameResumed);
            _signalBus?.TryUnsubscribe<LevelChangedSignal>(OnLevelChanged);
        }
    }
}