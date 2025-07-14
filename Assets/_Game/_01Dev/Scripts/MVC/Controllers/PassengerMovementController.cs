using System.Collections.Generic;
using System.Linq;
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
    public class PassengerMovementController : MonoBehaviour
    {
        private GridController _gridController;
        private GameConfig _gameConfig;
        private SignalBus _signalBus;

        [Inject]
        public void Construct(GridController gridController, GameConfig gameConfig, SignalBus signalBus)
        {
            _gridController = gridController;
            _gameConfig = gameConfig;
            _signalBus = signalBus;
        }

        public void MovePassengerTowardExit(PassengerView passenger, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            var path = _gridController.FindPathToFrontRow(currentPos);
            
            if (path.Count > 0)
            {
                ExecuteFullPathMovement(passenger, path, passengerGrid);
            }
            else
            {
                if (model.IsInFrontRow(_gridController.GetGridHeight()))
                {
                    ContinueToDestination(passenger);
                }
                else
                {
                    RestorePassengerPosition(passenger, passengerGrid);
                    passenger.PlayErrorAnimation();
                }
            }
        }

        public void ExecuteFullPathMovement(PassengerView passenger, List<Vector2Int> gridPath, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var finalPos = gridPath[gridPath.Count - 1];
            
            var worldPath = gridPath.Select(gridPos => _gridController.GridToWorldPosition(gridPos)).ToList();
            
            passengerGrid[finalPos] = passenger;
            _gridController.SetCellState(finalPos, false);
            model.SetGridPosition(finalPos);
            
            passenger.MoveAlongPath(worldPath, () =>
            {
                model.SetState(PassengerState.OnGrid);
                ContinueToDestination(passenger);
            });
        }

        public void ExecuteGridMovement(PassengerView passenger, Vector2Int targetPosition, Vector3 worldPosition, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            
            passengerGrid[targetPosition] = passenger;
            _gridController.SetCellState(targetPosition, false);
            model.SetGridPosition(targetPosition);
            
            passenger.MoveToGrid(targetPosition, worldPosition, () =>
            {
                model.SetState(PassengerState.OnGrid);
                CheckExitConditions(passenger);
            });
        }

        public void RestorePassengerPosition(PassengerView passenger, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            var currentPos = model.GridPosition;
            
            passengerGrid[currentPos] = passenger;
            _gridController.SetCellState(currentPos, false);
            model.SetState(PassengerState.OnGrid);
        }

        public void UpdatePassengerGridPosition(PassengerView passenger, Vector2Int fromPos, Vector2Int toPos, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            var model = passenger.GetModel();
            
            passengerGrid.Remove(fromPos);
            passengerGrid[toPos] = passenger;
            
            _gridController.SetCellState(fromPos, true);
            _gridController.SetCellState(toPos, false);
            
            model.SetGridPosition(toPos);
        }

        public bool CanMoveToPosition(PassengerModel model, Vector2Int targetPosition, Dictionary<Vector2Int, PassengerView> passengerGrid)
        {
            if (!model.CanMoveTo(targetPosition)) return false;
            if (passengerGrid.ContainsKey(targetPosition)) return false;
            if (!_gridController.IsValidPosition(targetPosition)) return false;
            if (_gridController.IsVoidCell(targetPosition)) return false;
            return true;
        }

        private void ContinueToDestination(PassengerView passenger)
        {
            var model = passenger.GetModel();
            
            if (model.IsInFrontRow(_gridController.GetGridHeight()))
            {
                _signalBus.Fire(new PassengerClickedSignal(passenger.gameObject, model.GridPosition));
            }
            else
            {
                model.SetCanExit(true);
            }
        }

        private void CheckExitConditions(PassengerView passenger)
        {
            var model = passenger.GetModel();
            
            if (model.IsInFrontRow(_gridController.GetGridHeight()))
            {
                model.SetCanExit(true);
                _signalBus.Fire(new PassengerClickedSignal(passenger.gameObject, model.GridPosition));
            }
        }
    }
} 