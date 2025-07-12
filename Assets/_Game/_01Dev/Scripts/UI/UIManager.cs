using BusJam.Events;
using UnityEngine;
using Zenject;

namespace BusJam.UI
{
    public class UIManager : MonoBehaviour, IInitializable
    {
        [SerializeField] private MainMenuPanel mainMenuPanel;
        [SerializeField] private GameUIPanel gameUIPanel;
        [SerializeField] private LevelEndPanel levelEndPanel;

        private SignalBus _signalBus;
        private UIPanel _currentPanel;

        public static UIManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
        {
            SubscribeToEvents();
            ShowMainMenu();
        }

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void SubscribeToEvents()
        {
            _signalBus.Subscribe<GameStateChangedSignal>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedSignal signal)
        {
            switch (signal.NewState)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                case GameState.Playing:
                    ShowGameUI();
                    break;
                case GameState.LevelComplete:
                    ShowLevelEnd(true);
                    break;
                case GameState.LevelFailed:
                    ShowLevelEnd(false);
                    break;
            }
        }

        public void ShowMainMenu()
        {
            SwitchPanel(mainMenuPanel);
        }

        public void ShowGameUI()
        {
            SwitchPanel(gameUIPanel);
        }

        public void ShowLevelEnd(bool success)
        {
            if (levelEndPanel != null)
            {
                levelEndPanel.SetSuccess(success);
            }
            SwitchPanel(levelEndPanel);
        }

        private void SwitchPanel(UIPanel targetPanel)
        {
            if (_currentPanel != null)
            {
                _currentPanel.Hide();
            }

            _currentPanel = targetPanel;

            if (_currentPanel != null)
            {
                _currentPanel.Show();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (Instance == this) Instance = null;
        }

        private void UnsubscribeFromEvents()
        {
            _signalBus?.TryUnsubscribe<GameStateChangedSignal>(OnGameStateChanged);
        }
    }
}