using System;
using Gateway;
using Infrastructure;
using NetScaleCommon.Utility;
using UI.Screens;

namespace Mediators
{
    public class HudMediator : IDisposable
    {
        private readonly IntervalTimer _intervalTimer;

        private readonly HudScreen hudScreen;
        private readonly InputGateway _inputGateway;
        private readonly GameLoop _gameLoop;

        public HudMediator(HudScreen hudScreen, InputGateway inputGateway, GameLoop gameLoop)
        {
            this.hudScreen = hudScreen;
            _inputGateway = inputGateway;
            _gameLoop = gameLoop;
            _gameLoop.Updated += OnUpdated;
        }

        private void OnUpdated(float obj)
        {
            _inputGateway.Publish(hudScreen.MovmentDirection);
        }

        public void Dispose()
        {
            _gameLoop.Updated -= OnUpdated;
        }
    }
}