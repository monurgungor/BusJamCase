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
        [SerializeField] private Transform gridParent;
        
        private readonly Dictionary<Vector2Int, GridCellView> _cellLookup = new();
        private PoolingGridCells _gridCellPool;
        private GameConfig _gameConfig;
        private GridModel _gridModel;
        private SignalBus _signalBus;
        
        private BusController _busController;
        private BenchController _benchController;

        public int Width => _gridModel?.Width ?? 0;
        public int Height => _gridModel?.Height ?? 0;
        public float CellSize => _gridModel?.CellSize ?? 1f;
        public Vector3 GridOffset => gridParent != null ? gridParent.position : Vector3.zero;
        public GridCellView[,] GridCells { get; private set; }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearGrid();
        }
        
        public void Initialize()
        {
            SetupGridParent();
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig, PoolingGridCells gridCellPool)
        {
            _signalBus = signalBus;
            _gameConfig = gameConfig;
            _gridCellPool = gridCellPool;
        }

        private void SetupGridParent()
        {
            if (gridParent == null)
            {
                var parentGo = new GameObject("Grid");
                parentGo.transform.SetParent(transform);
                gridParent = parentGo.transform;
            }
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            InitializeLevel(signal.LevelData);
        }
        
        public void RegisterControllers(BusController busController, BenchController benchController)
        {
            _busController = busController;
            _benchController = benchController;
            Debug.Log("Controllers registered with GridController");
        }
        
        #region Grid Management

        private void InitializeLevel(LevelData levelData)
        {
            ClearGrid();
            CreateGridModel(levelData);
            GenerateGrid();
        }

        private void CreateGridModel(LevelData levelData)
        {
            _gridModel = new GridModel(
                levelData.cols,
                levelData.rows,
                _gameConfig.CellSize,
                GridOffset);
        }

        private void GenerateGrid()
        {
            GridCells = new GridCellView[_gridModel.Width, _gridModel.Height];
            _cellLookup.Clear();

            for (var x = 0; x < _gridModel.Width; x++)
            for (var y = 0; y < _gridModel.Height; y++)
                CreateGridCell(x, y);
        }

        private void CreateGridCell(int x, int y)
        {
            var gridPosition = new Vector2Int(x, y);
            var worldPosition = GridToWorldPosition(gridPosition);

            var cell = _gridCellPool.Get();
            if (cell == null)
            {
                Debug.LogError("Failed to get grid cell from pool");
                return;
            }

            cell.Initialize(gridPosition, worldPosition);
            cell.transform.SetParent(gridParent);
            cell.transform.position = worldPosition;

            GridCells[x, y] = cell;
            _cellLookup[gridPosition] = cell;
            cell.name = $"GridCell_{x}_{y}";
        }

        #endregion

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
            if (!IsValidPosition(position)) return false;

            var neighbors = GetDirectNeighbors(position);
            return neighbors.Any(IsCellEmpty);
        }

        private bool IsBusAvailableForColor(PassengerColor color)
        {
            if (_busController == null) return false;
            
            var bus = _busController.GetLoadingBusOfColor(color);
            return bus != null && !bus.GetModel().IsFull;
        }

        private bool IsBenchSpaceAvailable()
        {
            if (_benchController == null) return false;
            return _benchController.CanAcceptPassenger();
        }
        
        #region Grid Queries

        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return _gridModel?.GridToWorldPosition(gridPosition) ?? Vector3.zero;
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return _gridModel?.WorldToGridPosition(worldPosition) ?? Vector2Int.zero;
        }

        public bool IsValidPosition(Vector2Int gridPosition)
        {
            return _gridModel?.IsValidPosition(gridPosition) ?? false;
        }

        public GridCellView GetCellAt(Vector2Int gridPosition)
        {
            _cellLookup.TryGetValue(gridPosition, out var cell);
            return cell;
        }

        public bool IsCellEmpty(Vector2Int gridPosition)
        {
            var cell = GetCellAt(gridPosition);
            return cell != null && cell.IsEmpty;
        }

        public List<Vector2Int> GetDirectNeighbors(Vector2Int gridPosition)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var direction in directions)
            {
                var neighborPos = gridPosition + direction;
                if (IsValidPosition(neighborPos)) 
                    neighbors.Add(neighborPos);
            }

            return neighbors;
        }

        public void SetCellState(Vector2Int gridPosition, bool isEmpty)
        {
            var cell = GetCellAt(gridPosition);
            if (cell != null) cell.SetEmpty(isEmpty);
        }

        #endregion

        #region Cleanup

        private void ClearGrid()
        {
            if (GridCells != null)
            {
                foreach (var cell in GridCells)
                    if (cell != null)
                        _gridCellPool.Return(cell);
            }

            GridCells = null;
            _cellLookup.Clear();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
        }

        #endregion
    }
}