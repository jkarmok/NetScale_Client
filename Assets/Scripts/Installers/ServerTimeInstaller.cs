using System;
using Gateway;
using Scene;
using VContainer.Unity;

namespace Installers
{
    public class ServerTimeInstaller : IDisposable, IStartable
    {
        private readonly TimeGateway _gateway;
        private NetworkTime _networkTime;

        public ServerTimeInstaller(TimeGateway gateway, NetworkTime networkTime)
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