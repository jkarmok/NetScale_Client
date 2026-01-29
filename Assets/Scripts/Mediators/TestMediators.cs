using System;
using Gateway;
using Infrastructure;
using NetScaleCommon.Utility;

namespace Mediators
{
    public class TestMediators : IDisposable
    {
        private readonly GameLoop _gameLoop;
        private readonly IntervalTimer _intervalTimer;
        private readonly TestGateway _gateway;
       
        public TestMediators(GameLoop gameLoop, TestGateway gateway)
        {
            _gameLoop = gameLoop;
            _gateway = gateway;

            _intervalTimer = new IntervalTimer(1f, OnUpdated);
            _gameLoop.Updated += _intervalTimer.Update;
        }

        private void OnUpdated(float obj)
        {
            _gateway.Publish();
        }

        public void Dispose()
        {
            _gameLoop.Updated -= _intervalTimer.Update;
        }
    }
}