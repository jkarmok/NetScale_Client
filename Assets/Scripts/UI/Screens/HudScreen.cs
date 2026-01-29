using BetterJoystick.Runtime;
using UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Screens
{
    public class HudScreen : UIScreen
    {
        private readonly AreaOfInterestViewModel _areaOfInterestViewModel;
        private readonly NetworkStatisticViewModel _networkStatisticViewModel;
        private Joystick _movementDirectionJoystick;
        private const string MovementDirectionJoystickName = "MovmentDirectionJoystick";
        public Vector2 MovmentDirection => _movementDirectionJoystick.Value;
        public HudScreen(AreaOfInterestViewModel areaOfInterestViewModel, NetworkStatisticViewModel networkStatisticViewModel)
        {
            _areaOfInterestViewModel = areaOfInterestViewModel;
            _networkStatisticViewModel = networkStatisticViewModel;
        }
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
   
            base.Initialize(topElement, rootElement);
            _movementDirectionJoystick = m_TopElement.Q<Joystick>(MovementDirectionJoystickName);
            _areaOfInterestViewModel.Initialize(topElement, rootElement);      
            _networkStatisticViewModel.Initialize(topElement, rootElement);
        }

        public override void Show()
        {
            Debug.Log($"Start Show HudView");
            base.Show();
        }
    }
}