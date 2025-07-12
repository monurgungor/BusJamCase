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
        private StationConfiguration _queueConfig;

        private SignalBus _signalBus;

        public int QueueCount => _passengerQueue.Count;
        private int MaxQueueSize { get; set; }

        private bool IsFull => _passengerQueue.Count >= MaxQueueSize;
        private bool IsEmpty => _passengerQueue.Count == 0;

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
            MaxQueueSize = levelData.waitingAreaSize;

            ClearQueueSlots();
            CreateQueueSlots();
        }

        public void StartLevel()
        {
        }

        private void CreateQueueSlots()
        {
            ClearQueueSlots();

            GameObject slotGo;
            for (var i = 0; i < MaxQueueSize; i++)
            {
                var slotPosition = CalculateSlotPosition(i);

                if (queueSlotPrefab != null)
                {
                    slotGo = Instantiate(queueSlotPrefab, slotPosition, Quaternion.identity, queueParent);
                }
                else
                {
                    slotGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    slotGo.transform.SetParent(queueParent);
                    slotGo.transform.position = slotPosition;
                    slotGo.transform.localScale = new Vector3(0.8f, 0.1f, 0.8f);
                }

                slotGo.name = $"QueueSlot_{i}";
                _queueSlots.Add(slotGo.transform);
            }
        }

        private Vector3 CalculateSlotPosition(int index)
        {
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
            if (IsFull || passengerGo == null)
                return false;

            var passengerView = passengerGo.GetComponent<PassengerView>();
            if (passengerView == null)
                return false;

            var slotIndex = GetNextAvailableSlot();
            if (slotIndex == -1)
                return false;

            _passengerQueue.Enqueue(passengerGo);
            _slotOccupancy[slotIndex] = passengerGo;

            var model = passengerView.GetModel();
            model.SetState(PassengerState.InQueue);
            model.SetQueueIndex(slotIndex);

            var targetPosition = GetQueuePosition(slotIndex);
            AnimatePassengerToQueue(passengerGo, targetPosition);

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
                return null;

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

            if (targetPassenger == null)
                return null;

            _passengerQueue.Clear();
            _slotOccupancy.Clear();

            for (var i = 0; i < tempList.Count; i++)
            {
                var passengerGo = tempList[i];
                var passengerView = passengerGo.GetComponent<PassengerView>();

                if (passengerView != null)
                {
                    _passengerQueue.Enqueue(passengerGo);
                    _slotOccupancy[i] = passengerGo;

                    var model = passengerView.GetModel();
                    model.SetQueueIndex(i);

                    var newPosition = GetQueuePosition(i);
                    AnimatePassengerToQueue(passengerGo, newPosition);
                }
            }

            var targetPassengerView = targetPassenger.GetComponent<PassengerView>();
            if (targetPassengerView != null)
            {
                var model = targetPassengerView.GetModel();
                model.SetQueueIndex(-1);
            }

            return targetPassenger;
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

        private void ReorganizeQueue()
        {
            _slotOccupancy.Clear();

            var tempList = new List<GameObject>(_passengerQueue);
            _passengerQueue.Clear();

            for (var i = 0; i < tempList.Count; i++)
            {
                var passengerGO = tempList[i];
                var passengerView = passengerGO.GetComponent<PassengerView>();

                if (passengerView != null)
                {
                    _passengerQueue.Enqueue(passengerGO);
                    _slotOccupancy[i] = passengerGO;

                    var model = passengerView.GetModel();
                    model.SetQueueIndex(i);

                    var newPosition = GetQueuePosition(i);
                    AnimatePassengerToQueue(passengerGO, newPosition);
                }
            }
        }

        public void ClearQueue()
        {
            _passengerQueue.Clear();
            _slotOccupancy.Clear();
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