using BusJam.UI;
using UnityEngine;
using Zenject;

namespace BusJam.Installers
{
    public class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            InstallUIManager();
        }

        private void InstallUIManager()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                Container.BindInterfacesAndSelfTo<UIManager>().FromInstance(uiManager).AsSingle();
                Container.QueueForInject(uiManager);
            }
        }
    }
}