using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class PathfindingService : MonoBehaviour
    {
        private GridGenerator _gridGenerator;

        [Inject]
        public void Construct(GridGenerator gridGenerator)
        {
            _gridGenerator = gridGenerator;
        }

        public List<Vector2Int> FindPathToFrontRow(Vector2Int startPos)
        {
            const int targetRow = 0;
            
            if (startPos.y == targetRow)
            {
                return new List<Vector2Int>();
            }
            
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<PathNode>();
            
            queue.Enqueue(new PathNode(startPos, new List<Vector2Int> { startPos }));
            visited.Add(startPos);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current.Position.y == targetRow)
                {
                    var path = current.Path.Skip(1).ToList();
                    return path;
                }
                
                var neighbors = GetDirectNeighbors(current.Position);
                foreach (var neighbor in neighbors)
                {
                    if (visited.Contains(neighbor) || !_gridGenerator.IsCellEmpty(neighbor))
                        continue;
                    
                    visited.Add(neighbor);
                    var newPath = new List<Vector2Int>(current.Path) { neighbor };
                    queue.Enqueue(new PathNode(neighbor, newPath));
                }
            }
            
            return new List<Vector2Int>();
        }

        public List<Vector2Int> GetDirectNeighbors(Vector2Int gridPosition)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var direction in directions)
            {
                var neighborPos = gridPosition + direction;
                if (_gridGenerator.IsValidPosition(neighborPos)) 
                    neighbors.Add(neighborPos);
            }

            return neighbors;
        }

        private class PathNode
        {
            public Vector2Int Position { get; }
            public List<Vector2Int> Path { get; }
            
            public PathNode(Vector2Int position, List<Vector2Int> path)
            {
                Position = position;
                Path = path;
            }
        }
    }
} 