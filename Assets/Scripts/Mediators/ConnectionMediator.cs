using System;
using Network;
using UI;
using UI.Screens;

namespace Mediators
{
    public class ConnectionMediator : IDisposable
    {
        private readonly SplashScreen splashScreen;
        private readonly LoginScreen loginScreen;
        private readonly ClientToProxy _clientToProxy;
        private readonly ScreenSwitcher _screenSwitcher;
      
        public ConnectionMediator(SplashScreen splashScreen, LoginScreen loginScreen, ClientToProxy clientToProxy, ScreenSwitcher screenSwitcher)
        {
            this.splashScreen = splashScreen;
            this.loginScreen = loginScreen;
            _clientToProxy = clientToProxy;
            _screenSwitcher = screenSwitcher;

            _clientToProxy.Connected += OnClientConnected;
        }

        private void OnClientConnected(ClientToProxy clientToProxy)
        {
            _screenSwitcher.ShowScreen(ScreenId.Hud);
        }

        public void Start()
        {
            splashScreen.Completed += OnCompleted;
        }

        private void OnCompleted()
        {
            loginScreen.OnConnectSubmitted += OnConnected;
            _screenSwitcher.ShowScreen(ScreenId.Login, 0.5f);
        }

        private void OnConnected(string host, int port)
        {
            _clientToProxy.Connect(host, port);
        }

        public void Dispose()
        {
            _clientToProxy.Connected -= OnClientConnected;
            
            splashScreen?.Dispose();
            loginScreen?.Dispose();
            _clientToProxy?.Dispose();
        }
    }
}