using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class GridController : MonoBehaviour, IInitializable
    {
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private Transform gridParent;
        private readonly Dictionary<Vector2Int, GridCellView> _cellLookup = new();
        private DiContainer _container;
        private GameConfig _gameConfig;
        private GridModel _gridModel;

        private SignalBus _signalBus;

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

        private void OnDrawGizmos()
        {
            if (GridCells == null || _gridModel == null)
                return;

            Gizmos.color = Color.white;
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
            {
                var worldPos = GridToWorldPosition(new Vector2Int(x, y));
                Gizmos.DrawWireCube(worldPos, Vector3.one * CellSize);
            }
        }

        public void Initialize()
        {
            if (gridParent == null)
            {
                var parentGo = new GameObject("Grid");
                parentGo.transform.SetParent(transform);
                gridParent = parentGo.transform;
            }

            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig, DiContainer container)
        {
            _signalBus = signalBus;
            _gameConfig = gameConfig;
            _container = container;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus.Subscribe<GridCellClickedSignal>(OnGridCellClicked);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            InitializeLevel(signal.LevelData);
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            
        }

        private void InitializeLevel(LevelData levelData)
        {
            ClearGrid();

            _gridModel = new GridModel(
                levelData.cols,
                levelData.rows,
                _gameConfig.CellSize,
                GridOffset);

            GenerateGrid();
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

            GameObject cellGo;
            if (gridCellPrefab != null)
            {
                cellGo = Instantiate(gridCellPrefab, worldPosition, Quaternion.identity, gridParent);
            }
            else
            {
                cellGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                cellGo.transform.SetParent(gridParent);
                cellGo.transform.position = worldPosition;
                cellGo.transform.localScale = Vector3.one * CellSize * 0.1f;
                cellGo.AddComponent<GridCellView>();
            }

            var cell = cellGo.GetComponent<GridCellView>();
            if (cell != null)
            {
                _container.Inject(cell);
                cell.Initialize(gridPosition, worldPosition);

                GridCells[x, y] = cell;
                _cellLookup[gridPosition] = cell;

                cellGo.name = $"GridCell_{x}_{y}";
            }
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

        public GridCellView GetCellAt(int x, int y)
        {
            return GetCellAt(new Vector2Int(x, y));
        }

        public bool IsCellEmpty(Vector2Int gridPosition)
        {
            var cell = GetCellAt(gridPosition);
            return cell != null && cell.IsEmpty;
        }

        public List<Vector2Int> GetNeighbors(Vector2Int gridPosition, bool includeDiagonal = false)
        {
            var neighbors = new List<Vector2Int>();

            var directions = includeDiagonal
                ? new[]
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                }
                : new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var direction in directions)
            {
                var neighborPos = gridPosition + direction;
                if (IsValidPosition(neighborPos)) neighbors.Add(neighborPos);
            }

            return neighbors;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
        {
            if (!IsValidPosition(start) || !IsValidPosition(target))
                return null;

            if (start == target)
                return new List<Vector2Int> { start };

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();

            var openSet = new List<Vector2Int> { start };
            gScore[start] = 0;
            fScore[start] = GetHeuristic(start, target);

            while (openSet.Count > 0)
            {
                var current = GetLowestFScore(openSet, fScore);

                if (current == target) return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!IsCellEmpty(neighbor) && neighbor != target)
                        continue;

                    var tentativeGScore = gScore[current] + 1;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + GetHeuristic(neighbor, target);

                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                    }
                }
            }

            return null;
        }

        private float GetHeuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
        {
            var lowest = openSet[0];
            var lowestScore = fScore.ContainsKey(lowest) ? fScore[lowest] : float.MaxValue;

            for (var i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                var score = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;

                if (score < lowestScore)
                {
                    lowest = node;
                    lowestScore = score;
                }
            }

            return lowest;
        }

        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }

            return path;
        }

        public List<Vector2Int> GetEmptyCells()
        {
            var emptyCells = new List<Vector2Int>();

            foreach (var kvp in _cellLookup)
                if (kvp.Value != null && kvp.Value.IsEmpty)
                    emptyCells.Add(kvp.Key);

            return emptyCells;
        }

        public List<Vector2Int> GetOccupiedCells()
        {
            var occupiedCells = new List<Vector2Int>();

            foreach (var kvp in _cellLookup)
                if (kvp.Value != null && !kvp.Value.IsEmpty)
                    occupiedCells.Add(kvp.Key);

            return occupiedCells;
        }

        public void SetCellState(Vector2Int gridPosition, bool isEmpty)
        {
            var cell = GetCellAt(gridPosition);
            if (cell != null) cell.SetEmpty(isEmpty);
        }

        public void ClearGrid()
        {
            if (GridCells != null)
                foreach (var cell in GridCells)
                    if (cell != null && cell.gameObject != null)
                        DestroyImmediate(cell.gameObject);

            GridCells = null;
            _cellLookup.Clear();
        }

        public Vector2Int GetRandomEmptyCell()
        {
            var emptyCells = GetEmptyCells();
            if (emptyCells.Count > 0) return emptyCells[Random.Range(0, emptyCells.Count)];
            return Vector2Int.zero;
        }

        public float GetDistanceBetweenCells(Vector2Int cellA, Vector2Int cellB)
        {
            return Vector2Int.Distance(cellA, cellB);
        }

        public bool IsWithinBounds(Vector3 worldPosition)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            return IsValidPosition(gridPos);
        }

        private void OnGridCellClicked(GridCellClickedSignal signal)
        {
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
            _signalBus?.TryUnsubscribe<GridCellClickedSignal>(OnGridCellClicked);
        }
    }
}