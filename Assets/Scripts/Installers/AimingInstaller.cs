using System;
using Aiming;
using Game.Scene;
using NetScaleClient.Camera;
using Scene;
using VContainer.Unity;

namespace Installers
{
    public class AimingInstaller : IStartable, IDisposable
    {
        private readonly AimingStrategySelector _strategySelector;
        private readonly BatchSceneApplier _batchSceneApplier;

        public AimingInstaller(AimingStrategySelector strategySelector, BatchSceneApplier batchSceneApplier)
        {
            _strategySelector = strategySelector;
            _batchSceneApplier = batchSceneApplier;
        }

        private void EntityRemoved(NetworkEntity view, bool owned)
        {
            var cameraTarget = view.GetComponent<ICameraTarget>();

            if (cameraTarget == null)
                return;

            if (false == cameraTarget.Owned)
                return;

        }

        private void OnEntityAdded(NetworkEntity view, EntityDto entityDto, bool owned)
        {
            var cameraTarget = view.GetComponent<ICameraTarget>();

            if (cameraTarget == null)
                return;

            if (false == owned)
                return;

            _strategySelector.SetupStrategy(view.transform);
        }

        private void OnEntityUpdated(NetworkEntity view, EntityDto entityDto, bool owned)
        {
            var cameraTarget = view.GetComponent<ICameraTarget>();

            if (cameraTarget == null)
                return;

            if (false == owned)
                return;

            _strategySelector.SetupStrategy(view.transform);
        }
        
        public void Dispose()
        {
            _batchSceneApplier.OnEntityAdded -= OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved -= EntityRemoved;
            _batchSceneApplier.OnEntityUpdated -= OnEntityUpdated;
        }
 
        public void Start()
        {
            _batchSceneApplier.OnEntityAdded += OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved += EntityRemoved;
            _batchSceneApplier.OnEntityUpdated += OnEntityUpdated;
        }
    }
}