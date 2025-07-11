using BusJam.Data;
using UnityEngine;

namespace BusJam.MVC.Models
{
    public enum PassengerState
    {
        OnGrid,
        Moving,
        InQueue,
        OnBus,
        Removed
    }

    public class PassengerModel
    {
        public PassengerModel(PassengerColor color, Vector2Int gridPosition)
        {
            Color = color;
            GridPosition = gridPosition;
            State = PassengerState.OnGrid;
            IsSelected = false;
            IsDragging = false;
            CanInteract = true;
            QueueIndex = -1;
        }

        public PassengerColor Color { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public PassengerState State { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsDragging { get; private set; }
        public bool CanInteract { get; private set; }
        public int QueueIndex { get; private set; }

        public bool IsOnGrid => State == PassengerState.OnGrid;
        public bool IsInQueue => State == PassengerState.InQueue;
        public bool IsOnBus => State == PassengerState.OnBus;

        public void SetGridPosition(Vector2Int newPosition)
        {
            GridPosition = newPosition;
        }

        public void SetState(PassengerState newState)
        {
            State = newState;

            switch (newState)
            {
                case PassengerState.OnGrid:
                    CanInteract = true;
                    QueueIndex = -1;
                    break;
                case PassengerState.Moving:
                    CanInteract = false;
                    break;
                case PassengerState.InQueue:
                    CanInteract = true;
                    break;
                case PassengerState.OnBus:
                case PassengerState.Removed:
                    CanInteract = false;
                    break;
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
        }

        public void SetDragging(bool dragging)
        {
            IsDragging = dragging;
        }

        public void SetQueueIndex(int index)
        {
            QueueIndex = index;
        }
    }
}