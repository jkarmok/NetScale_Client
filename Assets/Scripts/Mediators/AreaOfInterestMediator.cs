using System;
using Gateway;
using Infrastructure;
using NetScaleCommon.Utility;
using Scene;
using UI.ViewModels;

namespace Mediators
{
    public class AreaOfInterestMediator : IDisposable
    {
        private readonly BatchSceneApplier _batchSceneApplier;
        private readonly AreaOfInterestGateway _gateway;
        private readonly AreaOfInterestViewModel _areaOfInterestViewModel;
        private readonly IntervalTimer _statsInterval;

        private readonly GameLoop _gameLoop;

        public AreaOfInterestMediator(BatchSceneApplier batchSceneApplier, AreaOfInterestGateway gateway, AreaOfInterestViewModel areaOfInterestViewModel, GameLoop gameLoop)
        {
            _batchSceneApplier = batchSceneApplier;
            _gateway = gateway;
            _areaOfInterestViewModel = areaOfInterestViewModel;
            _gameLoop = gameLoop;
            _gateway.EntityLifecycleUpdate += batchSceneApplier.ApplyLifecycle;
            _gateway.TransformUpdate += batchSceneApplier.ApplyTransforms;
 
            _statsInterval = new IntervalTimer(0.1f, OnUpdated);
            _gameLoop.Updated += _statsInterval.Update;
        }

        private void OnUpdated(float deltaTime)
        {
            _batchSceneApplier.GetStats(out var activeEntities, out var totalSpawns, out var totalDespawns);
           _areaOfInterestViewModel.UpdateStats(activeEntities);
        }


        public void Dispose()
        {
            _gameLoop.Updated -= _statsInterval.Update;
            _gateway.EntityLifecycleUpdate -= _batchSceneApplier.ApplyLifecycle;
            _gateway.TransformUpdate -= _batchSceneApplier.ApplyTransforms;
        }
    }
}