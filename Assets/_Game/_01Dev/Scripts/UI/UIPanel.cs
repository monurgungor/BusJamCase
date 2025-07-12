using DG.Tweening;
using UnityEngine;

namespace BusJam.UI
{
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private float animationDuration = 0.3f;

        protected bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            if (canvas == null)
                canvas = GetComponent<Canvas>();
            

            InitializePanel();
        }

        protected virtual void InitializePanel()
        {
            if (gameObject.name != "Canvas - MainMenu")
            {
                gameObject.SetActive(false);
                IsVisible = false;
            }
            else
            {
                IsVisible = true;
            }
        }

        public void Show(bool animate = true)
        {
            if (IsVisible) return;

            IsVisible = true;
            gameObject.SetActive(true);

            if (animate)
            {
                transform.localScale = Vector3.zero;
                transform.DOScale(Vector3.one, animationDuration).SetEase(Ease.OutBack);
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            OnShow();
        }

        public virtual void Hide(bool animate = true)
        {
            if (!IsVisible) return;

            IsVisible = false;

            if (animate)
            {
                transform.DOScale(Vector3.zero, animationDuration).SetEase(Ease.InBack)
                    .OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                transform.localScale = Vector3.zero;
                gameObject.SetActive(false);
            }

            OnHide();
        }

        protected virtual void OnShow() { }
        private void OnHide() { }

        protected virtual void OnDestroy()
        {
            transform.DOKill();
        }
    }
}