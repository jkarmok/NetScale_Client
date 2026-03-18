using System;
using FischlWorks_FogWar;
using Game.Scene;
using Scene;
using VContainer.Unity;

namespace Installers
{
    public class FOVInstaller : IStartable, IDisposable
    {
        private const int Radius = 30; 
        private csFogWar _fogWar;
        private readonly BatchSceneApplier _batchSceneApplier;
 
        private csFogWar.FogRevealer _fogRevealer;

        public FOVInstaller(BatchSceneApplier batchSceneApplier, csFogWar fogWar)
        {
            _batchSceneApplier = batchSceneApplier;
            _fogWar = fogWar;
        }
        
        private void EntityRemoved(NetworkEntity view, bool owned)
        {
            if (!owned)
            {
                return;
            }
            
            if (!view)
            {
                return;
            }
            _fogWar._FogRevealers.Remove(_fogRevealer);
        }

        private void OnEntityAdded(NetworkEntity view, EntityDto entityDto, bool owned)
        {
            if (!owned)
            {
                return;
            }
            
            _fogRevealer =  new csFogWar.FogRevealer(view.transform, Radius, true);
            
            _fogWar._FogRevealers.Add(_fogRevealer);
        }

        public void Dispose()
        {
            _batchSceneApplier.OnEntityAdded -= OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved -= EntityRemoved;
        }

        public void Start()
        {
            _batchSceneApplier.OnEntityAdded += OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved += EntityRemoved;
        }
    }
}