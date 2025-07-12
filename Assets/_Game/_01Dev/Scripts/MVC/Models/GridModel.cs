using System.Collections.Generic;
using BusJam.Data;
using UnityEngine;

namespace BusJam.MVC.Models
{
    public class GridModel
    {
        private readonly HashSet<Vector2Int> _voidCells;

        public GridModel(int width, int height, float cellSize, Vector3 gridOffset, LevelData levelData = null)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            GridOffset = gridOffset;
            OccupiedCells = new Dictionary<Vector2Int, bool>();
            _voidCells = new HashSet<Vector2Int>();
            
            if (levelData != null)
                InitializeVoidCells(levelData);
        }

        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }
        public Vector3 GridOffset { get; }
        public Dictionary<Vector2Int, bool> OccupiedCells { get; }

        public bool IsValidPosition(Vector2Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < Width &&
                   gridPosition.y >= 0 && gridPosition.y < Height;
        }

        public bool IsCellEmpty(Vector2Int gridPosition)
        {
            return IsValidPosition(gridPosition) && !OccupiedCells.ContainsKey(gridPosition);
        }

        public void SetCellOccupied(Vector2Int gridPosition, bool occupied)
        {
            if (occupied)
                OccupiedCells[gridPosition] = true;
            else
                OccupiedCells.Remove(gridPosition);
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            var worldX = (gridPosition.x - Width * 0.5f + 0.5f) * CellSize + GridOffset.x;
            var worldZ = (gridPosition.y - Height * 0.5f + 0.5f) * CellSize + GridOffset.z;
            return new Vector3(worldX, GridOffset.y, worldZ);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            var adjustedX = (worldPosition.x - GridOffset.x) / CellSize + Width * 0.5f - 0.5f;
            var adjustedZ = (worldPosition.z - GridOffset.z) / CellSize + Height * 0.5f - 0.5f;

            var gridX = Mathf.RoundToInt(adjustedX);
            var gridY = Mathf.RoundToInt(adjustedZ);

            return new Vector2Int(gridX, gridY);
        }

        public bool IsVoidCell(Vector2Int gridPosition)
        {
            return _voidCells.Contains(gridPosition);
        }

        private void InitializeVoidCells(LevelData levelData)
        {
            for (var i = 0; i < levelData.cells.Length; i++)
            {
                if (levelData.cells[i]?.type == CellType.Void)
                {
                    var row = i / levelData.cols;
                    var col = i % levelData.cols;
                    _voidCells.Add(new Vector2Int(col, row));
                }
            }
        }
    }
}