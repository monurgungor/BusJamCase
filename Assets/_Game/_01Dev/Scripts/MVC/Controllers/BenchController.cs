using System;
using System.Collections.Generic;
using BusJam.Core;
using BusJam.Data;
using BusJam.Events;
using BusJam.MVC.Models;
using BusJam.MVC.Views;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Controllers
{
    public class BenchController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform queueParent;
        [SerializeField] private GameObject queueSlotPrefab;
        
        private readonly Queue<GameObject> _passengerQueue = new();
        private readonly List<Transform> _queueSlots = new();
        private readonly Dictionary<int, GameObject> _slotOccupancy = new();
        
        private GameConfig _gameConfig;
        private SignalBus _signalBus;

        public int QueueCount => _passengerQueue.Count;
        public int MaxQueueSize { get; private set; }
        public bool IsFull => _passengerQueue.Count >= MaxQueueSize;
        public bool IsEmpty => _passengerQueue.Count == 0;
        public float UtilizationPercentage => MaxQueueSize > 0 ? (float)QueueCount / MaxQueueSize * 100f : 0f;

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearQueueSlots();
        }

        public void Initialize()
        {
            if (queueParent == null)
            {
                var parentGo = new GameObject("Queue");
                if (parentGo == null) throw new ArgumentNullException(nameof(parentGo));
                parentGo.transform.SetParent(transform);
                queueParent = parentGo.transform;
            }

            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig)
        {
            _signalBus = signalBus;
            _gameConfig = gameConfig;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<LevelStartedSignal>(OnLevelStarted);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            InitializeLevel(signal.LevelData);
        }

        private void OnLevelStarted(LevelStartedSignal signal)
        {
            StartLevel();
        }
        
        public void InitializeLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[BENCH CONTROLLER] LevelData is null");
                return;
            }

            MaxQueueSize = levelData.waitingAreaSize;
            
            if (MaxQueueSize <= 0)
            {
                Debug.LogWarning("[BENCH CONTROLLER] Invalid waiting area size, defaulting to 5");
                MaxQueueSize = 5;
            }

            ClearQueueSlots();
            CreateQueueSlots();
            
            Debug.Log($"[BENCH CONTROLLER] Initialized bench with {MaxQueueSize} queue slots");
        }

        public void StartLevel()
        {
        }
        
        private void CreateQueueSlots()
        {
            ClearQueueSlots();

            for (var i = 0; i < MaxQueueSize; i++)
            {
                var slotPosition = CalculateSlotPosition(i);
                var slotGo = CreateQueueSlot(i, slotPosition);
                
                if (slotGo != null)
                {
                    _queueSlots.Add(slotGo.transform);
                }
            }

            Debug.Log($"[BENCH CONTROLLER] Created {_queueSlots.Count} queue slots");
        }
        
        private GameObject CreateQueueSlot(int index, Vector3 position)
        {
            GameObject slotGo;

            if (queueSlotPrefab != null)
            {
                slotGo = Instantiate(queueSlotPrefab, position, Quaternion.identity, queueParent);
            }
            else
            {
                slotGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                slotGo.transform.SetParent(queueParent);
                slotGo.transform.position = position;
                slotGo.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
                
                var collider = slotGo.GetComponent<Collider>();
                if (collider != null) DestroyImmediate(collider);
            }

            slotGo.name = $"QueueSlot_{index}";
            return slotGo;
        }
        
        private Vector3 CalculateSlotPosition(int index)
        {
            if (_gameConfig == null)
            {
                Debug.LogWarning("[BENCH CONTROLLER] GameConfig not available, using default spacing");
                return Vector3.zero;
            }

            var spacing = _gameConfig.StationSlotSpacing;
            var basePosition = queueParent != null ? queueParent.position : Vector3.zero;
            var direction = queueParent != null ? queueParent.forward : Vector3.forward;

            return basePosition + direction * (index * spacing);
        }

        public Vector3 GetQueuePosition(int index)
        {
            if (index < 0 || index >= MaxQueueSize)
                return Vector3.zero;

            return CalculateSlotPosition(index);
        }
        
        public bool CanAcceptPassenger()
        {
            return !IsFull;
        }
        
        public bool AddPassengerToQueue(GameObject passengerGo)
        {
            if (!ValidatePassengerForQueue(passengerGo))
                return false;

            var slotIndex = GetNextAvailableSlot();
            if (slotIndex == -1)
            {
                Debug.LogWarning("[BENCH CONTROLLER] No available slots for passenger");
                return false;
            }

            var passengerView = passengerGo.GetComponent<PassengerView>();
            var model = passengerView.GetModel();

            _passengerQueue.Enqueue(passengerGo);
            _slotOccupancy[slotIndex] = passengerGo;

            model.SetState(PassengerState.InQueue);
            model.SetQueueIndex(slotIndex);

            var targetPosition = GetQueuePosition(slotIndex);
            AnimatePassengerToQueue(passengerGo, targetPosition);
            passengerView.ResetToForwardRotation();

            Debug.Log($"[BENCH CONTROLLER] Added {model.Color} passenger to queue at slot {slotIndex}");
            return true;
        }
        
        private bool ValidatePassengerForQueue(GameObject passengerGo)
        {
            if (IsFull)
            {
                Debug.LogWarning("[BENCH CONTROLLER] Queue is full, cannot add passenger");
                return false;
            }

            if (passengerGo == null)
            {
                Debug.LogError("[BENCH CONTROLLER] Passenger GameObject is null");
                return false;
            }

            var passengerView = passengerGo.GetComponent<PassengerView>();
            if (passengerView == null)
            {
                Debug.LogError("[BENCH CONTROLLER] PassengerView component not found");
                return false;
            }

            return true;
        }

        private List<GameObject> GetPassengersOfColor(PassengerColor color)
        {
            var result = new List<GameObject>();

            foreach (var passengerGo in _passengerQueue)
            {
                var passengerView = passengerGo.GetComponent<PassengerView>();
                if (passengerView != null && passengerView.GetModel().Color == color) result.Add(passengerGo);
            }

            return result;
        }

        public int GetPassengerCountOfColor(PassengerColor color)
        {
            return GetPassengersOfColor(color).Count;
        }
        
        public GameObject RemovePassengerOfColorFromQueue(PassengerColor color)
        {
            if (IsEmpty)
            {
                Debug.LogWarning($"[BENCH CONTROLLER] Cannot remove {color} passenger - queue is empty");
                return null;
            }

            var targetPassenger = FindAndRemovePassengerOfColor(color);
            if (targetPassenger == null)
            {
                Debug.LogWarning($"[BENCH CONTROLLER] No {color} passenger found in queue");
                return null;
            }

            ReorganizeQueueAfterRemoval();

            var targetPassengerView = targetPassenger.GetComponent<PassengerView>();
            if (targetPassengerView != null)
            {
                targetPassengerView.GetModel().SetQueueIndex(-1);
            }

            Debug.Log($"[BENCH CONTROLLER] Removed {color} passenger from queue");
            return targetPassenger;
        }
        
        private GameObject FindAndRemovePassengerOfColor(PassengerColor color)
        {
            var tempList = new List<GameObject>(_passengerQueue);
            GameObject targetPassenger = null;

            for (var i = 0; i < tempList.Count; i++)
            {
                var passengerView = tempList[i].GetComponent<PassengerView>();
                if (passengerView != null && passengerView.GetModel().Color == color)
                {
                    targetPassenger = tempList[i];
                    tempList.RemoveAt(i);
                    break;
                }
            }

            if (targetPassenger != null)
            {
                _passengerQueue.Clear();
                _slotOccupancy.Clear();

                foreach (var passenger in tempList)
                {
                    _passengerQueue.Enqueue(passenger);
                }
            }

            return targetPassenger;
        }
        
        private void ReorganizeQueueAfterRemoval()
        {
            var tempList = new List<GameObject>(_passengerQueue);
            _slotOccupancy.Clear();

            for (var i = 0; i < tempList.Count; i++)
            {
                var passengerGo = tempList[i];
                var passengerView = passengerGo.GetComponent<PassengerView>();

                if (passengerView != null)
                {
                    _slotOccupancy[i] = passengerGo;

                    var model = passengerView.GetModel();
                    model.SetQueueIndex(i);

                    var newPosition = GetQueuePosition(i);
                    AnimatePassengerToQueue(passengerGo, newPosition);
                }
            }
        }

        private int GetNextAvailableSlot()
        {
            for (var i = 0; i < MaxQueueSize; i++)
                if (!_slotOccupancy.ContainsKey(i))
                    return i;

            return -1;
        }

        private void AnimatePassengerToQueue(GameObject passengerGO, Vector3 targetPosition)
        {
            passengerGO.transform.DOMove(targetPosition, 0.5f)
                .SetEase(Ease.OutCubic);
        }
        
        public void ClearQueue()
        {
            var queueCount = _passengerQueue.Count;
            _passengerQueue.Clear();
            _slotOccupancy.Clear();
            
            if (queueCount > 0)
            {
                Debug.Log($"[BENCH CONTROLLER] Cleared {queueCount} passengers from queue");
            }
        }

        public Vector3 GetNextQueuePosition()
        {
            var nextSlot = GetNextAvailableSlot();
            return nextSlot != -1 ? GetQueuePosition(nextSlot) : Vector3.zero;
        }

        private void ClearQueueSlots()
        {
            foreach (var slot in _queueSlots)
                if (slot != null && slot.gameObject != null)
                    DestroyImmediate(slot.gameObject);

            _queueSlots.Clear();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<LevelStartedSignal>(OnLevelStarted);
        }
    }
}