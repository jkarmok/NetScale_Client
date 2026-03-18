using System;
using Aiming;
using BetterJoystick.Runtime;
using Game;
using Gateway;
using Infrastructure;
using Skills;
using UI.Screens;
using UI.ViewModels;
using UnityEngine;
using VContainer.Unity;
using Activator = Skills.Activator;

namespace Installers
{
    public class HudInstaller : IStartable, IDisposable
    {
        private readonly HudScreen _hudScreen;
        private readonly InputGateway _inputGateway;
        private readonly HudGateway _hudGateway;
        private readonly GameLoop _gameLoop;
        private readonly SkillActivatorRegistry _registry;
        private readonly AbilityViewModel _abilityViewModel;

        // FIX: кешируем активаторы — не ходим в Dictionary на каждый input
        private Activator _activator01;
        private Activator _activator02;
        private Activator _activator03;
        private Activator _activator04;

        private string _skill01 = string.Empty;
        private string _skill02 = string.Empty;
        private string _skill03 = string.Empty;
        private string _skill04 = string.Empty;

        // FIX: кешируем последние значения Interactable — не дёргаем сеттер без нужды
        private bool _interactable01;
        private bool _interactable02;
        private bool _interactable03;
        private bool _interactable04;

        private float _startedTimeSkill01;
        private float _startedTimeSkill02;
        private float _startedTimeSkill03;
        private float _startedTimeSkill04;

        private bool _initialized;

        public HudInstaller(
            HudScreen hudScreen,
            InputGateway inputGateway,
            HudGateway hudGateway,
            GameLoop gameLoop,
            SkillActivatorRegistry registry,
            AbilityViewModel abilityViewModel)
        {
            _hudScreen        = hudScreen;
            _inputGateway     = inputGateway;
            _hudGateway       = hudGateway;
            _gameLoop         = gameLoop;
            _registry         = registry;
            _abilityViewModel = abilityViewModel;

            _hudScreen.Initialized += OnInitialized;
        }

        private void OnInitialized()
        {
            // FIX: guard от двойной подписки если экран переоткрывается
            if (_initialized) return;
            _initialized = true;

            _gameLoop.Updated += OnUpdated;
            _hudGateway.HudUpdated += OnHudUpdated;

            _hudScreen.Skill01DirectionJoystick.Started   += OnStartedSkill01;
            _hudScreen.Skill01DirectionJoystick.Performed += OnPerformedSkill01;
            _hudScreen.Skill01DirectionJoystick.Completed += OnCompletedSkill01;

            _hudScreen.Skill02DirectionJoystick.Started   += OnStartedSkill02;
            _hudScreen.Skill02DirectionJoystick.Performed += OnPerformedSkill02;
            _hudScreen.Skill02DirectionJoystick.Completed += OnCompletedSkill02;

            _hudScreen.Skill03DirectionJoystick.Started   += OnStartedSkill03;
            _hudScreen.Skill03DirectionJoystick.Performed += OnPerformedSkill03;
            _hudScreen.Skill03DirectionJoystick.Completed += OnCompletedSkill03;

            _hudScreen.Skill04DirectionJoystick.Started   += OnStartedSkill04;
            _hudScreen.Skill04DirectionJoystick.Performed += OnPerformedSkill04;
            _hudScreen.Skill04DirectionJoystick.Completed += OnCompletedSkill04;
        }

        private void OnHudUpdated(HudDto hudDto)
        {
            _abilityViewModel.SetMana((int)hudDto.Mana.Value, (int)hudDto.Mana.MaxValue);

            UpdateSkillState(
                ref _skill01, ref _activator01, ref _interactable01,
                hudDto.Skill01, 0, _hudScreen.Skill01DirectionJoystick);

            UpdateSkillState(
                ref _skill02, ref _activator02, ref _interactable02,
                hudDto.Skill02, 1, _hudScreen.Skill02DirectionJoystick);

            UpdateSkillState(
                ref _skill03, ref _activator03, ref _interactable03,
                hudDto.Skill03, 2, _hudScreen.Skill03DirectionJoystick);

            UpdateSkillState(
                ref _skill04, ref _activator04, ref _interactable04,
                hudDto.Skill04, 3, _hudScreen.Skill04DirectionJoystick);
        }

        private void UpdateSkillState(
            ref string cachedId,
            ref Activator cachedActivator,
            ref bool cachedInteractable,
            SkillStateDto state,
            int index,
            Joystick joystick)
        {
            // FIX: обновляем кеш активатора только при смене скилла
            if (cachedId != state.SkillId)
            {
                cachedId        = state.SkillId;
                cachedActivator = string.IsNullOrEmpty(cachedId) ? null : _registry.Get(cachedId);
            }

            // FIX: не дёргаем сеттер Interactable если значение не изменилось
            if (cachedInteractable != state.CanActivat)
            {
                cachedInteractable     = state.CanActivat;
                joystick.Interactable  = state.CanActivat;
            }

            _abilityViewModel.SetAbility(
                index,
                state.CooldownRemaining,
                state.CooldownProgress,
                state.ManaCost,
                state.CanActivat,
                state.CastProgress,
                state.CastTimeRemaining,
                state.ChargeElapsed,
                state.ChargeProgress);
        }

        private void OnUpdated(float deltaTime)
        {
            // FIX: кешируем MovementDirection — одно обращение к свойству вместо inline
            var movement = _hudScreen.MovementDirection;
            
            if (Application.isEditor)
            {
                movement = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );
            }
            
            bool has1 = _hudScreen.Skill01DirectionJoystick.HasValue(out var dir01);
            bool has2 = _hudScreen.Skill02DirectionJoystick.HasValue(out var dir02);
            bool has3 = _hudScreen.Skill03DirectionJoystick.HasValue(out var dir03);
            bool has4 = _hudScreen.Skill04DirectionJoystick.HasValue(out var dir04);

            _inputGateway.Publish(movement, has1, dir01, has2, dir02, has3, dir03, has4, dir04);
        }

        // Skill01
        private void OnStartedSkill01(Vector2 d)   { _startedTimeSkill01 = Time.time; _activator01?.OnInputStarted(d); }
        private void OnPerformedSkill01(Vector2 d)  { _activator01?.OnInputPerformed(d, Time.time - _startedTimeSkill01); }
        private void OnCompletedSkill01(Vector2 d)  { _activator01?.OnInputCompleted(d, Time.time - _startedTimeSkill01); }

        // Skill02
        private void OnStartedSkill02(Vector2 d)   { _startedTimeSkill02 = Time.time; _activator02?.OnInputStarted(d); }
        private void OnPerformedSkill02(Vector2 d)  { _activator02?.OnInputPerformed(d, Time.time - _startedTimeSkill02); }
        private void OnCompletedSkill02(Vector2 d)  { _activator02?.OnInputCompleted(d, Time.time - _startedTimeSkill02); }

        // Skill03
        private void OnStartedSkill03(Vector2 d)   { _startedTimeSkill03 = Time.time; _activator03?.OnInputStarted(d); }
        private void OnPerformedSkill03(Vector2 d)  { _activator03?.OnInputPerformed(d, Time.time - _startedTimeSkill03); }
        private void OnCompletedSkill03(Vector2 d)  { _activator03?.OnInputCompleted(d, Time.time - _startedTimeSkill03); }

        // Skill04
        private void OnStartedSkill04(Vector2 d)   { _startedTimeSkill04 = Time.time; _activator04?.OnInputStarted(d); }
        private void OnPerformedSkill04(Vector2 d)  { _activator04?.OnInputPerformed(d, Time.time - _startedTimeSkill04); }
        private void OnCompletedSkill04(Vector2 d)  { _activator04?.OnInputCompleted(d, Time.time - _startedTimeSkill04); }

        public void Start() { }

        public void Dispose()
        {
            _hudScreen.Initialized -= OnInitialized;
            _gameLoop.Updated      -= OnUpdated;
            _hudGateway.HudUpdated -= OnHudUpdated;

            _hudScreen.Skill01DirectionJoystick.Started   -= OnStartedSkill01;
            _hudScreen.Skill01DirectionJoystick.Performed -= OnPerformedSkill01;
            _hudScreen.Skill01DirectionJoystick.Completed -= OnCompletedSkill01;

            _hudScreen.Skill02DirectionJoystick.Started   -= OnStartedSkill02;
            _hudScreen.Skill02DirectionJoystick.Performed -= OnPerformedSkill02;
            _hudScreen.Skill02DirectionJoystick.Completed -= OnCompletedSkill02;

            _hudScreen.Skill03DirectionJoystick.Started   -= OnStartedSkill03;
            _hudScreen.Skill03DirectionJoystick.Performed -= OnPerformedSkill03;
            _hudScreen.Skill03DirectionJoystick.Completed -= OnCompletedSkill03;

            _hudScreen.Skill04DirectionJoystick.Started   -= OnStartedSkill04;
            _hudScreen.Skill04DirectionJoystick.Performed -= OnPerformedSkill04;
            _hudScreen.Skill04DirectionJoystick.Completed -= OnCompletedSkill04;
        }
    }
}