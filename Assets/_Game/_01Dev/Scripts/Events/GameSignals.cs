using BusJam.Data;
using UnityEngine;

namespace BusJam.Events
{
    public struct LevelLoadedSignal
    {
        public readonly LevelData LevelData;

        public LevelLoadedSignal(LevelData levelData)
        {
            LevelData = levelData;
            Debug.Log($"[LEVEL] Level loaded: {levelData?.name} | Passengers: {levelData?.GetTotalPassengerCount()} | Buses: {levelData?.GetBusCount()}");
        }
    }

    public struct LevelStartedSignal
    {
        public readonly LevelData LevelData;

        public LevelStartedSignal(LevelData levelData)
        {
            LevelData = levelData;
            Debug.Log($"[LEVEL] Level started: {levelData?.name} | Time limit: {levelData?.timeLimit}s");
        }
    }

    public struct LevelCompletedSignal
    {
    }

    public struct LevelFailedSignal
    {
    }

    public struct GamePausedSignal
    {
    }

    public struct GameResumedSignal
    {
    }

    public struct PassengerCreatedSignal
    {
        public readonly GameObject PassengerView;
        public readonly PassengerColor Color;
        public readonly Vector2Int GridPosition;

        public PassengerCreatedSignal(GameObject passengerView, PassengerColor color, Vector2Int gridPosition)
        {
            PassengerView = passengerView;
            Color = color;
            GridPosition = gridPosition;
            Debug.Log($"[PASSENGER] Created {color} passenger at {gridPosition} | Object: {passengerView?.name}");
        }
    }

    public struct PassengerClickedSignal
    {
        public readonly GameObject PassengerView;
        public readonly Vector2Int GridPosition;

        public PassengerClickedSignal(GameObject passengerView, Vector2Int gridPosition)
        {
            PassengerView = passengerView;
            GridPosition = gridPosition;
            Debug.Log($"[PASSENGER] Clicked passenger at {gridPosition} | Object: {passengerView?.name}");
        }
    }

    public struct PassengerMovedSignal
    {
        public readonly GameObject PassengerView;
        public readonly Vector2Int FromPosition;
        public readonly Vector2Int ToPosition;

        public PassengerMovedSignal(GameObject passengerView, Vector2Int fromPosition, Vector2Int toPosition)
        {
            PassengerView = passengerView;
            FromPosition = fromPosition;
            ToPosition = toPosition;
            var distance = Vector2Int.Distance(fromPosition, toPosition);
            Debug.Log($"[PASSENGER] Moved from {fromPosition} to {toPosition} | Distance: {distance:F1} | Object: {passengerView?.name}");
        }
    }

    public struct PassengerRemovedSignal
    {
        public readonly GameObject PassengerView;
        public readonly PassengerColor Color;

        public PassengerRemovedSignal(GameObject passengerView, PassengerColor color)
        {
            PassengerView = passengerView;
            Color = color;
            Debug.Log($"[PASSENGER] Removed {color} passenger | Object: {passengerView?.name}");
        }
    }

    public struct BusSpawnedSignal
    {
        public readonly GameObject BusView;
        public readonly PassengerColor BusColor;

        public BusSpawnedSignal(GameObject busView, PassengerColor busColor)
        {
            BusView = busView;
            BusColor = busColor;
            Debug.Log($"[BUS] <color=cyan>Spawned {busColor} bus</color> | Object: {busView?.name}");
        }
    }

    public struct BusArrivedSignal
    {
        public readonly GameObject BusView;
        public readonly PassengerColor BusColor;

        public BusArrivedSignal(GameObject busView, PassengerColor busColor)
        {
            BusView = busView;
            BusColor = busColor;
            Debug.Log($"[BUS] <color=yellow>{busColor} bus arrived at station</color> | Object: {busView?.name}");
        }
    }

    public struct BusLoadedSignal
    {
        public readonly GameObject BusView;
        public readonly PassengerColor BusColor;
        public readonly int PassengerCount;

        public BusLoadedSignal(GameObject busView, PassengerColor busColor, int passengerCount)
        {
            BusView = busView;
            BusColor = busColor;
            PassengerCount = passengerCount;
            if (passengerCount == 0)
            {
                Debug.LogWarning($"[BUS] <color=orange>{busColor} bus loaded with 0 passengers!</color> | Object: {busView?.name}");
            }
            else
            {
                Debug.Log($"[BUS] <color=green>{busColor} bus loaded with {passengerCount} passengers</color> | Object: {busView?.name}");
            }
        }
    }

    public struct BusDepartedSignal
    {
        public readonly GameObject BusView;
        public readonly PassengerColor BusColor;

        public BusDepartedSignal(GameObject busView, PassengerColor busColor)
        {
            BusView = busView;
            BusColor = busColor;
            Debug.Log($"[BUS] <color=magenta>{busColor} bus departed</color> | Object: {busView?.name}");
        }
    }

    public struct GridCellClickedSignal
    {
        public readonly Vector2Int GridPosition;
        public readonly Vector3 WorldPosition;

        public GridCellClickedSignal(Vector2Int gridPosition, Vector3 worldPosition)
        {
            GridPosition = gridPosition;
            WorldPosition = worldPosition;
            Debug.Log($"[GRID] Cell clicked at {gridPosition} | World pos: {worldPosition}");
        }
    }

    public struct AllBusesCompletedSignal
    {
    }

    public struct AllPassengersRemovedSignal
    {
    }
}