using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Transitions
{
    public class FadeTransitionScreen : UIScreen, ITransition
    {
        private VisualElement _overlay;

        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement, rootElement);
            _overlay = topElement;
        }

        public IEnumerator AnimateIn(float duration)
        {
            _overlay.style.display = DisplayStyle.Flex;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.style.opacity = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        public IEnumerator AnimateOut(float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.style.opacity = Mathf.Clamp01(1 - elapsed / duration);
                yield return null;
            }

            _overlay.style.display = DisplayStyle.None;
        }
    }
}