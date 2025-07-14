using System.Collections.Generic;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using BusJam.Factories;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class PassengerSpawner : MonoBehaviour
    {
        private PassengerFactory _passengerFactory;
        private GridController _gridController;
        private SignalBus _signalBus;

        [Inject]
        public void Construct(PassengerFactory passengerFactory, GridController gridController, SignalBus signalBus)
        {
            _passengerFactory = passengerFactory;
            _gridController = gridController;
            _signalBus = signalBus;
        }

        public void CreatePassengersFromLevelData(LevelData levelData, Transform passengerParent)
        {
            var initialPassengers = levelData?.GetInitialPassengers();
            if (initialPassengers == null) return;

            foreach (var passengerData in initialPassengers)
                CreatePassenger(passengerData.color, passengerData.gridPosition, passengerParent);
        }

        public PassengerView CreatePassenger(PassengerColor color, Vector2Int gridPosition, Transform passengerParent)
        {
            if (!_gridController.IsValidPosition(gridPosition)) return null;
            if (_gridController.IsVoidCell(gridPosition)) return null;

            var worldPosition = _gridController.GridToWorldPosition(gridPosition);
            var passengerView = _passengerFactory.Create(color, gridPosition, worldPosition, passengerParent);

            if (passengerView != null)
            {
                _signalBus.Fire(new PassengerCreatedSignal(passengerView.gameObject, color, gridPosition));
            }

            return passengerView;
        }
    }
} 