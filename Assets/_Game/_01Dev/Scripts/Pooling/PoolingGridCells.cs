using System.Collections.Generic;
using UnityEngine;
using Zenject;
using BusJam.MVC.Views;

namespace BusJam.Pooling
{
    public class PoolingGridCells : MonoBehaviour
    {
        [SerializeField] private GridCellView prefab;
        [SerializeField] private int poolSize = 100;
        
        private Queue<GridCellView> availableObjects = new();
        private List<GridCellView> activeObjects = new();
        private DiContainer container;
        
        [Inject]
        public void Construct(DiContainer container)
        {
            this.container = container;
        }
        
        private void Start()
        {
            PreWarmPool();
        }
        
        private void PreWarmPool()
        {
            for (var i = 0; i < poolSize; i++)
            {
                var obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                availableObjects.Enqueue(obj);
            }
        }
        
        public GridCellView Get()
        {
            GridCellView obj;
            
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }
            
            container.Inject(obj);
            
            activeObjects.Add(obj);
            obj.gameObject.SetActive(true);
            obj.SetEmpty(true);
            return obj;
        }
        
        public void Return(GridCellView obj)
        {
            if (obj == null) return;
            
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            obj.SetEmpty(true);
            
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
        
        private GridCellView CreateNewObject()
        {
            var obj = Instantiate(prefab, transform);
            return obj;
        }
        
        public void SetPrefab(GridCellView newPrefab)
        {
            prefab = newPrefab;
        }
        
        public void SetPoolSize(int size)
        {
            poolSize = size;
        }
    }
}