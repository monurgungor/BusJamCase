using BusJam.Core;
using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class GridCellView : MonoBehaviour
    {
        [SerializeField] private Renderer cellRenderer;
        private GameConfig gameConfig;
        private Vector2Int gridPosition;

        private SignalBus signalBus;

        public Vector2Int GridPosition => gridPosition;
        public Vector3 WorldPosition { get; private set; }

        public bool IsEmpty { get; private set; }

        private void Awake()
        {
            if (cellRenderer == null)
                cellRenderer = GetComponent<Renderer>();
        }

        private void OnMouseDown()
        {
            signalBus.Fire(new GridCellClickedSignal(gridPosition, WorldPosition));
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig)
        {
            this.signalBus = signalBus;
            this.gameConfig = gameConfig;
        }

        public void Initialize(Vector2Int gridPos, Vector3 worldPos)
        {
            gridPosition = gridPos;
            WorldPosition = worldPos;
            IsEmpty = true;

            transform.position = WorldPosition;
            name = $"GridCell_{gridPosition.x}_{gridPosition.y}";
        }

        public void SetEmpty(bool empty)
        {
            IsEmpty = empty;
        }
    }
}