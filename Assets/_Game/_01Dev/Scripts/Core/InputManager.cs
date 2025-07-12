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
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            Vector3 inputPosition = Vector3.zero;
            bool hasInput = false;

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
            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var clickedObject = hit.collider.gameObject;
                
                var passengerView = clickedObject.GetComponent<PassengerView>();
                if (passengerView != null)
                {
                    var model = passengerView.GetModel();
                    if (model != null && model.CanInteract)
                    {
                        _signalBus.Fire(new PassengerClickedSignal(clickedObject, model.GridPosition));
                    }
                    return;
                }

                var gridCellView = clickedObject.GetComponent<GridCellView>();
                if (gridCellView != null)
                {
                    _signalBus.Fire(new GridCellClickedSignal(gridCellView.GridPosition, gridCellView.WorldPosition));
                    return;
                }
            }
        }
    }
}