using System;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace UI.Screens
{
    public class SplashScreen : UIScreen
    {
        private readonly float _displayDuration;
        public event Action Completed;

        public SplashScreen(float displayDuration = 0.5f)
        {
            _displayDuration = displayDuration;
        }
  
        public override void Show()
        {
            base.Show();

            StartDisplayTimer();
        }

        private async void StartDisplayTimer()
        {
            await Task.Delay(TimeSpan.FromSeconds(_displayDuration)); // todo will replaced to unitask
            
            Completed?.Invoke();
        }
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement, rootElement);
        }
    }
}