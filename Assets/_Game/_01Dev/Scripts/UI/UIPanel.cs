using UnityEngine;

namespace BusJam.UI
{
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;

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

        public void Show()
        {
            if (IsVisible) return;

            IsVisible = true;
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;

            OnShow();
        }

        public void Hide()
        {
            if (!IsVisible) return;

            IsVisible = false;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);

            OnHide();
        }

        protected virtual void OnShow() { }
        private void OnHide() { }

        protected virtual void OnDestroy()
        {
        }
    }
}