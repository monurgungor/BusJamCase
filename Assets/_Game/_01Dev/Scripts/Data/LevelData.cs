using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam.Data
{
    public enum PassengerColor
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5
    }

    public enum CellType
    {
        Empty = 0,
        Void = 1,
        Passenger = 2
    }

    [Serializable]
    public class Cell
    {
        public CellType type;
        public PassengerColor colour;
    }

    [Serializable]
    public class GridConfiguration
    {
        public int width = 8;
        public int height = 6;
    }

    [Serializable]
    public class StationConfiguration
    {
        public int maxQueueSize = 10;
    }

    [Serializable]
    public class BusQueueEntry
    {
        public PassengerColor busColor;
        public int capacity = 4;
    }

    [CreateAssetMenu(fileName = "New Level", menuName = "BusJam/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int rows = 6;
        public int cols = 8;
        public float timeLimit = 120f;
        public int waitingAreaSize = 10;

        public Cell[] cells = new Cell[0];
        public BusQueueEntry[] busQueue = new BusQueueEntry[0];

        public bool ValidateLevel()
        {
            if (GetTotalPassengerCount() == 0)
                return false;

            if (busQueue.Length == 0)
                return false;

            var usedColors = new HashSet<PassengerColor>();
            foreach (var cell in cells)
                if (cell != null && cell.type == CellType.Passenger)
                    usedColors.Add(cell.colour);

            foreach (var bus in busQueue)
                if (!usedColors.Contains(bus.busColor))
                    return false;

            return true;
        }

        public int GetPassengerCountByColor(PassengerColor color)
        {
            var count = 0;
            foreach (var cell in cells)
                if (cell != null && cell.type == CellType.Passenger && cell.colour == color)
                    count++;
            return count;
        }

        public int GetTotalPassengerCount()
        {
            var count = 0;
            foreach (var cell in cells)
                if (cell != null && cell.type == CellType.Passenger)
                    count++;
            return count;
        }

        public int GetBusCount()
        {
            return busQueue.Length;
        }

        public List<PassengerData> GetInitialPassengers()
        {
            var passengers = new List<PassengerData>();
            for (var i = 0; i < cells.Length; i++)
                if (cells[i] != null && cells[i].type == CellType.Passenger)
                {
                    var row = i / cols;
                    var col = i % cols;
                    passengers.Add(new PassengerData
                    {
                        color = cells[i].colour,
                        gridPosition = new Vector2Int(col, row)
                    });
                }

            return passengers;
        }

        public List<BusQueueEntry> GetBusSequence()
        {
            var busData = new List<BusQueueEntry>();
            for (var i = 0; i < busQueue.Length; i++)
            {
                var entry = new BusQueueEntry
                {
                    busColor = busQueue[i].busColor,
                    capacity = busQueue[i].capacity
                };
                busData.Add(entry);
            }
            return busData;
        }
    }

    [Serializable]
    public class PassengerData
    {
        public PassengerColor color;
        public Vector2Int gridPosition;
    }
}