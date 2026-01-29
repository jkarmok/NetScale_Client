using System;
using Gateway;
using Scene;

namespace Mediators
{
    public class TimeMediator : IDisposable
    {
        private readonly TimeGateway _gateway;
        private NetworkTime _networkTime;

        public TimeMediator(TimeGateway gateway, NetworkTime networkTime)
        {
            _gateway = gateway;
            _networkTime = networkTime;
            _gateway.ReceiveServerTime(_networkTime.SyncWithServer);
        }
 
        public void Dispose()
        {

        }

        public void Start()
        {
          
        }
    }
}