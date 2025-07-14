using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class PassengerController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform passengerParent;
        
        private PassengerSpawner _passengerSpawner;
        private PassengerMovementController _movementController;
        private PassengerInteractionHandler _interactionHandler;
        private BenchController _benchController;
        private BusController _busController;
        private GridController _gridController;
        private SignalBus _signalBus;
        
        private readonly List<PassengerView> _allPassengers = new();
        private readonly Dictionary<Vector2Int, PassengerView> _passengerGrid = new();
        
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
        public void Construct(SignalBus signalBus, GridController gridController,
            BenchController benchController, BusController busController,
            PassengerSpawner passengerSpawner, PassengerMovementController movementController,
            PassengerInteractionHandler interactionHandler)
        {
            _signalBus = signalBus;
            _gridController = gridController;
            _benchController = benchController;
            _busController = busController;
            _passengerSpawner = passengerSpawner;
            _movementController = movementController;
            _interactionHandler = interactionHandler;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<PassengerCreatedSignal>(OnPassengerCreated);
            _signalBus.Subscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus.Subscribe<PassengerRemovedSignal>(OnPassengerRemoved);
            _signalBus.Subscribe<GridCellClickedSignal>(OnGridCellClicked);
        }

        private void RegisterControllersWithGrid()
        {
            _gridController.RegisterControllers(_busController, _benchController);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            _allPassengers.Clear();
            _passengerGrid.Clear();
            _passengerSpawner.CreatePassengersFromLevelData(signal.LevelData, passengerParent);
            CalculatePassengerOutlines();
        }

        private void OnPassengerCreated(PassengerCreatedSignal signal)
        {
            var passenger = signal.PassengerView.GetComponent<PassengerView>();
            if (passenger == null) return;
            _allPassengers.Add(passenger);
            _passengerGrid[signal.GridPosition] = passenger;
        }
        
        private void OnPassengerClicked(PassengerClickedSignal signal)
        {
            var passenger = GetPassengerFromSignal(signal);
            if (passenger == null || passenger.IsMoving()) return;

            _interactionHandler.HandlePassengerClick(passenger, _passengerGrid);
        }

        private void OnGridCellClicked(GridCellClickedSignal signal)
        {
            _interactionHandler.HandleGridCellClicked(signal.GridPosition, signal.WorldPosition, _passengerGrid);
        }

        private void OnPassengerRemoved(PassengerRemovedSignal signal)
        {
            var passengerView = signal.PassengerView.GetComponent<PassengerView>();
            if (passengerView == null) return;
            RemovePassengerFromTracking(passengerView);
            CheckAllPassengersRemoved();
        }

        private PassengerView GetPassengerFromSignal(PassengerClickedSignal signal)
        {
            var passenger = signal.PassengerView.GetComponent<PassengerView>();
            if (passenger == null || !_allPassengers.Contains(passenger))
                return null;
            return passenger;
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
        }

        private void RemovePassengerFromTracking(PassengerView passenger)
        {
            _allPassengers.Remove(passenger);
            RemovePassengerFromGrid(passenger);
            passenger.transform.DOKill();
        }

        private void CheckAllPassengersRemoved()
        {
            if (_allPassengers.Count == 0)
            {
                _signalBus.Fire<AllPassengersRemovedSignal>();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus?.TryUnsubscribe<PassengerRemovedSignal>(OnPassengerRemoved);
            _signalBus?.TryUnsubscribe<GridCellClickedSignal>(OnGridCellClicked);
        }

        private void CalculatePassengerOutlines(PassengerView exclude = null)
        {
            foreach (var passenger in _allPassengers)
            {
                if (passenger == exclude) continue;
                
                var model = passenger.GetModel();
                var canMove = CanPassengerMoveTowardsExit(passenger);
                
                if (canMove)
                    passenger.EnableOutline();
                else
                    passenger.DisableOutline();
            }
        }

        private bool CanPassengerMoveTowardsExit(PassengerView passenger)
        {
            var model = passenger.GetModel();
            if (!model.IsOnGrid) return false;
            
            var path = _gridController.FindPathToFrontRow(model.GridPosition);
            return path.Count > 0;
        }
    }
}