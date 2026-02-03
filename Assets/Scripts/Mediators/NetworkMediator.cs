using System;
using Infrastructure;
using NetScaleCommon.Utility;
using Network;
using UI.ViewModels;
using UnityEngine;

namespace Mediators
{
    public class NetworkMediator : IDisposable
    {
        private readonly NetworkStatisticViewModel _networkStatisticViewModel;
        private readonly ClientToProxy _clientToProxy;
        private readonly GameLoop _gameLoop;
        private readonly IntervalTimer _intervalTimer;
        
        public NetworkMediator(NetworkStatisticViewModel networkStatisticViewModel, ClientToProxy clientToProxy, GameLoop gameLoop)
        {
            _networkStatisticViewModel = networkStatisticViewModel;
            _clientToProxy = clientToProxy;
            _gameLoop = gameLoop;
            _intervalTimer = new IntervalTimer(1, OnUpdated);
            gameLoop.Updated += _intervalTimer.Update;
        }

        private void OnUpdated(float deltaTime)
        {
            if (_clientToProxy.TryGetStatistics(out long packetsSent, out long packetsReceived, out var bytesSent,
                    out var bytesReceived))
            {
                _networkStatisticViewModel.UpdateStatistics(packetsSent, packetsReceived, bytesSent, bytesReceived);
            }
        }

        public void Dispose()
        {
            _gameLoop.Updated -= _intervalTimer.Update;
        }

        public void Start()
        {
            
        }
    }
}