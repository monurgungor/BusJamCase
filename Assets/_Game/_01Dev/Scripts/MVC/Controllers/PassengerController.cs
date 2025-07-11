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
    public class PassengerController : MonoBehaviour, IInitializable
    {
        [SerializeField] private Transform passengerParent;
        [SerializeField] private GameObject passengerPrefab;
        private readonly List<PassengerView> _allPassengers = new();
        private readonly Dictionary<Vector2Int, PassengerView> _passengerGrid = new();
        private BenchController _benchController;
        private BusController _busController;
        private DiContainer _container;
        private GameConfig _gameConfig;
        private GridController _gridController;
        private PassengerView _selectedPassenger;

        private SignalBus _signalBus;

        public int TotalPassengerCount => _allPassengers.Count;

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ClearAllPassengers();
            DOTween.Kill(this);
        }

        public void Initialize()
        {
            SetupTransforms();
            SubscribeToEvents();
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig, GridController gridController,
            DiContainer container, BenchController benchController, BusController busController)
        {
            this._signalBus = signalBus;
            this._gameConfig = gameConfig;
            this._gridController = gridController;
            this._container = container;
            this._benchController = benchController;
            this._busController = busController;
        }

        private void SetupTransforms()
        {
            if (passengerParent == null)
            {
                var parentGo = new GameObject("Passengers");
                parentGo.transform.SetParent(transform);
                passengerParent = parentGo.transform;
            }
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus.Subscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus.Subscribe<PassengerRemovedSignal>(OnPassengerRemoved);
        }

        private void OnLevelLoaded(LevelLoadedSignal signal)
        {
            CreatePassengersFromLevelData(signal.LevelData);
        }

        private void OnPassengerClicked(PassengerClickedSignal signal)
        {
            var clickedPassenger = signal.PassengerView.GetComponent<PassengerView>();
            if (clickedPassenger == null || !_allPassengers.Contains(clickedPassenger))
                return;

            var model = clickedPassenger.GetModel();

            if (!model.IsOnGrid || !model.CanInteract)
                return;

            if (_selectedPassenger != null)
            {
                _selectedPassenger.SetSelected(false);
                _selectedPassenger = null;
            }

            var loadingBus = _busController.GetLoadingBusOfColor(model.Color);
            if (loadingBus != null)
            {
                TryBoardPassengerDirectly(clickedPassenger, loadingBus);
                return;
            }

            TryMovePassengerToQueue(clickedPassenger);
        }

        private void TryBoardPassengerDirectly(PassengerView passengerView, BusView busView)
        {
            var model = passengerView.GetModel();
            var gridPosition = model.GridPosition;

            if (!busView.BoardPassenger(passengerView.gameObject)) return;

            model.SetState(PassengerState.OnBus);

            if (_passengerGrid.ContainsKey(gridPosition))
            {
                _passengerGrid.Remove(gridPosition);
                _gridController.SetCellState(gridPosition, true);
            }

            _allPassengers.Remove(passengerView);

            CheckAllPassengersRemoved();
        }

        private void TryMovePassengerToQueue(PassengerView passengerView)
        {
            var model = passengerView.GetModel();
            var gridPosition = model.GridPosition;

            if (!_benchController.CanAcceptPassenger()) return;

            if (!_benchController.AddPassengerToQueue(passengerView.gameObject)) return;

            if (_passengerGrid.ContainsKey(gridPosition))
            {
                _passengerGrid.Remove(gridPosition);
                _gridController.SetCellState(gridPosition, true);
            }

            _allPassengers.Remove(passengerView);

            CheckAllPassengersRemoved();
        }

        private void CheckAllPassengersRemoved()
        {
            if (_allPassengers.Count == 0) _signalBus.Fire<AllPassengersRemovedSignal>();
        }

        private void OnPassengerRemoved(PassengerRemovedSignal signal)
        {
            var passengerView = signal.PassengerView.GetComponent<PassengerView>();
            if (passengerView != null)
            {
                if (_selectedPassenger == passengerView) _selectedPassenger = null;

                var model = passengerView.GetModel();

                if (model.IsOnGrid)
                {
                    var gridPos = model.GridPosition;
                    if (_passengerGrid.ContainsKey(gridPos))
                    {
                        _passengerGrid.Remove(gridPos);
                        _gridController.SetCellState(gridPos, true);
                    }

                    _allPassengers.Remove(passengerView);
                }

                if (_allPassengers.Count == 0) _signalBus.Fire<AllPassengersRemovedSignal>();
            }
        }

        private void CreatePassengersFromLevelData(LevelData levelData)
        {
            ClearAllPassengers();
            _benchController.ClearQueue();

            var initialPassengers = levelData?.GetInitialPassengers();
            if (initialPassengers == null)
                return;

            foreach (var passengerData in initialPassengers)
                CreatePassenger(passengerData.color, passengerData.gridPosition);
        }

        private void CreatePassenger(PassengerColor color, Vector2Int gridPosition)
        {
            if (_passengerGrid.ContainsKey(gridPosition))
                return;

            if (!_gridController.IsValidPosition(gridPosition))
                return;

            GameObject passengerGO;

            if (passengerPrefab != null)
            {
                passengerGO = Instantiate(passengerPrefab, passengerParent);
            }
            else
            {
                passengerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                passengerGO.transform.SetParent(passengerParent);
                passengerGO.AddComponent<PassengerView>();
            }

            var passengerView = passengerGO.GetComponent<PassengerView>();
            if (passengerView != null)
            {
                _container.Inject(passengerView);

                var passengerModel = new PassengerModel(color, gridPosition);
                passengerView.Initialize(passengerModel);

                var worldPosition = _gridController.GridToWorldPosition(gridPosition);
                passengerGO.transform.position = worldPosition;

                _allPassengers.Add(passengerView);
                _passengerGrid[gridPosition] = passengerView;

                _gridController.SetCellState(gridPosition, false);

                _signalBus.Fire(new PassengerCreatedSignal(passengerGO, color, gridPosition));
            }
        }

        public bool MovePassenger(PassengerView passengerView, Vector2Int newGridPosition)
        {
            if (passengerView == null || !_allPassengers.Contains(passengerView))
                return false;

            if (!_gridController.IsValidPosition(newGridPosition))
                return false;

            if (_passengerGrid.ContainsKey(newGridPosition) && _passengerGrid[newGridPosition] != passengerView)
                return false;

            var model = passengerView.GetModel();
            var oldGridPosition = model.GridPosition;

            if (_passengerGrid.ContainsKey(oldGridPosition) && _passengerGrid[oldGridPosition] == passengerView)
            {
                _passengerGrid.Remove(oldGridPosition);
                _gridController.SetCellState(oldGridPosition, true);
            }

            model.SetGridPosition(newGridPosition);
            _passengerGrid[newGridPosition] = passengerView;
            _gridController.SetCellState(newGridPosition, false);

            var worldPosition = _gridController.GridToWorldPosition(newGridPosition);
            passengerView.MoveTo(worldPosition);

            _signalBus.Fire(new PassengerMovedSignal(passengerView.gameObject, oldGridPosition, newGridPosition));

            return true;
        }

        public PassengerView GetPassengerAt(Vector2Int gridPosition)
        {
            _passengerGrid.TryGetValue(gridPosition, out var passenger);
            return passenger;
        }

        public List<PassengerView> GetPassengersOfColor(PassengerColor color)
        {
            var result = new List<PassengerView>();
            foreach (var passenger in _allPassengers)
                if (passenger.GetModel().Color == color)
                    result.Add(passenger);

            return result;
        }

        private void ClearAllPassengers()
        {
            _selectedPassenger = null;

            foreach (var passenger in _allPassengers)
                if (passenger != null && passenger.gameObject != null)
                {
                    passenger.transform.DOKill();
                    DestroyImmediate(passenger.gameObject);
                }

            _allPassengers.Clear();
            _passengerGrid.Clear();
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<LevelLoadedSignal>(OnLevelLoaded);
            _signalBus?.TryUnsubscribe<PassengerClickedSignal>(OnPassengerClicked);
            _signalBus?.TryUnsubscribe<PassengerRemovedSignal>(OnPassengerRemoved);
        }
    }
}