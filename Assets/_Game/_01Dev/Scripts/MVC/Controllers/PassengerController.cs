using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using BusJam.Factories;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class PassengerController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform passengerParent;
        
        private readonly List<PassengerView> _allPassengers = new();
        private readonly Dictionary<Vector2Int, PassengerView> _passengerGrid = new();
        
        private BenchController _benchController;
        private BusController _busController;
        private PassengerFactory _passengerFactory;
        private GameConfig _gameConfig;
        private GridController _gridController;
        private PassengerView _selectedPassenger;
        private SignalBus _signalBus;

        public int TotalPassengerCount => _allPassengers.Count;
        
        public void Initialize()
        {
            SetupTransforms();
            SubscribeToEvents();
            RegisterControllersWithGrid();
        }

        private void SetupTransforms()
        {
            if (passengerParent == null)
            {
                var parentGo = new GameObject("Passengers");
                parentGo.transform.SetParent(transform);
                passengerParent = parentGo.transform;
            }
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig, GridController gridController,
            PassengerFactory passengerFactory, BenchController benchController, BusController busController)
        {
            _signalBus = signalBus;
            _gameConfig = gameConfig;
            _gridController = gridController;
            _passengerFactory = passengerFactory;
            _benchController = benchController;
            _busController = busController;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus.Subscribe<PassengerRemovedSignal>(OnPassengerRemoved);
        }

        private void RegisterControllersWithGrid()
        {
            _gridController.RegisterControllers(_busController, _benchController);
        }
        
        #region Event Handlers

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            CreatePassengersFromLevelData(signal.LevelData);
        }
        
        private void OnPassengerClicked(PassengerClickedSignal signal)
        {
            var passenger = GetPassengerFromSignal(signal);
            if (passenger == null) return;

            var model = passenger.GetModel();
            ClearSelection();

            var validation = _gridController.ValidatePassengerMovement(model.GridPosition, model.Color);

            if (!validation.CanMove)
            {
                HandleBlockedPassenger(passenger, validation.BlockingReason);
                return;
            }

            switch (validation.Destination)
            {
                case MovementDestination.Bus:
                    ExecuteBusMovement(passenger);
                    break;
                case MovementDestination.Bench:
                    ExecuteBenchMovement(passenger);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnPassengerRemoved(PassengerRemovedSignal signal)
        {
            var passengerView = signal.PassengerView.GetComponent<PassengerView>();
            if (passengerView != null)
            {
                RemovePassengerFromTracking(passengerView);
                _passengerFactory.Release(passengerView);
                CheckAllPassengersRemoved();
            }
        }

        #endregion

        #region Movement Execution

        private void ExecuteBusMovement(PassengerView passenger)
        {
            var bus = _busController.GetLoadingBusOfColor(passenger.GetModel().Color);
            if (bus == null || !bus.BoardPassenger(passenger.gameObject))
            {
                Debug.LogError("Bus movement failed");
                return;
            }

            passenger.GetModel().SetState(PassengerState.OnBus);
            RemovePassengerFromGrid(passenger);
            passenger.PlayPickupAnimation();
        }

        private void ExecuteBenchMovement(PassengerView passenger)
        {
            if (!_benchController.AddPassengerToQueue(passenger.gameObject))
            {
                Debug.LogError("Bench movement failed - validation error!");
                return;
            }

            passenger.GetModel().SetState(PassengerState.InQueue);
            RemovePassengerFromGrid(passenger);
            passenger.PlayPickupAnimation();
        }

        #endregion

        #region Blocked Passenger Handling

        private void HandleBlockedPassenger(PassengerView passenger, MovementBlockingReason reason)
        {
            switch (reason)
            {
                case MovementBlockingReason.SurroundedByPassengers:
                    Debug.LogWarning($"Passenger at {passenger.GetModel().GridPosition} is surrounded!");
                    ShowBlockedAnimation(passenger);
                    break;
                    
                case MovementBlockingReason.BenchQueueFull:
                    Debug.LogError("GAME OVER: Bench queue is full!");
                    _signalBus.Fire<LevelFailedSignal>();
                    break;
            }
        }

        private void ShowBlockedAnimation(PassengerView passenger)
        {
            passenger.transform.DOPunchScale(Vector3.one * -0.1f, 0.3f).SetEase(Ease.OutBounce);
        }

        #endregion

        #region Helper Methods

        private PassengerView GetPassengerFromSignal(PassengerClickedSignal signal)
        {
            var passenger = signal.PassengerView.GetComponent<PassengerView>();
            if (passenger == null || !_allPassengers.Contains(passenger))
                return null;
            return passenger;
        }

        private void ClearSelection()
        {
            if (_selectedPassenger != null)
            {
                _selectedPassenger.SetSelected(false);
                _selectedPassenger = null;
            }
        }

        private void RemovePassengerFromGrid(PassengerView passenger)
        {
            var model = passenger.GetModel();
            var gridPosition = model.GridPosition;

            if (_passengerGrid.ContainsKey(gridPosition))
            {
                _passengerGrid.Remove(gridPosition);
                _gridController.SetCellState(gridPosition, true);
            }

            _allPassengers.Remove(passenger);
            CheckAllPassengersRemoved();
        }

        private void RemovePassengerFromTracking(PassengerView passenger)
        {
            if (_selectedPassenger == passenger) 
                _selectedPassenger = null;

            var model = passenger.GetModel();
            if (model.IsOnGrid)
            {
                var gridPos = model.GridPosition;
                if (_passengerGrid.ContainsKey(gridPos))
                {
                    _passengerGrid.Remove(gridPos);
                    _gridController.SetCellState(gridPos, true);
                }
                _allPassengers.Remove(passenger);
            }
        }

        private void CheckAllPassengersRemoved()
        {
            if (_allPassengers.Count == 0) 
                _signalBus.Fire<AllPassengersRemovedSignal>();
        }

        #endregion

        #region Passenger Creation

        private void CreatePassengersFromLevelData(LevelData levelData)
        {
            ClearAllPassengers();
            _benchController.ClearQueue();

            var initialPassengers = levelData?.GetInitialPassengers();
            if (initialPassengers == null) return;

            foreach (var passengerData in initialPassengers)
                CreatePassenger(passengerData.color, passengerData.gridPosition);
        }

        private void CreatePassenger(PassengerColor color, Vector2Int gridPosition)
        {
            if (_passengerGrid.ContainsKey(gridPosition)) return;
            if (!_gridController.IsValidPosition(gridPosition)) return;

            var worldPosition = _gridController.GridToWorldPosition(gridPosition);
            var passengerView = _passengerFactory.Create(color, gridPosition, worldPosition, passengerParent);

            _allPassengers.Add(passengerView);
            _passengerGrid[gridPosition] = passengerView;
            _gridController.SetCellState(gridPosition, false);

            _signalBus.Fire(new PassengerCreatedSignal(passengerView.gameObject, color, gridPosition));
        }

        private void ClearAllPassengers()
        {
            _selectedPassenger = null;
            foreach (var passenger in _allPassengers)
                if (passenger != null)
                    _passengerFactory.Release(passenger);
            _allPassengers.Clear();
            _passengerGrid.Clear();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearAllPassengers();
            DOTween.Kill(this);
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus?.TryUnsubscribe<PassengerRemovedSignal>(OnPassengerRemoved);
        }

        #endregion
    }
}