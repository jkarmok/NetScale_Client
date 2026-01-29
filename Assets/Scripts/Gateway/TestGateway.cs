
using System;
using Game.Enums2;
using NetScaleCommon;
using Network;
using UnityEngine;

namespace Gateway
{
    public class TestGateway : IDisposable
    {
        private readonly ClientToProxy _clientToProxy;
        public TestGateway(ClientToProxy connection)
        {
            _clientToProxy = connection;
            _clientToProxy.Listen<string>((ushort)RpcTypes.Test, OnTimeHandle);
        }

 
        public void Publish()
        {
            _clientToProxy.Publish(RpcTypes.Test, DeliveryMethod.UnreliableSequenced, "Hello from client!");
        }

        private void OnTimeHandle(in string message)
        {
            Debug.Log($"OnTimeHandle:{message}");
        }

        public void Dispose()
        {
        
        }
    }
}