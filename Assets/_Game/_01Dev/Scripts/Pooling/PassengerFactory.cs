using BusJam.Data;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using BusJam.Pooling;
using UnityEngine;
using Zenject;

namespace BusJam.Factories
{
    public class PassengerFactory
    {
        private readonly PoolingPassengers _pool;
        private readonly DiContainer _container;

        public PassengerFactory(PoolingPassengers pool, DiContainer container)
        {
            _pool = pool;
            _container = container;
        }

        public PassengerView Create(PassengerColor color, Vector2Int gridPosition, Vector3 worldPosition, Transform parent)
        {
            var passengerView = _pool.Get();
            
            var passengerModel = new PassengerModel(color, gridPosition);
            
            passengerView.Initialize(passengerModel);
            
            passengerView.transform.position = worldPosition;
            passengerView.transform.SetParent(parent);
            
            passengerView.name = $"Passenger_{color}_{gridPosition.x}_{gridPosition.y}";
            
            return passengerView;
        }

        public void Release(PassengerView passengerView)
        {
            if (passengerView == null) return;
            
            _pool.Return(passengerView);
        }
    }
}