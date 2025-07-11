using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class BusController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform busParent;
        [SerializeField] private Transform busArrivalPoint;
        [SerializeField] private Transform busDeparturePoint;
        [SerializeField] private GameObject busPrefab;
        private readonly List<BusView> _activeBuses = new();
        private readonly Queue<BusData> _busSequence = new();
        private BenchController _benchController;
        private DiContainer _container;
        private GameConfig _gameConfig;

        private SignalBus _signalBus;

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearAllBuses();
        }

        public void Initialize()
        {
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig, DiContainer container,
            BenchController benchController)
        {
            this._signalBus = signalBus;
            this._gameConfig = gameConfig;
            this._container = container;
            this._benchController = benchController;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus.Subscribe<BusArrivedSignal>(OnBusArrived);
            _signalBus.Subscribe<BusDepartedSignal>(OnBusDeparted);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            SetupBusSequence(signal.LevelData.GetBusSequence());
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            StartBusSequence();
        }

        private void OnBusArrived(BusArrivedSignal signal)
        {
            var busView = signal.BusView.GetComponent<BusView>();
            if (busView == null) return;

            busView.GetModel();

            AutoLoadPassengersFromQueue(busView);
        }

        private void AutoLoadPassengersFromQueue(BusView busView)
        {
            var busModel = busView.GetModel();

            var passengerGo = _benchController.RemovePassengerOfColorFromQueue(busModel.BusColor);
            while (!busModel.IsFull && _benchController.GetPassengerCountOfColor(busModel.BusColor) > 0)
            {
                if (passengerGo == null) break;

                var passengerView = passengerGo.GetComponent<PassengerView>();
                if (passengerView == null) continue;

                if (busView.BoardPassenger(passengerGo))
                {
                    passengerView.GetModel().SetState(PassengerState.OnBus);
                }
                else
                {
                    _benchController.AddPassengerToQueue(passengerGo);
                    break;
                }
            }
        }

        public BusView GetLoadingBusOfColor(PassengerColor color)
        {
            foreach (var busView in _activeBuses)
            {
                var model = busView.GetModel();
                if (model.BusColor == color && model.State == BusState.Loading && !model.IsFull) return busView;
            }

            return null;
        }

        public BusView GetApproachingBusOfColor(PassengerColor color)
        {
            foreach (var busView in _activeBuses)
            {
                var model = busView.GetModel();
                if (model.BusColor == color && model.State == BusState.Approaching) return busView;
            }

            return null;
        }

        private void OnBusDeparted(BusDepartedSignal signal)
        {
            var busView = signal.BusView.GetComponent<BusView>();
            if (busView != null) _activeBuses.Remove(busView);

            SpawnNextBus();

            if (_busSequence.Count == 0 && _activeBuses.Count == 0) _signalBus.Fire<AllBusesCompletedSignal>();
        }

        private void SetupBusSequence(List<BusData> busDataList)
        {
            _busSequence.Clear();
            ClearAllBuses();

            foreach (var busData in busDataList) _busSequence.Enqueue(busData);
        }

        private void StartBusSequence()
        {
            SpawnNextBus();
        }

        private void SpawnNextBus()
        {
            if (_busSequence.Count > 0)
            {
                var busData = _busSequence.Dequeue();
                SpawnBus(busData);
            }
        }

        private void SpawnBus(BusData busData)
        {
            GameObject busGo;

            if (busPrefab != null)
            {
                busGo = Instantiate(busPrefab, busParent);
                busGo.transform.localPosition = Vector3.zero;
            }
            else
            {
                busGo = null;
                Debug.LogError("Bus prefab not found on" + gameObject.name);
            }

            if (busGo != null)
            {
                var busView = busGo.GetComponent<BusView>();
                if (busView != null)
                {
                    _container.Inject(busView);

                    var busModel = new BusModel(
                        busData.busColor,
                        busData.capacity,
                        busArrivalPoint.position,
                        busDeparturePoint.position
                    );

                    busView.Initialize(busModel, _gameConfig);
                    _activeBuses.Add(busView);

                    _signalBus.Fire(new BusSpawnedSignal(busGo, busData.busColor));
                }
            }
        }

        private void ClearAllBuses()
        {
            foreach (var bus in _activeBuses)
                if (bus != null && bus.gameObject != null)
                    DestroyImmediate(bus.gameObject);

            _activeBuses.Clear();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus?.TryUnsubscribe<BusArrivedSignal>(OnBusArrived);
            _signalBus?.TryUnsubscribe<BusDepartedSignal>(OnBusDeparted);
        }
    }
}