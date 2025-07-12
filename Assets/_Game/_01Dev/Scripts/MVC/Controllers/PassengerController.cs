using System.Collections.Generic;
using System.Linq;
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
        private SignalBus _signalBus;
        
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
            _signalBus.Subscribe<GridCellClickedSignal>(OnGridCellClicked);
        }

        private void RegisterControllersWithGrid()
        {
            _gridController.RegisterControllers(_busController, _benchController);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            CreatePassengersFromLevelData(signal.LevelData);
            CalculatePassengerOutlines();
        }
        
        private void OnPassengerClicked(PassengerClickedSignal signal)
        {
            var passenger = GetPassengerFromSignal(signal);
            if (passenger == null || passenger.IsMoving()) return;

            HandlePassengerClick(passenger);
        }

        private void HandlePassengerClick(PassengerView passenger)
        {
            UnityEngine.Debug.Assert(passenger != null, "[PASSENGER CONTROLLER] Passenger view is null");
            
            var model = passenger.GetModel();
            UnityEngine.Debug.Assert(model != null, "[PASSENGER CONTROLLER] Passenger model is null");
            
            var currentPos = model.GridPosition;
            UnityEngine.Debug.Assert(_passengerGrid.ContainsKey(currentPos), "[PASSENGER CONTROLLER] Passenger not found in grid at expected position");
            
            _passengerGrid.Remove(currentPos);
            _gridController.SetCellState(currentPos, true);
            model.SetState(PassengerState.Moving);
            
            CalculatePassengerOutlines(passenger);
            
            var isInFrontRow = model.IsInFrontRow(_gridController.GetGridHeight());
            
            if (isInFrontRow)
            {
                HandleFrontRowPassengerClick(passenger);
            }
            else
            {
                HandleBackRowPassengerClick(passenger);
            }
        }

        private void HandleFrontRowPassengerClick(PassengerView passenger)
        {
            var model = passenger.GetModel();
            var validation = _gridController.ValidatePassengerMovement(model.GridPosition, model.Color);

            if (!validation.CanMove)
            {
                RestorePassengerPosition(passenger);
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
                    RestorePassengerPosition(passenger);
                    break;
            }
        }

        private void HandleBackRowPassengerClick(PassengerView passenger)
        {
            MovePassengerTowardExit(passenger);
        }

        private void MovePassengerTowardExit(PassengerView passenger)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            var path = _gridController.FindPathToFrontRow(currentPos);
            
            if (path.Count > 0)
            {
                ExecuteFullPathMovement(passenger, path);
            }
            else
            {
                if (model.IsInFrontRow(_gridController.GetGridHeight()))
                {
                    ContinueToDestination(passenger);
                }
                else
                {
                    RestorePassengerPosition(passenger);
                    passenger.PlayErrorAnimation();
                }
            }
        }
        
        private void ExecuteFullPathMovement(PassengerView passenger, List<Vector2Int> gridPath)
        {
            var model = passenger.GetModel();
            var finalPos = gridPath[gridPath.Count - 1];
            
            var worldPath = gridPath.Select(gridPos => _gridController.GridToWorldPosition(gridPos)).ToList();
            
            _passengerGrid[finalPos] = passenger;
            _gridController.SetCellState(finalPos, false);
            model.SetGridPosition(finalPos);
            
            passenger.MoveAlongPath(worldPath, () =>
            {
                model.SetState(PassengerState.OnGrid);
                ContinueToDestination(passenger);
                CalculatePassengerOutlines();
            });
        }

        private void RestorePassengerPosition(PassengerView passenger)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            _passengerGrid[currentPos] = passenger;
            _gridController.SetCellState(currentPos, false);
            model.SetState(PassengerState.OnGrid);
            
            CalculatePassengerOutlines();
            
        }

        private void UpdatePassengerGridPosition(PassengerView passenger, Vector2Int fromPos, Vector2Int toPos)
        {
            var model = passenger.GetModel();
            
            _passengerGrid.Remove(fromPos);
            _passengerGrid[toPos] = passenger;
            
            _gridController.SetCellState(fromPos, true);
            _gridController.SetCellState(toPos, false);
            
            model.SetGridPosition(toPos);
            
        }
        
        private void ContinueToDestination(PassengerView passenger)
        {
            var model = passenger.GetModel();
            
            if (model.IsInFrontRow(_gridController.GetGridHeight()))
            {
                HandleFrontRowPassengerClick(passenger);
            }
            else
            {
                model.SetCanExit(true);
            }
        }

        private void OnGridCellClicked(GridCellClickedSignal signal)
        {
            if (!_passengerGrid.TryGetValue(signal.GridPosition, out var passenger) || passenger == null)
                return;

            var targetPosition = signal.GridPosition;
            var model = passenger.GetModel();

            if (CanMoveToPosition(model, targetPosition))
            {
                ExecuteGridMovement(passenger, targetPosition, signal.WorldPosition);
            }
            else
            {
                passenger.PlayErrorAnimation();
            }
        }

        private bool CanMoveToPosition(PassengerModel model, Vector2Int targetPosition)
        {
            if (!model.CanMoveTo(targetPosition)) return false;
            
            if (_passengerGrid.ContainsKey(targetPosition)) return false;
            
            if (!_gridController.IsValidPosition(targetPosition)) return false;
            
            if (_gridController.IsVoidCell(targetPosition)) return false;
            
            return true;
        }

        private void ExecuteGridMovement(PassengerView passenger, Vector2Int targetPosition, Vector3 worldPosition)
        {
            var model = passenger.GetModel();
            
            _passengerGrid[targetPosition] = passenger;
            _gridController.SetCellState(targetPosition, false);
            model.SetGridPosition(targetPosition);
            
            passenger.MoveToGrid(targetPosition, worldPosition, () =>
            {
                model.SetState(PassengerState.OnGrid);
                CheckExitConditions(passenger);
                CalculatePassengerOutlines();
            });
        }

        private void CheckExitConditions(PassengerView passenger)
        {
            var model = passenger.GetModel();
            
            if (model.IsInFrontRow(_gridController.GetGridHeight()))
            {
                model.SetCanExit(true);
                HandleFrontRowPassengerClick(passenger);
            }
        }

        private void OnPassengerRemoved(PassengerRemovedSignal signal)
        {
            var passengerView = signal.PassengerView.GetComponent<PassengerView>();
            if (passengerView == null) return;
            RemovePassengerFromTracking(passengerView);
            _passengerFactory.Release(passengerView);
            CheckAllPassengersRemoved();
        }

        private void ExecuteBusMovement(PassengerView passenger)
        {
            var bus = _busController.GetLoadingBusOfColor(passenger.GetModel().Color);
            if (bus == null)
            {
                passenger.PlayErrorAnimation();
                return;
            }

            var busPosition = bus.transform.position;
            
            RemovePassengerFromGrid(passenger);
            
            passenger.MoveOffGrid(busPosition, () =>
            {
                if (!bus.BoardPassenger(passenger.gameObject)) return;
                passenger.GetModel().SetState(PassengerState.OnBus);
                passenger.PlayPickupAnimation();
                passenger.DisableOutline();
            });
        }

        private void ExecuteBenchMovement(PassengerView passenger)
        {
            var benchPosition = _benchController.GetNextQueuePosition();
            if (benchPosition == Vector3.zero)
            {
                passenger.PlayErrorAnimation();
                return;
            }

            RemovePassengerFromGrid(passenger);
            
            passenger.MoveOffGrid(benchPosition, () =>
            {
                if (!_benchController.AddPassengerToQueue(passenger.gameObject)) return;
                passenger.GetModel().SetState(PassengerState.InQueue);
                passenger.PlayPickupAnimation();
                passenger.DisableOutline();
            });
        }

        private void HandleBlockedPassenger(PassengerView passenger, MovementBlockingReason reason)
        {
            switch (reason)
            {
                case MovementBlockingReason.SurroundedByPassengers:
                    ShowBlockedAnimation(passenger);
                    break;
                    
                case MovementBlockingReason.BenchQueueFull:
                    _signalBus.Fire<LevelFailedSignal>();
                    break;
            }
        }

        private void ShowBlockedAnimation(PassengerView passenger)
        {
            passenger.transform.DOPunchScale(Vector3.one * -0.1f, _gameConfig.ErrorAnimationDuration).SetEase(Ease.OutBounce);
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

            _allPassengers.Remove(passenger);
            
            if (passenger != null)
            {
                passenger.transform.DOKill();
            }
            
            CheckAllPassengersRemoved();
        }

        private void RemovePassengerFromTracking(PassengerView passenger)
        {
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
            Debug.Assert(_passengerFactory != null, "[PASSENGER CONTROLLER] PassengerFactory is null");
            Debug.Assert(_gridController != null, "[PASSENGER CONTROLLER] GridController is null");
            
            if (_passengerGrid.ContainsKey(gridPosition)) return;
            if (!_gridController.IsValidPosition(gridPosition)) return;
            if (_gridController.IsVoidCell(gridPosition)) return;

            var worldPosition = _gridController.GridToWorldPosition(gridPosition);
            var passengerView = _passengerFactory.Create(color, gridPosition, worldPosition, passengerParent);

            Debug.Assert(passengerView != null, "[PASSENGER CONTROLLER] Failed to create passenger view");

            _allPassengers.Add(passengerView);
            _passengerGrid[gridPosition] = passengerView;
            _gridController.SetCellState(gridPosition, false);

            _signalBus.Fire(new PassengerCreatedSignal(passengerView.gameObject, color, gridPosition));
        }

        private void ClearAllPassengers()
        {
            if (_passengerFactory != null)
            {
                foreach (var passenger in _allPassengers)
                    if (passenger != null)
                        _passengerFactory.Release(passenger);
            }
            _allPassengers.Clear();
            _passengerGrid.Clear();
        }

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
            _signalBus?.TryUnsubscribe<GridCellClickedSignal>(OnGridCellClicked);
        }

        private void CalculatePassengerOutlines(PassengerView exclude = null)
        {
            foreach (var passenger in _allPassengers)
            {
                if (passenger == exclude)
                {
                    passenger.DisableOutline();
                    continue;
                }
                
                var model = passenger.GetModel();
                
                if (!model.IsOnGrid)
                {
                    passenger.DisableOutline();
                    continue;
                }
                
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
            var currentPos = model.GridPosition;
            
            if (model.IsInFrontRow(_gridController.GetGridHeight()))
            {
                var validation = _gridController.ValidatePassengerMovement(currentPos, model.Color);
                return validation.CanMove;
            }
            
            var path = _gridController.FindPathToFrontRow(currentPos);
            return path.Count > 0;
        }
    }
}