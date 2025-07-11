using BusJam.Data;
using UnityEngine;
using Zenject;

namespace BusJam.Installers
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField] private LevelData levelData;

        public override void InstallBindings()
        {
            if (levelData != null) Container.BindInstance(levelData).AsSingle();
        }
    }
}