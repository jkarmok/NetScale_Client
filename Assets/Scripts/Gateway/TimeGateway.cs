using System;
using System.Threading.Tasks;
using Game.Enums2;
using NetScaleCommon;
using Network;
using UnityEngine;

namespace Gateway
{
    public class TimeGateway : IDisposable
    {
        private readonly ClientToProxy _clientToProxy;
        private Action<double> _connectionOpened;

        public TimeGateway(ClientToProxy connection)
        {
            _clientToProxy = connection;
            _clientToProxy.Connected += OnConnected;
        }
        private async void OnConnected(ClientToProxy obj)
        {   
            var serverTime =  await _clientToProxy.AskAsync<double, double>((ushort)RpcTypes.ServerTime, DeliveryMethod.ReliableOrdered, 0, TimeSpan.FromSeconds(10));
            _connectionOpened?.Invoke(serverTime);
        }

        public void ReceiveServerTime(Action<double> callback)
        {
            _connectionOpened = callback;
        }
 
        public void Dispose()
        {
            _clientToProxy.Connected -= OnConnected;
        }
    }
}