using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using BusJam.Pooling;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class GridGenerator : MonoBehaviour
    {
        [SerializeField] private Transform gridParent;
        
        private PoolingGridCells _gridCellPool;
        private GameConfig _gameConfig;
        private GridModel _gridModel;
        
        public Vector3 GridOffset => gridParent != null ? gridParent.position : Vector3.zero;
        public GridCellView[,] GridCells { get; private set; }
        private readonly Dictionary<Vector2Int, GridCellView> _cellLookup = new();
        
        [Inject]
        public void Construct(GameConfig gameConfig, PoolingGridCells gridCellPool)
        {
            _gameConfig = gameConfig;
            _gridCellPool = gridCellPool;
        }

        public void InitializeLevel(LevelData levelData)
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
                GridOffset,
                levelData);
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
            var isVoid = IsVoidCell(gridPosition);

            var cell = _gridCellPool.Get();
            if (cell == null)
            {
                Debug.LogError("Failed to get grid cell from pool");
                return;
            }

            cell.Initialize(gridPosition, worldPosition, isVoid);
            cell.transform.SetParent(gridParent);
            cell.transform.position = worldPosition;

            GridCells[x, y] = cell;
            _cellLookup[gridPosition] = cell;
            cell.name = $"GridCell_{x}_{y}";
        }

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
            return cell != null && cell.IsEmpty && !IsVoidCell(gridPosition);
        }

        public bool IsVoidCell(Vector2Int gridPosition)
        {
            return _gridModel?.IsVoidCell(gridPosition) ?? false;
        }

        public void SetCellState(Vector2Int gridPosition, bool isEmpty)
        {
            var cell = GetCellAt(gridPosition);
            if (cell != null) cell.SetEmpty(isEmpty);
        }

        public int GetGridHeight()
        {
            return _gridModel?.Height ?? 0;
        }

        public int GetGridWidth()
        {
            return _gridModel?.Width ?? 0;
        }

        private void ClearGrid()
        {
            if (GridCells != null && _gridCellPool != null)
            {
                foreach (var cell in GridCells)
                    if (cell != null)
                        _gridCellPool.Return(cell);
            }

            GridCells = null;
            _cellLookup.Clear();
        }

        private void OnDestroy()
        {
            ClearGrid();
        }
    }
} 