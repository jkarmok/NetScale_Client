using UI;
using UI.Screens;
using UI.Transitions;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace Installers
{
    public class UIStartap : IStartable
    {
        private readonly ScreenRegistry _screenRegistry;
        private readonly UIDocument _uiDocument;
        private readonly HudScreen hudScreen;
        private readonly LoadingScreen loadingScreen;
        private readonly LoginScreen loginScreen;
        private readonly SplashScreen splashScreen;
        private readonly FadeTransitionScreen fadeTransitionScreen;
        private readonly ScreenSwitcher _screenSwitcher;

        public const string HUDViewName = "HudView";
        public const string LoginViewName = "LoginView";
        public const string SplashViewName = "SplashView";
        public const string LoadingViewName = "LoadingView";

        public UIStartap(
            ScreenRegistry screenRegistry,
            UIDocument uiDocument,
            HudScreen hudScreen,
            LoadingScreen loadingScreen,
            LoginScreen loginScreen,
            SplashScreen splashScreen,
            FadeTransitionScreen fadeTransitionScreen,
            ScreenSwitcher screenSwitcher
        )
        {
            _screenRegistry = screenRegistry;
            _uiDocument = uiDocument;
            this.hudScreen = hudScreen;
            this.loadingScreen = loadingScreen;
            this.loginScreen = loginScreen;
            this.splashScreen = splashScreen;
            this.fadeTransitionScreen = fadeTransitionScreen;
            _screenSwitcher = screenSwitcher;
        }

        public void Start()
        {
            SetupViews();
        }

        public void SetupViews()
        {
            VisualElement root = _uiDocument.rootVisualElement;

            hudScreen.Initialize(root.Q<VisualElement>(HUDViewName), root);
            loginScreen.Initialize(root.Q<VisualElement>(LoginViewName), root);
            splashScreen.Initialize(root.Q<VisualElement>(SplashViewName), root);
            loadingScreen.Initialize(root.Q<VisualElement>(LoadingViewName), root);
            fadeTransitionScreen.Initialize(CreateFadeOverlay(root), root);

            _screenRegistry.RegisterScreen(ScreenId.Hud, hudScreen);
            _screenRegistry.RegisterScreen(ScreenId.Loading, loadingScreen);
            _screenRegistry.RegisterScreen(ScreenId.Login, loginScreen);
            _screenRegistry.RegisterScreen(ScreenId.Splash, splashScreen);
            
            _screenSwitcher.ShowScreen(ScreenId.Splash, 0.5f);
        }

        private static VisualElement CreateFadeOverlay(VisualElement root)
        {
            var fadeOverlay = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    top = 0,
                    left = 0,
                    right = 0,
                    bottom = 0,
                    backgroundColor = new Color(0, 0, 0, 1),
                    opacity = 0,
                    display = DisplayStyle.None
                }
            };
            root.Add(fadeOverlay);
            return fadeOverlay;
        }
    }
}