using UnityEngine;
using Zenject;
using BusJam.MVC.Views;
using BusJam.Pooling;
using BusJam.Factories;

namespace BusJam.Installers
{
    public class PoolingInstaller : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private PassengerView passengerPrefab;
        [SerializeField] private BusView busPrefab;
        [SerializeField] private GridCellView gridCellPrefab;
        
        [Header("Pool Sizes")]
        [SerializeField] private int passengerPoolSize = 20;
        [SerializeField] private int busPoolSize = 5;
        [SerializeField] private int gridCellPoolSize = 100;
        
        public void InstallBindings(DiContainer container)
        {
            CreatePools(container);
            BindFactory(container);
        }
        
        private void CreatePools(DiContainer container)
        {
            var passengerPool = CreatePassengerPool(container);
            container.Bind<PoolingPassengers>().FromInstance(passengerPool).AsSingle();
            
            var busPool = CreateBusPool(container);
            container.Bind<PoolingBuses>().FromInstance(busPool).AsSingle();
            
            var gridCellPool = CreateGridCellPool(container);
            container.Bind<PoolingGridCells>().FromInstance(gridCellPool).AsSingle();
        }
        
        private void BindFactory(DiContainer container)
        {
            container.Bind<PassengerFactory>().AsSingle();
        }
        
        private PoolingPassengers CreatePassengerPool(DiContainer container)
        {
            var poolGO = new GameObject("Passenger Pool");
            poolGO.transform.SetParent(transform);
            
            var pool = poolGO.AddComponent<PoolingPassengers>();
            pool.SetPrefab(passengerPrefab);
            pool.SetPoolSize(passengerPoolSize);
            
            container.QueueForInject(pool);
            return pool;
        }
        
        private PoolingBuses CreateBusPool(DiContainer container)
        {
            var poolGO = new GameObject("Bus Pool");
            poolGO.transform.SetParent(transform);
            
            var pool = poolGO.AddComponent<PoolingBuses>();
            pool.SetPrefab(busPrefab);
            pool.SetPoolSize(busPoolSize);
            
            container.QueueForInject(pool);
            return pool;
        }
        
        private PoolingGridCells CreateGridCellPool(DiContainer container)
        {
            var poolGO = new GameObject("GridCell Pool");
            poolGO.transform.SetParent(transform);
            
            var pool = poolGO.AddComponent<PoolingGridCells>();
            pool.SetPrefab(gridCellPrefab);
            pool.SetPoolSize(gridCellPoolSize);
            
            container.QueueForInject(pool);
            return pool;
        }
    }
}