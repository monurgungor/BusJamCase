using BusJam.Core;
using BusJam.Events;
using UnityEngine;
using TMPro;
using Zenject;

namespace BusJam.UI
{
    public class GameUIPanel : UIPanel
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI levelNameText;

        private SignalBus _signalBus;

        [Inject]
        public void Construct(SignalBus signalBus, GameManager gameManager)
        {
            _signalBus = signalBus;
            SubscribeToEvents();
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();
        }

        private void SubscribeToEvents()
        {
            if (_signalBus != null)
            {
                _signalBus.Subscribe<TimerUpdatedSignal>(OnTimerUpdated);
                _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            }
        }

        private void OnTimerUpdated(TimerUpdatedSignal signal)
        {
            if (timerText != null)
            {
                UpdateTimerDisplay(signal.RemainingTime);
            }
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            if (levelNameText != null && signal.LevelData != null)
            {
                levelNameText.text = signal.LevelData.name;
            }
            if (timerText != null && signal.LevelData != null)
            {
                UpdateTimerDisplay(signal.LevelData.timeLimit);
            }
        }

        private void UpdateTimerDisplay(float remainingTime)
        {
            var minutes = Mathf.FloorToInt(remainingTime / 60);
            var seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = $"{minutes}:{seconds:00}";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<TimerUpdatedSignal>(OnTimerUpdated);
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
        }
    }
}