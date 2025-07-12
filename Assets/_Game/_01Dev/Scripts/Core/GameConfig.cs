using BusJam.Data;
using UnityEngine;

namespace BusJam.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "BusJam/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Grid Configuration")] [SerializeField]
        private float cellSize = 1f;

        [Header("Station Configuration")] [SerializeField]
        private float stationSlotSpacing = 1f;

        [Header("Bus Configuration")] [SerializeField]
        private float busArrivalSpeed = 5f;

        [SerializeField] private float busDepartureSpeed = 8f;
        [SerializeField] private float busAutoLoadDelay = 1f;
        [SerializeField] private int defaultBusCapacity = 4;

        [Header("Passenger Configuration")] [SerializeField]
        private float passengerMoveSpeed = 3f;

        [Header("Color Configuration")] [SerializeField]
        private Color[] gameColors =
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.magenta,
            new(1f, 0.5f, 0f)
        };

        [Header("Animation Configuration")] [SerializeField]
        private float successAnimationDuration = 0.4f;

        [SerializeField] private float errorAnimationDuration = 0.5f;
        [SerializeField] private float pickupAnimationDuration = 0.3f;
        [SerializeField] private float dropAnimationDuration = 0.4f;

        public float CellSize => cellSize;
        public float StationSlotSpacing => stationSlotSpacing;
        public float BusArrivalSpeed => busArrivalSpeed;
        public float BusDepartureSpeed => busDepartureSpeed;
        public float PassengerMoveSpeed => passengerMoveSpeed;
        public float ErrorAnimationDuration => errorAnimationDuration;
        public float PickupAnimationDuration => pickupAnimationDuration;

        public Color GetPassengerColor(PassengerColor color)
        {
            var index = (int)color;
            if (index >= 0 && index < gameColors.Length)
                return gameColors[index];
            return Color.white;
        }

        public Color GetBusColor(PassengerColor color)
        {
            var index = (int)color;
            if (index >= 0 && index < gameColors.Length)
                return gameColors[index];
            return Color.white;
        }
    }
}