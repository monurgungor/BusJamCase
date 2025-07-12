using BusJam.Core;
using BusJam.Events;
using BusJam.MVC.Controllers;
using UnityEngine;
using Zenject;

namespace BusJam.Installers
{
    public class GameLogicInstaller : MonoInstaller
    {
        [SerializeField] private PoolingInstaller poolingInstaller;

        public override void InstallBindings()
        {
            InstallSignals();
            InstallPooling();
            InstallManagers();
        }

        private void InstallPooling()
        {
            if (poolingInstaller != null)
            {
                poolingInstaller.InstallBindings(Container);
            }
            else
            {
                Debug.LogWarning("PoolingInstaller not assigned in GameLogicInstaller");
            }
        }

        private void InstallSignals()
        {
            SignalBusInstaller.Install(Container);

            Container.DeclareSignal<LevelLoadedSignal>();
            Container.DeclareSignal<LevelStartedSignal>();
            Container.DeclareSignal<LevelCompletedSignal>();
            Container.DeclareSignal<LevelFailedSignal>();
            Container.DeclareSignal<GamePausedSignal>();
            Container.DeclareSignal<GameResumedSignal>();

            Container.DeclareSignal<PassengerCreatedSignal>().OptionalSubscriber();
            Container.DeclareSignal<PassengerClickedSignal>();
            Container.DeclareSignal<PassengerMovedSignal>().OptionalSubscriber();
            Container.DeclareSignal<PassengerRemovedSignal>();

            Container.DeclareSignal<BusSpawnedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BusArrivedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BusLoadedSignal>().OptionalSubscriber();
            Container.DeclareSignal<BusDepartedSignal>();

            Container.DeclareSignal<GridCellClickedSignal>().OptionalSubscriber();
            Container.DeclareSignal<AllBusesCompletedSignal>().OptionalSubscriber();
            Container.DeclareSignal<AllPassengersRemovedSignal>().OptionalSubscriber();

            Container.DeclareSignal<GameStateChangedSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnterMainMenuSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnterLevelSelectSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnterPlayingSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnterLevelCompleteSignal>().OptionalSubscriber();
            Container.DeclareSignal<EnterLevelFailedSignal>().OptionalSubscriber();

            Container.DeclareSignal<TimerUpdatedSignal>().OptionalSubscriber();
            Container.DeclareSignal<TimerExpiredSignal>().OptionalSubscriber();
            Container.DeclareSignal<TimerStartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<TimerStoppedSignal>().OptionalSubscriber();
            Container.DeclareSignal<TimerPausedSignal>().OptionalSubscriber();
            Container.DeclareSignal<TimerResumedSignal>().OptionalSubscriber();

            Container.DeclareSignal<LevelChangedSignal>().OptionalSubscriber();
            Container.DeclareSignal<LevelRestartedSignal>().OptionalSubscriber();
            Container.DeclareSignal<NextLevelAvailableSignal>().OptionalSubscriber();
            Container.DeclareSignal<AllLevelsCompletedSignal>().OptionalSubscriber();
        }

        private void InstallManagers()
        {
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                Container.BindInstance(gameManager).AsSingle();
                Container.QueueForInject(gameManager);
            }

            var gridController = FindObjectOfType<GridController>();
            if (gridController != null)
            {
                Container.BindInterfacesAndSelfTo<GridController>().FromInstance(gridController).AsSingle();
                Container.QueueForInject(gridController);
            }

            var busController = FindObjectOfType<BusController>();
            if (busController != null)
            {
                Container.BindInterfacesAndSelfTo<BusController>().FromInstance(busController).AsSingle();
                Container.QueueForInject(busController);
            }

            var passengerController = FindObjectOfType<PassengerController>();
            if (passengerController != null)
            {
                Container.BindInterfacesAndSelfTo<PassengerController>().FromInstance(passengerController).AsSingle();
                Container.QueueForInject(passengerController);
            }

            var benchController = FindObjectOfType<BenchController>();
            if (benchController != null)
            {
                Container.BindInterfacesAndSelfTo<BenchController>().FromInstance(benchController).AsSingle();
                Container.QueueForInject(benchController);
            }

            var gameStateManager = FindObjectOfType<GameStateManager>();
            if (gameStateManager != null)
            {
                Container.BindInterfacesAndSelfTo<GameStateManager>().FromInstance(gameStateManager).AsSingle();
                Container.QueueForInject(gameStateManager);
            }

            var levelTimer = FindObjectOfType<LevelTimer>();
            if (levelTimer != null)
            {
                Container.BindInterfacesAndSelfTo<LevelTimer>().FromInstance(levelTimer).AsSingle();
                Container.QueueForInject(levelTimer);
            }

            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
            {
                Container.BindInterfacesAndSelfTo<LevelManager>().FromInstance(levelManager).AsSingle();
                Container.QueueForInject(levelManager);
            }

            var inputManager = FindObjectOfType<InputManager>();
            if (inputManager != null)
            {
                Container.BindInterfacesAndSelfTo<InputManager>().FromInstance(inputManager).AsSingle();
                Container.QueueForInject(inputManager);
            }
        }
    }
}