using BusJam.Data;
using UnityEngine;

namespace BusJam.Events
{
    public enum GameState
    {
        MainMenu = 0,
        LevelSelect = 1,
        Playing = 2,
        LevelComplete = 3,
        LevelFailed = 4
    }
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

    public struct GameStateChangedSignal
    {
        public readonly GameState PreviousState;
        public readonly GameState NewState;

        public GameStateChangedSignal(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
            Debug.Log($"[GAME STATE] Changed from {previousState} to {newState}");
        }
    }

    public struct EnterMainMenuSignal
    {
    }

    public struct EnterLevelSelectSignal
    {
    }

    public struct EnterPlayingSignal
    {
    }

    public struct EnterLevelCompleteSignal
    {
    }

    public struct EnterLevelFailedSignal
    {
    }

    public struct TimerUpdatedSignal
    {
        public readonly float RemainingTime;
        public readonly float TotalTime;
        public readonly float ElapsedTime;

        public TimerUpdatedSignal(float remainingTime, float totalTime, float elapsedTime)
        {
            RemainingTime = remainingTime;
            TotalTime = totalTime;
            ElapsedTime = elapsedTime;
        }
    }

    public struct TimerExpiredSignal
    {
    }

    public struct TimerStartedSignal
    {
        public readonly float TotalTime;

        public TimerStartedSignal(float totalTime)
        {
            TotalTime = totalTime;
            Debug.Log($"[TIMER] Timer started with {totalTime}s");
        }
    }

    public struct TimerStoppedSignal
    {
    }

    public struct TimerPausedSignal
    {
    }

    public struct TimerResumedSignal
    {
    }

    public struct LevelChangedSignal
    {
        public readonly int PreviousLevelIndex;
        public readonly int NewLevelIndex;
        public readonly LevelData NewLevelData;

        public LevelChangedSignal(int previousLevelIndex, int newLevelIndex, LevelData newLevelData)
        {
            PreviousLevelIndex = previousLevelIndex;
            NewLevelIndex = newLevelIndex;
            NewLevelData = newLevelData;
            Debug.Log($"[LEVEL] Changed from level {previousLevelIndex} to level {newLevelIndex}: {newLevelData?.name}");
        }
    }

    public struct LevelRestartedSignal
    {
        public readonly int LevelIndex;
        public readonly LevelData LevelData;

        public LevelRestartedSignal(int levelIndex, LevelData levelData)
        {
            LevelIndex = levelIndex;
            LevelData = levelData;
            Debug.Log($"[LEVEL] Restarted level {levelIndex}: {levelData?.name}");
        }
    }

    public struct NextLevelAvailableSignal
    {
        public readonly int NextLevelIndex;

        public NextLevelAvailableSignal(int nextLevelIndex)
        {
            NextLevelIndex = nextLevelIndex;
            Debug.Log($"[LEVEL] Next level available: {nextLevelIndex}");
        }
    }

    public struct AllLevelsCompletedSignal
    {
    }
}