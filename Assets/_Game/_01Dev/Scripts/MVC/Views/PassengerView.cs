using BusJam.Core;
using BusJam.Events;
using BusJam.MVC.Models;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class PassengerView : MonoBehaviour
    {
        [SerializeField] private Renderer passengerRenderer;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private GameConfig gameConfig;
        private PassengerModel model;

        private SignalBus signalBus;

        private void Awake()
        {
            if (passengerRenderer == null)
                passengerRenderer = GetComponent<Renderer>();

            if (passengerRenderer == null)
                passengerRenderer = GetComponentInChildren<Renderer>();
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }


        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig)
        {
            this.signalBus = signalBus;
            this.gameConfig = gameConfig;
        }

        public void Initialize(PassengerModel passengerModel)
        {
            model = passengerModel;
            SetupMaterialAndColor();
            name = $"Passenger_{model.Color}_{model.GridPosition.x}_{model.GridPosition.y}";
        }

        private void SetupMaterialAndColor()
        {
            if (passengerRenderer != null && gameConfig != null)
            {
                var passengerColor = gameConfig.GetPassengerColor(model.Color);
                passengerRenderer.material.color = passengerColor;
            }
        }

        public void MoveTo(Vector3 destination)
        {
            var previousState = model.State;
            model.SetState(PassengerState.Moving);

            var distance = Vector3.Distance(transform.position, destination);
            var duration = distance / 3f;

            transform.DOMove(destination, duration)
                .SetEase(moveCurve)
                .OnComplete(() =>
                {
                    if (previousState != PassengerState.Moving) model.SetState(previousState);
                });
        }

        public void SetSelected(bool selected)
        {
            model.SetSelected(selected);

            if (selected)
                PlaySelectionAnimation();
            else
                StopSelectionAnimation();
        }

        public void SetDragging(bool dragging)
        {
            model.SetDragging(dragging);
        }

        private void PlaySelectionAnimation()
        {
            transform.DOScale(1.2f, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopSelectionAnimation()
        {
            transform.DOKill();
            transform.DOScale(1f, 0.1f);
        }

        public void PlayPickupAnimation()
        {
            transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }

        public PassengerModel GetModel()
        {
            return model;
        }
    }
}