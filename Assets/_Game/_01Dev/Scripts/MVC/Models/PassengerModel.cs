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

    public enum MovementDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public class PassengerModel
    {
        public PassengerModel(PassengerColor color, Vector2Int gridPosition)
        {
            Color = color;
            GridPosition = gridPosition;
            TargetPosition = gridPosition;
            State = PassengerState.OnGrid;
            IsSelected = false;
            IsDragging = false;
            CanInteract = true;
            QueueIndex = -1;
            CanExit = false;
            IsMoving = false;
        }

        public PassengerColor Color { get; private set; }
        public Vector2Int GridPosition { get; private set; }
        public Vector2Int TargetPosition { get; private set; }
        public PassengerState State { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsDragging { get; private set; }
        public bool CanInteract { get; private set; }
        public bool CanExit { get; private set; }
        public bool IsMoving { get; private set; }
        public int QueueIndex { get; private set; }

        public bool IsOnGrid => State == PassengerState.OnGrid;
        public bool IsInQueue => State == PassengerState.InQueue;
        public bool IsOnBus => State == PassengerState.OnBus;

        public void SetGridPosition(Vector2Int newPosition)
        {
            GridPosition = newPosition;
            TargetPosition = newPosition;
        }

        public void SetTargetPosition(Vector2Int targetPosition)
        {
            TargetPosition = targetPosition;
        }

        public void StartMovement(Vector2Int targetPosition)
        {
            TargetPosition = targetPosition;
            IsMoving = true;
            SetState(PassengerState.Moving);
        }

        public void CompleteMovement()
        {
            GridPosition = TargetPosition;
            IsMoving = false;
            SetState(PassengerState.OnGrid);
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

        public void SetCanExit(bool canExit)
        {
            CanExit = canExit;
        }

        public bool IsInFrontRow(int gridRows)
        {
            return GridPosition.y == 0;
        }

        public MovementDirection GetMovementDirection(Vector2Int target)
        {
            var diff = target - GridPosition;
            
            if (diff.x > 0) return MovementDirection.Right;
            if (diff.x < 0) return MovementDirection.Left;
            if (diff.y > 0) return MovementDirection.Up;
            if (diff.y < 0) return MovementDirection.Down;
            
            return MovementDirection.None;
        }

        public bool CanMoveTo(Vector2Int target)
        {
            if (IsMoving || !CanInteract) return false;
            
            var distance = Vector2Int.Distance(GridPosition, target);
            return distance == 1f;
        }

        public Vector2Int[] GetValidMovePositions()
        {
            return new Vector2Int[]
            {
                GridPosition + Vector2Int.up,
                GridPosition + Vector2Int.down,
                GridPosition + Vector2Int.left,
                GridPosition + Vector2Int.right
            };
        }
    }
}