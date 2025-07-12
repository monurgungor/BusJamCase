using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class WinConditionManager : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private bool _levelCompleted;
        private bool _levelFailed;

        public void Initialize()
        {
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus.Subscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus.Subscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
            _signalBus.Subscribe<TimerExpiredSignal>(OnTimerExpired);
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            ResetWinConditions();
            Debug.Log("[WIN CONDITION] Level started, monitoring win conditions");
        }

        private void ResetWinConditions()
        {
            _levelCompleted = false;
            _levelFailed = false;
        }

        private void OnAllBusesCompleted()
        {
            if (_levelCompleted || _levelFailed) return;

            Debug.Log("[WIN CONDITION] All buses completed - Level won!");
            _levelCompleted = true;
            _signalBus.Fire<LevelCompletedSignal>();
        }

        private void OnAllPassengersRemoved()
        {
            if (_levelCompleted || _levelFailed) return;

            Debug.Log("[WIN CONDITION] All passengers removed - Level won!");
            _levelCompleted = true;
            _signalBus.Fire<LevelCompletedSignal>();
        }

        private void OnTimerExpired()
        {
            if (_levelCompleted || _levelFailed) return;

            Debug.Log("[WIN CONDITION] Timer expired - Level failed!");
            _levelFailed = true;
            _signalBus.Fire<LevelFailedSignal>();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus?.TryUnsubscribe<AllBusesCompletedSignal>(OnAllBusesCompleted);
            _signalBus?.TryUnsubscribe<AllPassengersRemovedSignal>(OnAllPassengersRemoved);
            _signalBus?.TryUnsubscribe<TimerExpiredSignal>(OnTimerExpired);
        }
    }
}