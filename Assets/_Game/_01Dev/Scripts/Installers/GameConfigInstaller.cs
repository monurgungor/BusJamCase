using BusJam.Core;
using UnityEngine;
using Zenject;

namespace BusJam.Installers
{
    [CreateAssetMenu(fileName = "GameConfigInstaller", menuName = "Installers/GameConfigInstaller")]
    public class GameConfigInstaller : ScriptableObjectInstaller<GameConfigInstaller>
    {
        [SerializeField] private GameConfig gameConfig;

        public override void InstallBindings()
        {
            Container.BindInstance(gameConfig).AsSingle();
        }
    }
}