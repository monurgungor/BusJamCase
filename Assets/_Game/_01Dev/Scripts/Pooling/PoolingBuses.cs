using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using BusJam.MVC.Views;
using DG.Tweening;

namespace BusJam.Pooling
{
    public class PoolingBuses : MonoBehaviour
    {
        [SerializeField] private BusView prefab;
        [SerializeField] private int poolSize = 5;
        
        private Queue<BusView> availableObjects = new();
        private List<BusView> activeObjects = new();
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
        
        public BusView Get()
        {
            BusView obj;
            
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
            
            obj.transform.localScale = Vector3.one;
            
            return obj;
        }
        
        public void Return(BusView obj)
        {
            if (obj == null || this == null) return;
            
            obj.transform.DOKill();
            
            var model = obj.GetModel();
            if (model != null)
            {
                model.ClearPassengers();
            }
            
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
        
        private BusView CreateNewObject()
        {
            var obj = Instantiate(prefab, transform);
            return obj;
        }
        
        public void SetPrefab(BusView newPrefab)
        {
            prefab = newPrefab;
        }
        
        public void SetPoolSize(int size)
        {
            poolSize = size;
        }
        
        private void OnDestroy()
        {
            foreach (var obj in activeObjects.Where(obj => obj != null))
            {
                obj.transform.DOKill();
            }
        }
    }
}