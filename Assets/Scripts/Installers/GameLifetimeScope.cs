using Camera;
using Gateway;
using Infrastructure;
using Mediators;
using Scene;
using UI;
using UI.Screens;
using UI.Services;
using UI.Transitions;
using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

namespace Installers
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _inlineErrorTemplate;
        [SerializeField] private VisualTreeAsset _popupErrorTemplate;
        
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private EntityPrefabConfig _entityPrefabConfig;
        [SerializeField] private CameraView _cameraView;
        private ErrorServiceConfig _errorServiceConfig;
        private NetworkInstaller _networkInstaller;
        
        protected override void Configure(IContainerBuilder builder)
        {
            _errorServiceConfig = new ErrorServiceConfig
            {
                DefaultPopupDuration = 3f,
                DefaultInlineDuration = 5f
            };
            
            builder.RegisterInstance(_uiDocument);
            builder.RegisterInstance(_gameLoop);
            builder.RegisterInstance(_cameraView);
            builder.RegisterInstance(_entityPrefabConfig);
            builder.RegisterInstance(_errorServiceConfig);
            builder.RegisterInstance(new ErrorService(_inlineErrorTemplate, _popupErrorTemplate, _errorServiceConfig)).As<IErrorService>();
            
            builder.Register<NetworkTime>(Lifetime.Singleton);
            builder.Register<BatchSceneApplier>(Lifetime.Singleton);
            builder.Register<AreaOfInterestViewModel>(Lifetime.Singleton);
            builder.Register<NetworkStatisticViewModel>(Lifetime.Singleton);
            builder.Register<HudScreen>(Lifetime.Singleton);
            builder.Register<LoginScreen>(Lifetime.Singleton);
            builder.Register(resolver => new SplashScreen(0.5f), Lifetime.Singleton);
            builder.Register<LoadingScreen>(Lifetime.Singleton);
            builder.Register<FadeTransitionScreen>(Lifetime.Singleton).AsSelf().As<ITransition>();
            builder.Register<ScreenSwitcher>(Lifetime.Singleton);
            builder.Register<ScreenRegistry>(Lifetime.Singleton);
 
        
            ConfigureTestNetwork(builder);
            builder.RegisterEntryPoint<UIStartap>();
            builder.RegisterEntryPoint<NetworkStartup>();
            builder.RegisterEntryPoint<CameraInstaller>();
            builder.RegisterEntryPoint<ViewInstaller>();
        }

        private void ConfigureTestNetwork(IContainerBuilder builder)
        {
            _networkInstaller = new NetworkInstaller();
            _networkInstaller.Configure(builder, _gameLoop);
            builder.Register<AreaOfInterestGateway>(Lifetime.Singleton);
            builder.Register<TestGateway>(Lifetime.Singleton);
            builder.Register<InputGateway>(Lifetime.Singleton);
            builder.Register<TimeGateway>(Lifetime.Singleton);
            
            builder.Register<TestMediators>(Lifetime.Singleton);
            builder.Register<TimeMediator>(Lifetime.Singleton);
            builder.Register<AreaOfInterestMediator>(Lifetime.Singleton);
            builder.Register<ConnectionMediator>(Lifetime.Singleton);
            builder.Register<HudMediator>(Lifetime.Singleton);
            builder.Register<NetworkMediator>(Lifetime.Singleton);
        }
    }
}
