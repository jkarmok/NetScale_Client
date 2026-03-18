using System;
using Network;
using UI;
using UI.Screens;
using UI.Services;
using VContainer.Unity;

namespace Installers
{
    public class ConnectionInstaller : IDisposable, IStartable
    {
        private readonly SplashScreen _splashScreen;
        private readonly LoginScreen _loginScreen;
        private readonly ClientToProxy _clientToProxy;
        private readonly ScreenSwitcher _screenSwitcher;
        private readonly INotificationService _notificationService;
        public ConnectionInstaller(
            SplashScreen splashScreen, 
            LoginScreen loginScreen,
            ClientToProxy clientToProxy, 
            ScreenSwitcher screenSwitcher, 
            INotificationService notificationService)
        {
            _splashScreen = splashScreen;
            _loginScreen = loginScreen;
            _clientToProxy = clientToProxy;
            _screenSwitcher = screenSwitcher;
            _notificationService = notificationService;

            _clientToProxy.Connected += OnClientConnected;
            _clientToProxy.OnConnectionTimeout += OnConnectionTimeout;
        }

        private void OnConnectionTimeout()
        {
            _notificationService.ShowPopupNotification( "Connection timeout", NotificationType.Error, NotificationPosition.BottomRight);
        }

        private void OnClientConnected(ClientToProxy clientToProxy)
        {
            _screenSwitcher.ShowScreen(ScreenId.Hud);
            _notificationService.ShowPopupNotification( "Success!", NotificationType.Success, NotificationPosition.BottomRight);
 
        }

        public void Start()
        {
            _splashScreen.Completed += OnCompleted;
        }

        private void OnCompleted()
        {
            _loginScreen.OnConnectSubmitted += OnConnected;
            _screenSwitcher.ShowScreen(ScreenId.Login, 0.5f);
        }

        private void OnConnected(string host, int port)
        {
            _clientToProxy.Connect(host, port);
        }

        public void Dispose()
        {
            _clientToProxy.Connected -= OnClientConnected;
            _clientToProxy.OnConnectionTimeout -= OnConnectionTimeout;
            
            _splashScreen?.Dispose();
            _loginScreen?.Dispose();
            _clientToProxy?.Dispose();
        }
    }
}