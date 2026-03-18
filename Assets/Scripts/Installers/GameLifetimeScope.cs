using Aiming;
using Gateway;
using Infrastructure;
using NetScaleClient.Camera;
using Scene;
using Scriptables;
using Skills;
using UI;
using UI.Screens;
using UI.Services;
using UI.Transitions;
using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;
using Utilities.Inspector;
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
        [SerializeField] private SceneField _gameScene;
        [SerializeField] private AbilityAimingViewByCircle  _abilityAimingViewByCircle;
        [SerializeField] private AbilityAimingViewByDirectionedArea _abilityAimingViewByDirectioned;
        [SerializeField] private AbilityAimingViewByDirectionedLine  _abilityAimingViewByDirectionedLine;
        [SerializeField] private AimingStrategySelector  _aimingStrategySelector;
        [SerializeField] private SkillListConfig _skillListConfig;
        
        private NotificationServiceConfig _errorServiceConfig;
        private NetworkInstaller _networkInstaller;
        
        protected override void Configure(IContainerBuilder builder)
        {
            _errorServiceConfig = new NotificationServiceConfig
            {
                DefaultPopupDuration = 3f,
                DefaultInlineDuration = 5f
            };
            
            builder.RegisterInstance(_uiDocument);
            builder.RegisterInstance(_gameLoop);
            builder.RegisterInstance(_cameraView);
            builder.RegisterInstance(_entityPrefabConfig);
            builder.RegisterInstance(_errorServiceConfig);
            builder.RegisterInstance(_gameScene);
            builder.RegisterInstance(_abilityAimingViewByCircle);
            builder.RegisterInstance(_abilityAimingViewByDirectioned);
            builder.RegisterInstance(_abilityAimingViewByDirectionedLine);
            builder.RegisterInstance(_aimingStrategySelector);
            builder.RegisterInstance(_skillListConfig);
            builder.RegisterInstance(new SkillManaSystem(100, 1, 1));
         
            
            builder.RegisterInstance(new NotificationService(_inlineErrorTemplate, _popupErrorTemplate, _errorServiceConfig, _uiDocument)).As<INotificationService>();
            
            builder.Register<NetworkTime>(Lifetime.Singleton);
            builder.Register<BatchSceneApplier>(Lifetime.Singleton);
            builder.Register<AreaOfInterestViewModel>(Lifetime.Singleton);
            builder.Register<AbilityViewModel>(Lifetime.Singleton);
            builder.Register<NetworkStatisticViewModel>(Lifetime.Singleton);
            builder.Register<HudScreen>(Lifetime.Singleton);
            builder.Register<LoginScreen>(Lifetime.Singleton);
            builder.Register(resolver => new SplashScreen(0.5f), Lifetime.Singleton);
            builder.Register<LoadingScreen>(Lifetime.Singleton);
            builder.Register<FadeTransitionScreen>(Lifetime.Singleton).AsSelf().As<ITransition>();
            builder.Register<ScreenSwitcher>(Lifetime.Singleton);
            builder.Register<ScreenRegistry>(Lifetime.Singleton);
            builder.Register<SkillController>(Lifetime.Singleton);
            builder.Register<SkillActivatorRegistry>(Lifetime.Singleton);
 
        
            ConfigureTestNetwork(builder);
            builder.RegisterEntryPoint<TestInstaller>();
            builder.RegisterEntryPoint<ServerTimeInstaller>();
            builder.RegisterEntryPoint<AreaOfInterestInstaller>();
            builder.RegisterEntryPoint<ConnectionInstaller>();
            builder.RegisterEntryPoint<HudInstaller>();
            builder.RegisterEntryPoint<NetworkStatisticInstaller>();
     
            
            builder.RegisterEntryPoint<UIStartap>();
            builder.RegisterEntryPoint<StartupInstaller>();
            builder.RegisterEntryPoint<CameraInstaller>();
            builder.RegisterEntryPoint<ViewInstaller>();
            builder.RegisterEntryPoint<AimingInstaller>();
            builder.RegisterEntryPoint<SkillInstaller>();
        }

        private void ConfigureTestNetwork(IContainerBuilder builder)
        {
            _networkInstaller = new NetworkInstaller();
            _networkInstaller.Configure(builder, _gameLoop);
            builder.Register<AreaOfInterestGateway>(Lifetime.Singleton);
            builder.Register<TestGateway>(Lifetime.Singleton);
            builder.Register<InputGateway>(Lifetime.Singleton);
            builder.Register<TimeGateway>(Lifetime.Singleton);
            builder.Register<HudGateway>(Lifetime.Singleton);
        }
    }
}
