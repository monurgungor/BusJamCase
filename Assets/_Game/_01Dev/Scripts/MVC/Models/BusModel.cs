using System.Collections.Generic;
using BusJam.Data;
using UnityEngine;

namespace BusJam.MVC.Models
{
    public enum BusState
    {
        Approaching,
        Loading,
        Departing,
        Gone
    }

    public class BusModel
    {
        public BusModel(PassengerColor busColor, int capacity, Vector3 arrivalPosition, Vector3 departurePosition)
        {
            BusColor = busColor;
            Capacity = capacity;
            ArrivalPosition = arrivalPosition;
            DeparturePosition = departurePosition;
            State = BusState.Approaching;
            PassengerViews = new List<GameObject>();
            IsInteractable = false;
        }

        public PassengerColor BusColor { get; private set; }
        public int Capacity { get; }
        public BusState State { get; private set; }
        public Vector3 ArrivalPosition { get; private set; }
        public Vector3 DeparturePosition { get; private set; }
        public List<GameObject> PassengerViews { get; }
        public bool IsInteractable { get; private set; }

        public int CurrentPassengerCount => PassengerViews.Count;
        public int AvailableSpace => Capacity - PassengerViews.Count;
        public bool IsFull => PassengerViews.Count >= Capacity;
        public bool IsEmpty => PassengerViews.Count == 0;

        public void SetState(BusState newState)
        {
            State = newState;
            IsInteractable = newState == BusState.Loading;
        }

        public bool AddPassenger(GameObject passengerView)
        {
            if (IsFull) return false;

            PassengerViews.Add(passengerView);
            return true;
        }

        public void RemovePassenger(GameObject passengerView)
        {
            PassengerViews.Remove(passengerView);
        }

        public void ClearPassengers()
        {
            PassengerViews.Clear();
        }
    }
}