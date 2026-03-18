using FischlWorks_FogWar;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Installers
{
    public class GameSceneLifetimeScope : LifetimeScope
    {
        [SerializeField] private csFogWar _fogWar;

        protected override void Configure(IContainerBuilder builder)
        {

            builder.RegisterInstance(_fogWar);
            builder.RegisterEntryPoint<FOVInstaller>();
        }
    }
}
