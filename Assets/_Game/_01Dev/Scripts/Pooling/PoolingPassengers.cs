using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using BusJam.MVC.Views;
using DG.Tweening;

namespace BusJam.Pooling
{
    public class PoolingPassengers : MonoBehaviour
    {
        [SerializeField] private PassengerView prefab;
        [SerializeField] private int poolSize = 20;
        
        private Queue<PassengerView> availableObjects = new();
        private List<PassengerView> activeObjects = new();
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
            for (int i = 0; i < poolSize; i++)
            {
                var obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                availableObjects.Enqueue(obj);
            }
        }
        
        public PassengerView Get()
        {
            PassengerView obj;
            
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
            return obj;
        }
        
        public void Return(PassengerView obj)
        {
            if (obj == null) return;
            
            obj.transform.DOKill();
            
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;
            
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            
            obj.SetSelected(false);
            obj.SetDragging(false);
            
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }
        
        private PassengerView CreateNewObject()
        {
            var obj = Instantiate(prefab, transform);
            return obj;
        }
        
        public void SetPrefab(PassengerView newPrefab)
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