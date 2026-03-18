using System;
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
        private readonly AbilityViewModel _abilityViewModel;
        
        public Action Initialized;
        public Joystick MovementDirectionJoystick { get; private set; }
        public Joystick Skill01DirectionJoystick { get; private set; }
        public Joystick Skill02DirectionJoystick { get; private set; }
        public Joystick Skill03DirectionJoystick { get; private set; }
        public Joystick Skill04DirectionJoystick { get; private set; }
        
        private const string MovementDirectionJoystickName = "MovmentDirectionJoystick";
        private const string Skill01DirectionJoystickName = "Joystick1";
        private const string Skill02DirectionJoystickName = "Joystick2";
        private const string Skill03DirectionJoystickName = "Joystick3";
        private const string Skill04DirectionJoystickName = "Joystick4";
 
        public Vector2 MovementDirection => MovementDirectionJoystick.Value;
        public HudScreen(AreaOfInterestViewModel areaOfInterestViewModel, NetworkStatisticViewModel networkStatisticViewModel, AbilityViewModel abilityViewModel)
        {
            _areaOfInterestViewModel = areaOfInterestViewModel;
            _networkStatisticViewModel = networkStatisticViewModel;
            _abilityViewModel = abilityViewModel;
        }
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement, rootElement);
            MovementDirectionJoystick = m_TopElement.Q<Joystick>(MovementDirectionJoystickName);
            Skill01DirectionJoystick = m_TopElement.Q<Joystick>(Skill01DirectionJoystickName);
            Skill02DirectionJoystick = m_TopElement.Q<Joystick>(Skill02DirectionJoystickName);
            Skill03DirectionJoystick = m_TopElement.Q<Joystick>(Skill03DirectionJoystickName);
            Skill04DirectionJoystick = m_TopElement.Q<Joystick>(Skill04DirectionJoystickName);
            _areaOfInterestViewModel.Initialize(topElement, rootElement);      
            _networkStatisticViewModel.Initialize(topElement, rootElement);
            _abilityViewModel.Initialize(topElement, rootElement);
            Initialized.Invoke();
        }

 
        public override void Show()
        {
            Debug.Log($"Start Show HudView");
            base.Show();
        }
    }
}