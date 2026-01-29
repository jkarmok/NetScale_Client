using System;
using Mediators;
using Network;
using UI.Screens;
using VContainer.Unity;

namespace Installers
{
    public class NetworkStartup : IStartable, IDisposable
    {
        private readonly SplashScreen splashScreen;
        private readonly LoginScreen loginScreen;
        private readonly ClientToProxy _clientToProxy;
        private readonly TestMediators _testMediators;
        private readonly AreaOfInterestMediator _areaOfInterestGateway;
        private readonly ConnectionMediator _connectionMediator;
        private readonly HudMediator _hudMediator;
        private readonly TimeMediator _timeMediator;
        private readonly NetworkMediator _networkMediator;


        public NetworkStartup(
            SplashScreen splashScreen, LoginScreen loginScreen, ClientToProxy clientToProxy, 
            TestMediators testMediators, AreaOfInterestMediator areaOfInterestGateway,
            ConnectionMediator connectionMediator, HudMediator hudMediator, TimeMediator timeMediator, NetworkMediator networkMediator)
        {
            this.splashScreen = splashScreen;
            this.loginScreen = loginScreen;
            _clientToProxy = clientToProxy;
            _testMediators = testMediators;
            _areaOfInterestGateway = areaOfInterestGateway;
            _connectionMediator = connectionMediator;
            _hudMediator = hudMediator;
            _timeMediator = timeMediator;
            _networkMediator = networkMediator;
        }

        public void Start()
        {
            _connectionMediator.Start();
            _timeMediator.Start();
            _networkMediator.Start();
        }
 
        public void Dispose()
        {
            splashScreen?.Dispose();
            loginScreen?.Dispose();
            _clientToProxy?.Dispose();
        }
    }
}