using System.Collections.Generic;
using BusJam.Core;
using BusJam.Events;
using BusJam.MVC.Models;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace BusJam.MVC.Views
{
    public class PassengerView : MonoBehaviour
    {
        [SerializeField] private Renderer passengerRenderer;
        [SerializeField] private Renderer outlineRenderer;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float selectionScaleMultiplier = 1.1f;
        
        private GameConfig gameConfig;
        private PassengerModel model;
        private SignalBus signalBus;
        
        private Material originalMaterial;
        private Material outlineMaterial;
        private Vector3 originalScale;
        private bool isAnimating;
        private PassengerAnimationController _animationController;

        private void Awake()
        {
            if (passengerRenderer == null)
                passengerRenderer = GetComponent<Renderer>();

            if (passengerRenderer == null)
                passengerRenderer = GetComponentInChildren<Renderer>();

            if (outlineRenderer == null)
            {
                var outlineGO = new GameObject("Outline");
                outlineGO.transform.SetParent(transform);
                outlineGO.transform.localPosition = Vector3.zero;
                outlineGO.transform.localScale = Vector3.one * 1.05f;
                
                outlineRenderer = outlineGO.AddComponent<MeshRenderer>();
                var meshFilter = outlineGO.AddComponent<MeshFilter>();
                
                if (passengerRenderer != null)
                {
                    var originalMeshFilter = passengerRenderer.GetComponent<MeshFilter>();
                    if (originalMeshFilter != null)
                        meshFilter.mesh = originalMeshFilter.mesh;
                }
            }

            originalScale = transform.localScale;
            SetupMaterials();
            SetupAnimationController();
        }

        private void SetupMaterials()
        {
            if (passengerRenderer != null)
            {
                originalMaterial = passengerRenderer.material;
            }

            CreateOutlineMaterial();
            
            if (outlineRenderer != null)
            {
                outlineRenderer.material = outlineMaterial;
                outlineRenderer.enabled = false;
            }
        }

        private void CreateOutlineMaterial()
        {
            outlineMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = Color.yellow
            };
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
            
            if (outlineMaterial != null)
                DestroyImmediate(outlineMaterial);
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

        public void SetSelected(bool selected)
        {
            if (model != null)
                model.SetSelected(selected);

            if (outlineRenderer != null)
                outlineRenderer.enabled = selected;

            if (selected)
                PlaySelectionAnimation();
            else
                StopSelectionAnimation();
        }

        public void SetDragging(bool dragging)
        {
            model.SetDragging(dragging);
        }

        private void PlaySelectionAnimation()
        {
            transform.DOKill();
            transform.DOScale(originalScale * selectionScaleMultiplier, 0.2f).SetLoops(-1, LoopType.Yoyo);
        }

        private void StopSelectionAnimation()
        {
            transform.DOKill();
            transform.DOScale(originalScale, 0.1f);
        }

        public void PlayPickupAnimation()
        {
            transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);
        }

        public PassengerModel GetModel()
        {
            return model;
        }

        public void ShowValidMoveHint(bool show)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = show;
                if (show && outlineMaterial != null)
                {
                    outlineMaterial.color = Color.green;
                }
            }
        }

        public void ShowInvalidMoveHint(bool show)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = show;
                if (show && outlineMaterial != null)
                {
                    outlineMaterial.color = Color.red;
                }
            }
        }

        public void ResetOutlineColor()
        {
            if (outlineMaterial != null)
            {
                outlineMaterial.color = Color.yellow;
            }
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
    }
}