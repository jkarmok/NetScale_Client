using System.Collections;
using UI.Transitions;
using UnityEngine;

namespace UI
{
    public class ScreenSwitcher
    {
        private readonly ITransition _transition;
        private readonly ScreenRegistry _registry;
        private UIScreen _currentScreen;

        public ScreenSwitcher(ITransition transition, ScreenRegistry registry)
        {
            _transition = transition;
            _registry = registry;
        }

        public void ShowScreen(ScreenId key, float transitionDuration = 0.5f)
        {
            var newScreen = _registry.Get(key);
            if (_currentScreen == newScreen)
            {
                Debug.LogWarning($"Screen {key} is already shown");
                return;
            }
            Debug.Log($"Start switch {key}");

            CoroutineRunner.Instance.StartCoroutine(SwitchScreens(newScreen, transitionDuration));
        }

        private IEnumerator SwitchScreens(UIScreen newScreen, float transitionDuration)
        {
            if (_currentScreen != null)
            {
                yield return _transition.AnimateIn(transitionDuration);
                _currentScreen.Hide();
            }

            newScreen.Show();
            _currentScreen = newScreen;

            yield return _transition.AnimateOut(transitionDuration);
        }
    }
}