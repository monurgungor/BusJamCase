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
        private GameStateManager _gameStateManager;

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
            _gameStateManager = gameStateManager;
        }

        private void Update()
        {
            HandleInput();
            HandleDebugInput();
        }
        
        private void HandleDebugInput()
        {
            if (_gameStateManager == null) return;

            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_gameStateManager.CurrentState == GameState.MainMenu)
                {
                    _signalBus.Fire(new LoadLevelRequestedSignal(0));
                }
                else if (_gameStateManager.CurrentState == GameState.LevelComplete || _gameStateManager.CurrentState == GameState.LevelFailed)
                {
                    _signalBus.Fire<RestartLevelRequestedSignal>();
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_gameStateManager.CurrentState == GameState.Playing)
                {
                    _signalBus.Fire<PauseGameRequestedSignal>();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                if (_gameStateManager.CurrentState == GameState.Playing)
                {
                    _signalBus.Fire<ResumeGameRequestedSignal>();
                }
            }

            if (_gameStateManager.CurrentState == GameState.MainMenu)
            {
                for (var i = 0; i < 10; i++)
                {
                    if (!Input.GetKeyDown(KeyCode.Alpha0 + i)) continue;
                    
                    var levelIndex = i == 0 ? 9 : i - 1;
                    _signalBus.Fire(new LoadLevelRequestedSignal(levelIndex));
                    break;
                }
            }
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