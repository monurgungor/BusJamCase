using UnityEngine;
using DG.Tweening;

namespace BusJam.MVC.Views
{
    public class PassengerAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float rotationSpeed = 5f;
        
        private const string IsMovingParameter = "IsMoving";
        private bool _isMoving;
        private Vector3 _lastPosition;
        private Vector3 _currentDirection;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (animator == null)
                animator = GetComponent<Animator>();
            _lastPosition = transform.position;
        }

        private void Start()
        {
            SetForwardRotation();
            SetMoving(false);
        }

        public void SetForwardRotation()
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        public void SetMoving(bool moving)
        {
            if (_isMoving == moving) return;
            _isMoving = moving;
            if (animator != null && animator.runtimeAnimatorController != null && HasBoolParameter(IsMovingParameter))
            {
                animator.SetBool(IsMovingParameter, moving);
            }
        }

        private bool HasBoolParameter(string paramName)
        {
            if (animator == null) return false;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == paramName && parameter.type == AnimatorControllerParameterType.Bool)
                    return true;
            }
            return false;
        }

        public void SetMovementDirection(Vector3 direction)
        {
            if (direction != Vector3.zero)
            {
                _currentDirection = direction.normalized;
                RotateTowardsDirection(_currentDirection);
            }
        }

        private void RotateTowardsDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return;
            var flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;
            var targetRotation = Quaternion.LookRotation(flatDirection);
            transform.DORotateQuaternion(targetRotation, 1f / rotationSpeed);
        }

        public bool IsMoving => _isMoving;
        public void ForceIdle() => SetMoving(false);
        public void ForceRun() => SetMoving(true);

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}