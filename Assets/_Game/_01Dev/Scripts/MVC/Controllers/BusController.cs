using System.Collections.Generic;
using System.Linq;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using BusJam.Pooling;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class BusController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform busParent;
        [SerializeField] private Transform busSpawnPoint;
        [SerializeField] private Transform busArrivalPoint;
        [SerializeField] private Transform busDeparturePoint;
        
        private readonly List<BusView> _activeBuses = new();
        private readonly Queue<BusQueueEntry> _busSequence = new();
        
        private BenchController _benchController;
        private PoolingBuses _busPool;
        private GameConfig _gameConfig;
        private SignalBus _signalBus;

        public int ActiveBusCount => _activeBuses.Count;
        public int RemainingBusCount => _busSequence.Count;
        public bool HasActiveBuses => _activeBuses.Count > 0;

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
        public void Construct(SignalBus signalBus, GameConfig gameConfig, PoolingBuses busPool,
            BenchController benchController)
        {
            _signalBus = signalBus;
            _gameConfig = gameConfig;
            _busPool = busPool;
            _benchController = benchController;
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
            if (busView == null)
            {
                Debug.LogError("[BUS CONTROLLER] BusView component not found on arrived bus");
                return;
            }

            Debug.Log($"[BUS CONTROLLER] {signal.BusColor} bus arrived, starting passenger loading");
            AutoLoadPassengersFromQueue(busView);
        }
        
        private void AutoLoadPassengersFromQueue(BusView busView)
        {
            if (_benchController == null)
            {
                Debug.LogError("[BUS CONTROLLER] BenchController not available for passenger loading");
                return;
            }

            var busModel = busView.GetModel();
            var loadedCount = 0;

            while (!busModel.IsFull && _benchController.GetPassengerCountOfColor(busModel.BusColor) > 0)
            {
                var passengerGo = _benchController.RemovePassengerOfColorFromQueue(busModel.BusColor);
                if (passengerGo == null) break;

                if (TryBoardPassenger(busView, passengerGo))
                {
                    loadedCount++;
                }
                else
                {
                    _benchController.AddPassengerToQueue(passengerGo);
                    Debug.LogWarning("[BUS CONTROLLER] Failed to board passenger, returning to queue");
                    break;
                }
            }

            Debug.Log($"[BUS CONTROLLER] Loaded {loadedCount} passengers onto {busModel.BusColor} bus");
            
            _signalBus.Fire(new BusLoadedSignal(busView.gameObject, busModel.BusColor, loadedCount));
        }
        
        private bool TryBoardPassenger(BusView busView, GameObject passengerGo)
        {
            var passengerView = passengerGo.GetComponent<PassengerView>();
            if (passengerView == null)
            {
                Debug.LogError("[BUS CONTROLLER] PassengerView component not found");
                return false;
            }

            if (busView.BoardPassenger(passengerGo))
            {
                passengerView.GetModel().SetState(PassengerState.OnBus);
                return true;
            }

            return false;
        }
        
        public BusView GetLoadingBusOfColor(PassengerColor color)
        {
            foreach (var busView in _activeBuses)
            {
                if (busView == null) continue;

                var model = busView.GetModel();
                if (model.BusColor == color && model.State == BusState.Loading && !model.IsFull)
                {
                    return busView;
                }
            }

            return null;
        }

        private void OnBusDeparted(BusDepartedSignal signal)
        {
            var busView = signal.BusView.GetComponent<BusView>();
            if (busView != null)
            {
                RemovePassengersFromBus(busView);
                
                _activeBuses.Remove(busView);
                _busPool.Return(busView);
                Debug.Log($"[BUS CONTROLLER] {signal.BusColor} bus departed and returned to pool");
            }

            SpawnNextBus();

            CheckAllBusesCompleted();
        }
        
        private void RemovePassengersFromBus(BusView busView)
        {
            var busModel = busView.GetModel();
            var passengers = busModel.GetPassengers();
            
            foreach (var passengerGO in passengers)
            {
                if (passengerGO != null)
                {
                    _signalBus.Fire(new PassengerRemovedSignal(passengerGO));
                }
            }
            
            busModel.ClearPassengers();
        }
        
        private void CheckAllBusesCompleted()
        {
            if (_busSequence.Count == 0 && _activeBuses.Count == 0)
            {
                Debug.Log("[BUS CONTROLLER] All buses completed their routes");
                _signalBus.Fire<AllBusesCompletedSignal>();
            }
        }
        
        private void SetupBusSequence(List<BusQueueEntry> busDataList)
        {
            _busSequence.Clear();
            ClearAllBuses();

            if (busDataList == null || busDataList.Count == 0)
            {
                Debug.LogWarning("[BUS CONTROLLER] No bus data provided for level");
                return;
            }

            foreach (var busData in busDataList)
            {
                _busSequence.Enqueue(busData);
            }

            Debug.Log($"[BUS CONTROLLER] Set up bus sequence with {busDataList.Count} buses");
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
        
        private void SpawnBus(BusQueueEntry busData)
        {
            var busView = _busPool.Get();
            if (busView == null)
            {
                Debug.LogError("[BUS CONTROLLER] Failed to get bus from pool");
                return;
            }

            if (busParent != null)
            {
                busView.transform.SetParent(busParent);
            }

            if (busSpawnPoint == null || busArrivalPoint == null || busDeparturePoint == null)
            {
                Debug.LogError("[BUS CONTROLLER] Bus spawn, arrival, or departure point not set");
                return;
            }

            var busModel = new BusModel(
                busData.busColor,
                busData.capacity,
                busSpawnPoint.position,
                busArrivalPoint.position,
                busDeparturePoint.position
            );

            busView.Initialize(busModel);
            _activeBuses.Add(busView);

            Debug.Log($"[BUS CONTROLLER] Spawned {busData.busColor} bus with capacity {busData.capacity}");
            _signalBus.Fire(new BusSpawnedSignal(busView.gameObject, busData.busColor));
        }
        
        private void ClearAllBuses()
        {
            var busCount = _activeBuses.Count;
            
            if (_busPool != null)
            {
                foreach (var bus in _activeBuses.Where(bus => bus != null))
                    _busPool.Return(bus);
            }

            _activeBuses.Clear();
            
            if (busCount > 0)
            {
                Debug.Log($"[BUS CONTROLLER] Cleared {busCount} active buses");
            }
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