using System.Collections.Generic;
using System.Linq;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Views;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public enum MovementBlockingReason
    {
        None,
        SurroundedByPassengers,
        BenchQueueFull
    }

    public enum MovementDestination
    {
        Bus,
        Bench
    }

    public struct MovementValidationResult
    {
        public bool CanMove;
        public MovementDestination Destination;
        public MovementBlockingReason BlockingReason;
        
        public static MovementValidationResult Success(MovementDestination destination)
        {
            return new MovementValidationResult
            {
                CanMove = true,
                Destination = destination,
                BlockingReason = MovementBlockingReason.None
            };
        }
        
        public static MovementValidationResult Blocked(MovementBlockingReason reason)
        {
            return new MovementValidationResult
            {
                CanMove = false,
                Destination = MovementDestination.Bench,
                BlockingReason = reason
            };
        }
    }
    
    public class GridController : MonoBehaviour, IInitializable
    {
        private GridGenerator _gridGenerator;
        private PathfindingService _pathfindingService;
        private SignalBus _signalBus;
        
        private BusController _busController;
        private BenchController _benchController;
        
        public void Initialize()
        {
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GridGenerator gridGenerator, PathfindingService pathfindingService)
        {
            _signalBus = signalBus;
            _gridGenerator = gridGenerator;
            _pathfindingService = pathfindingService;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            _gridGenerator.InitializeLevel(signal.LevelData);
        }
        
        public void RegisterControllers(BusController busController, BenchController benchController)
        {
            _busController = busController;
            _benchController = benchController;
        }
        
        public MovementValidationResult ValidatePassengerMovement(Vector2Int passengerPosition, PassengerColor color)
        {
            if (!CanPassengerMoveAtAll(passengerPosition))
                return MovementValidationResult.Blocked(MovementBlockingReason.SurroundedByPassengers);

            if (IsBusAvailableForColor(color))
                return MovementValidationResult.Success(MovementDestination.Bus);

            if (!IsBenchSpaceAvailable())
                return MovementValidationResult.Blocked(MovementBlockingReason.BenchQueueFull);

            return MovementValidationResult.Success(MovementDestination.Bench);
        }
        
        public bool CanPassengerMoveAtAll(Vector2Int position)
        {
            if (!_gridGenerator.IsValidPosition(position)) return false;

            var neighbors = _pathfindingService.GetDirectNeighbors(position);
            return neighbors.Any(_gridGenerator.IsCellEmpty);
        }
        
        private bool IsBusAvailableForColor(PassengerColor color)
        {
            if (_busController == null)
            {
                Debug.LogWarning("[GRID CONTROLLER] BusController not registered");
                return false;
            }
            
            var bus = _busController.GetLoadingBusOfColor(color);
            return bus != null && !bus.GetModel().IsFull;
        }
        
        private bool IsBenchSpaceAvailable()
        {
            if (_benchController == null)
            {
                Debug.LogWarning("[GRID CONTROLLER] BenchController not registered");
                return false;
            }
            return _benchController.CanAcceptPassenger();
        }
        
        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return _gridGenerator.GridToWorldPosition(gridPosition);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return _gridGenerator.WorldToGridPosition(worldPosition);
        }

        public bool IsValidPosition(Vector2Int gridPosition)
        {
            return _gridGenerator.IsValidPosition(gridPosition);
        }

        public GridCellView GetCellAt(Vector2Int gridPosition)
        {
            return _gridGenerator.GetCellAt(gridPosition);
        }

        public bool IsCellEmpty(Vector2Int gridPosition)
        {
            return _gridGenerator.IsCellEmpty(gridPosition);
        }

        public bool IsVoidCell(Vector2Int gridPosition)
        {
            return _gridGenerator.IsVoidCell(gridPosition);
        }

        public List<Vector2Int> GetDirectNeighbors(Vector2Int gridPosition)
        {
            return _pathfindingService.GetDirectNeighbors(gridPosition);
        }


        public int GetGridHeight()
        {
            return _gridGenerator.GetGridHeight();
        }

        public int GetGridWidth()
        {
            return _gridGenerator.GetGridWidth();
        }

        public List<Vector2Int> FindPathToFrontRow(Vector2Int startPos)
        {
            return _pathfindingService.FindPathToFrontRow(startPos);
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
        }
    }
}