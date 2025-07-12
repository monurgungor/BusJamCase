using BusJam.Core;
using BusJam.Events;
using BusJam.MVC.Models;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class BusView : MonoBehaviour
    {
        [SerializeField] private Renderer busRenderer;
        [SerializeField] private Transform[] passengerSeats;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        private GameConfig gameConfig;
        private BusModel model;

        private SignalBus signalBus;

        private void Awake()
        {
            if (busRenderer == null)
                busRenderer = GetComponent<Renderer>();

            if (busRenderer == null)
                busRenderer = GetComponentInChildren<Renderer>();

            if (loadingIndicator != null) loadingIndicator.SetActive(false);
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

        public void Initialize(BusModel busModel)
        {
            model = busModel;

            if (passengerSeats == null || passengerSeats.Length == 0) CreateDefaultSeats();

            SetupColor();

            var startPosition = model.ArrivalPosition + Vector3.back * 10f;
            transform.position = startPosition;

            ApproachStation(gameConfig.BusArrivalSpeed);
        }

        private void CreateDefaultSeats()
        {
            passengerSeats = new Transform[model.Capacity];
            for (var i = 0; i < model.Capacity; i++)
            {
                var seat = new GameObject($"Seat_{i}");
                seat.transform.SetParent(transform);

                var xOffset = i % 2 == 0 ? -0.5f : 0.5f;
                var zOffset = Mathf.Floor(i / 2f) * 0.8f;
                seat.transform.localPosition = new Vector3(xOffset, 0.5f, zOffset);

                passengerSeats[i] = seat.transform;
            }
        }

        private void SetupColor()
        {
            if (busRenderer != null && gameConfig != null)
            {
                var busColor = gameConfig.GetBusColor(model.BusColor);
                busRenderer.material.color = busColor;
            }
        }

        private void ApproachStation(float arrivalSpeed)
        {
            var distance = Vector3.Distance(transform.position, model.ArrivalPosition);
            var duration = distance / arrivalSpeed;

            transform.DOMove(model.ArrivalPosition, duration)
                .SetEase(moveCurve)
                .OnComplete(() =>
                {
                    model.SetState(BusState.Loading);
                    StartLoading();
                    signalBus.Fire(new BusArrivedSignal(gameObject, model.BusColor));
                });
        }

        private void StartLoading()
        {
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
        }

        public bool BoardPassenger(GameObject passengerView)
        {
            if (!model.IsInteractable || model.IsFull)
                return false;

            if (model.AddPassenger(passengerView))
            {
                passengerView.transform.SetParent(transform);
                
                if (model.CurrentPassengerCount <= passengerSeats.Length)
                {
                    var seat = passengerSeats[model.CurrentPassengerCount - 1];
                    passengerView.transform.DOMove(seat.position, 0.5f)
                        .SetEase(Ease.OutCubic)
                        .OnComplete(() =>
                        {
                            var renderer = passengerView.GetComponent<Renderer>();
                            if (renderer != null) renderer.enabled = false;
                        });
                }
                else
                {
                    var renderer = passengerView.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                }

                passengerView.GetComponent<PassengerView>()?.PlayPickupAnimation();

                if (model.IsFull) StartDeparture();

                return true;
            }

            return false;
        }

        public void StartDeparture()
        {
            if (model.State != BusState.Loading)
                return;

            model.SetState(BusState.Departing);

            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            signalBus.Fire(new BusLoadedSignal(gameObject, model.BusColor, model.CurrentPassengerCount));

            var distance = Vector3.Distance(transform.position, model.DeparturePosition);
            var duration = distance / gameConfig.BusDepartureSpeed;

            transform.DOMove(model.DeparturePosition, duration)
                .SetEase(moveCurve)
                .OnComplete(() =>
                {
                    model.SetState(BusState.Gone);
                    signalBus.Fire(new BusDepartedSignal(gameObject, model.BusColor));
                });
        }

        public BusModel GetModel()
        {
            return model;
        }
    }
}