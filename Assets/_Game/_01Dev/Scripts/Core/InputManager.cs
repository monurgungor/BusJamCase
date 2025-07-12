using BusJam.Events;
using BusJam.MVC.Views;
using UnityEngine;
using Zenject;

namespace BusJam.Core
{
    public class InputManager : MonoBehaviour, IInitializable
    {
        private SignalBus _signalBus;
        private Camera _mainCamera;

        public void Initialize()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }
        }

        [Inject]
        public void Construct(SignalBus signalBus, GameStateManager gameStateManager)
        {
            _signalBus = signalBus;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            var inputPosition = Vector3.zero;
            var hasInput = false;

            if (Input.GetMouseButtonDown(0))
            {
                hasInput = true;
                inputPosition = Input.mousePosition;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                hasInput = true;
                inputPosition = Input.GetTouch(0).position;
            }

            if (hasInput && _mainCamera != null)
            {
                ProcessClick(inputPosition);
            }
        }

        private void ProcessClick(Vector3 screenPosition)
        {
            var ray = _mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit))
            {
                var clickedObject = hit.collider.gameObject;
                
                var passengerView = clickedObject.GetComponent<PassengerView>();
                if (passengerView != null)
                {
                    var model = passengerView.GetModel();
                    if (model is { CanInteract: true })
                    {
                        _signalBus.Fire(new PassengerClickedSignal(clickedObject, model.GridPosition));
                    }
                    return;
                }

                var gridCellView = clickedObject.GetComponent<GridCellView>();
                if (gridCellView != null)
                {
                    _signalBus.Fire(new GridCellClickedSignal(gridCellView.GridPosition, gridCellView.WorldPosition));
                }
            }
        }
    }
}