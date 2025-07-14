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
    public class PassengerInteractionHandler : MonoBehaviour
    {
        private GameConfig _gameConfig;
        private SignalBus _signalBus;
        private GridController _gridController;
        private PassengerMovementController _movementController;
        private BusController _busController;
        private BenchController _benchController;

        [Inject]
        public void Construct(GameConfig gameConfig, SignalBus signalBus, GridController gridController, 
            PassengerMovementController movementController, BusController busController, BenchController benchController)
        {
            _gameConfig = gameConfig;
            _signalBus = signalBus;
            _gridController = gridController;
            _movementController = movementController;
            _busController = busController;
            _benchController = benchController;
        }

        public void HandlePassengerClick(PassengerView passenger, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            passengerGrid.Remove(currentPos);
            model.SetState(PassengerState.Moving);
            
            var gridHeight = _gridController.GetGridHeight();
            var isInFrontRow = model.IsInFrontRow(gridHeight);
            
            if (isInFrontRow)
            {
                HandleFrontRowPassengerClick(passenger, passengerGrid);
            }
            else
            {
                _movementController.MovePassengerTowardExit(passenger, passengerGrid);
            }
        }

        public void HandleFrontRowPassengerClick(PassengerView passenger, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var validation = _gridController.ValidatePassengerMovement(model.GridPosition, model.Color);

            if (!validation.CanMove)
            {
                RestorePassengerPosition(passenger, passengerGrid);
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
                    RestorePassengerPosition(passenger, passengerGrid);
                    break;
            }
        }

        public void HandleGridCellClicked(Vector2Int gridPosition, Vector3 worldPosition, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            if (!passengerGrid.TryGetValue(gridPosition, out var passenger) || passenger == null)
                return;

            var model = passenger.GetModel();
            var targetPosition = gridPosition;

            if (_movementController.CanMoveToPosition(model, targetPosition, passengerGrid))
            {
                _movementController.ExecuteGridMovement(passenger, targetPosition, worldPosition, passengerGrid);
            }
            else
            {
                passenger.PlayErrorAnimation();
            }
        }

        public void HandleBlockedPassenger(PassengerView passenger, MovementBlockingReason reason)
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

        public void ShowBlockedAnimation(PassengerView passenger)
        {
            passenger.transform.DOPunchScale(Vector3.one * -0.1f, _gameConfig.ErrorAnimationDuration).SetEase(Ease.OutBounce);
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
            
            passenger.MoveOffGrid(benchPosition, () =>
            {
                if (!_benchController.AddPassengerToQueue(passenger.gameObject)) return;
                passenger.GetModel().SetState(PassengerState.InQueue);
                passenger.PlayPickupAnimation();
                passenger.DisableOutline();
            });
        }

        private void RestorePassengerPosition(PassengerView passenger, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            passengerGrid[currentPos] = passenger;
            passenger.transform.position = _gridController.GridToWorldPosition(currentPos);
            model.SetState(PassengerState.OnGrid);
        }

    }
} 