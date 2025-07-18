using BusJam.Core;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class GridCellView : MonoBehaviour
    {
        [SerializeField] private Renderer cellRenderer;
        private GameConfig gameConfig;
        private Vector2Int gridPosition;

        public Vector2Int GridPosition => gridPosition;
        public Vector3 WorldPosition { get; private set; }
        public bool IsEmpty { get; private set; }

        private void Awake()
        {
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
        }

        [Inject]
        public void Construct(GameConfig gameConfig)
        {
            this.gameConfig = gameConfig;
        }

        public void Initialize(Vector2Int gridPos, Vector3 worldPos)
        {
            gridPosition = gridPos;
            WorldPosition = worldPos;
            IsEmpty = true;

            transform.position = WorldPosition;
            name = $"GridCell_{gridPosition.x}_{gridPosition.y}";
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            if (cellRenderer != null)
            {
                var color = Color.white;
                color.a = 0.1f;
                cellRenderer.material.color = color;
            }
        }

    }
}