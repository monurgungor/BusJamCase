using System.Collections.Generic;
using BusJam.Core;
using BusJam.MVC.Models;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class PassengerView : MonoBehaviour
    {
        [SerializeField] private Renderer passengerRenderer;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float selectionScaleMultiplier = 1.1f;
        
        private GameConfig gameConfig;
        private PassengerModel model;
        private SignalBus signalBus;
        
        private Material originalMaterial;
        private Vector3 originalScale;
        private bool isAnimating;
        private PassengerAnimationController _animationController;
        [SerializeField] private Outline outline;

        private void Awake()
        {
            if (passengerRenderer == null)
                passengerRenderer = GetComponent<Renderer>();

            if (passengerRenderer == null)
                passengerRenderer = GetComponentInChildren<Renderer>();

            originalScale = transform.localScale;
            SetupMaterials();
            SetupAnimationController();
            if (outline != null)
                outline.enabled = false;
        }

        private void SetupMaterials()
        {
            if (passengerRenderer != null)
            {
                originalMaterial = passengerRenderer.material;
            }
        }

        private void SetupAnimationController()
        {
            _animationController = GetComponent<PassengerAnimationController>();
            if (_animationController == null)
                _animationController = GetComponentInChildren<PassengerAnimationController>();
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }


        [Inject]
        public void Construct(SignalBus signalBus, GameConfig gameConfig)
        {
            this.signalBus = signalBus;
            this.gameConfig = gameConfig;
        }

        public void Initialize(PassengerModel passengerModel)
        {
            model = passengerModel;
            SetupMaterialAndColor();
            name = $"Passenger_{model.Color}_{model.GridPosition.x}_{model.GridPosition.y}";
            if (_animationController != null)
            {
                _animationController.SetForwardRotation();
            }
        }
        
        private void SetupMaterialAndColor()
        {
            if (passengerRenderer != null && gameConfig != null)
            {
                var passengerColor = gameConfig.GetPassengerColor(model.Color);
                passengerRenderer.material.color = passengerColor;
            }
        }

        public void MoveToPosition(Vector3 destination, System.Action onComplete = null)
        {
            if (isAnimating) return;

            isAnimating = true;
            var distance = Vector3.Distance(transform.position, destination);
            var duration = distance / moveSpeed;

            if (_animationController != null)
            {
                var direction = (destination - transform.position).normalized;
                _animationController.SetMovementDirection(direction);
                _animationController.SetMoving(true);
            }

            transform.DOMove(destination, duration)
                .SetEase(moveCurve)
                .OnComplete(() =>
                {
                    isAnimating = false;
                    if (_animationController != null)
                        _animationController.SetMoving(false);
                    model?.CompleteMovement();
                    onComplete?.Invoke();
                });
        }

        public void MoveToGrid(Vector2Int gridPosition, Vector3 worldPosition, System.Action onComplete = null)
        {
            if (model != null)
            {
                model.StartMovement(gridPosition);
            }
            
            MoveToPosition(worldPosition, onComplete);
        }

        public void MoveOffGrid(Vector3 destination, System.Action onComplete = null)
        {
            if (model != null)
            {
                model.SetState(PassengerState.Moving);
            }
            
            MoveToPosition(destination, onComplete);
        }

        public void MoveAlongPath(List<Vector3> worldPath, System.Action onComplete = null)
        {
            if (isAnimating || worldPath.Count == 0) return;

            if (model != null)
            {
                model.SetState(PassengerState.Moving);
            }

            isAnimating = true;
            
            if (_animationController != null)
                _animationController.SetMoving(true);

            var sequence = DOTween.Sequence();

            for (var i = 0; i < worldPath.Count; i++)
            {
                var distance = i == 0 ? Vector3.Distance(transform.position, worldPath[i]) : Vector3.Distance(worldPath[i-1], worldPath[i]);
                var duration = distance / moveSpeed;
                
                var targetPos = worldPath[i];
                var currentPos = i == 0 ? transform.position : worldPath[i-1];
                var direction = (targetPos - currentPos).normalized;
                
                sequence.AppendCallback(() => {
                    if (_animationController != null)
                        _animationController.SetMovementDirection(direction);
                });
                
                sequence.Append(transform.DOMove(worldPath[i], duration).SetEase(moveCurve));
            }

            sequence.OnComplete(() =>
            {
                isAnimating = false;
                
                if (_animationController != null)
                    _animationController.SetMoving(false);
                
                if (model != null)
                {
                    model.CompleteMovement();
                }
                onComplete?.Invoke();
            });
        }

        public void SetDragging(bool dragging)
        {
            model.SetDragging(dragging);
        }

        public void PlayPickupAnimation()
        {
            transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }

        public PassengerModel GetModel()
        {
            return model;
        }

        public bool IsMoving()
        {
            return isAnimating || (model != null && model.IsMoving);
        }

        public void PlayErrorAnimation()
        {
            transform.DOShakePosition(0.3f, 0.1f, 10, 90f);
        }

        public void SetInteractable(bool interactable)
        {
            if (model != null)
            {
                model.SetState(interactable ? PassengerState.OnGrid : PassengerState.Moving);
            }
        }

        public void ResetToForwardRotation()
        {
            if (_animationController != null)
            {
                _animationController.SetForwardRotation();
            }
        }

        public void EnableOutline()
        {
            if (outline != null)
                outline.enabled = true;
        }

        public void DisableOutline()
        {
            if (outline != null)
                outline.enabled = false;
        }
    }
}