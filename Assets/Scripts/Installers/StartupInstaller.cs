using System;
using Network;
using UI.Screens;
using UnityEngine.SceneManagement;
using Utilities.Inspector;
using VContainer.Unity;

namespace Installers
{
    public class StartupInstaller : IStartable, IDisposable
    {
        private readonly SplashScreen _splashScreen;
        private readonly LoginScreen _loginScreen;
        private readonly ClientToProxy _clientToProxy;
        private readonly SceneField  _sceneGame;

        public StartupInstaller(
            SplashScreen splashScreen, 
            LoginScreen loginScreen, 
            ClientToProxy clientToProxy, 
            SceneField sceneGame)
        {
            _splashScreen = splashScreen;
            _loginScreen = loginScreen;
            _clientToProxy = clientToProxy;
            _sceneGame = sceneGame;
        }

        public void Start()
        {
            SceneManager.LoadSceneAsync(_sceneGame.SceneName, LoadSceneMode.Additive);
        }
 
        public void Dispose()
        {
            _splashScreen?.Dispose();
            _loginScreen?.Dispose();
            _clientToProxy?.Dispose();
        }
    }
}