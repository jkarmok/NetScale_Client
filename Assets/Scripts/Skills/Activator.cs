using System;
using Scriptables;
using UnityEngine;

namespace Skills
{
    public class Activator
    {
        private readonly SkillActivationType _activationType;
        private readonly float _castTime;
        private SkillState _currentState;

        private Func<bool> OnBeforeCast;
        public event Action OnCastExecuted;
        public event Action          OnInstantExecuted;
        public event Action<float>   OnChargeStarted;
        public event Action<float>   OnChargeUpdated;
        public event Action<float>   OnChargeCompleted;
        public event Action<Vector2> OnAimStarted;
        public event Action<Vector2> OnAimUpdated;
        public event Action<Vector2> OnAimCompleted;
        public event Action          OnAimCancelled;
        public event Action<bool>    OnToggleChanged;

        public event Action          OnCastStarted;
        public event Action<float>   OnCastUpdated;
        public event Action          OnCastCompleted;
        public event Action          OnCastCancelled;

        public SkillState CurrentState => _currentState;
        public event Action<SkillState> OnStateChanged;

        private float _chargeTime;
        private bool  _toggleState;
        private Vector2 _pendingPosition;

        private float   _castElapsed;
        private float   _pendingChargeValue;
        private Vector2 _pendingCastTarget;
        private bool    _castIsPending;

        public Activator(SkillActivationType activationType, Func<bool> onBeforeCast, float castTime = 0f)
        {
            _activationType = activationType;
            _castTime       = castTime;
            OnBeforeCast    = onBeforeCast;
            _currentState   = SkillState.Ready;
        }

        public void OnInputStarted(Vector2 position)
        {
            if (_currentState != SkillState.Ready) return;

            switch (_activationType)
            {
                case SkillActivationType.Instant:
                    if (!CheckBeforeCast()) return;
                    OnInstantExecuted?.Invoke();
                    BeginCast(Vector2.zero, 0f);
                    break;

                case SkillActivationType.Hold:
                    if (!CheckBeforeCast())
                    {
                        _pendingPosition = position;
                        SetState(SkillState.PendingActivation);
                        return;
                    }
                    SetState(SkillState.Aiming);
                    OnAimStarted?.Invoke(position);
                    break;

                case SkillActivationType.Charge:
                    if (!CheckBeforeCast())
                    {
                        SetState(SkillState.PendingActivation);
                        return;
                    }
                    SetState(SkillState.Charging);
                    _chargeTime = 0f;
                    OnChargeStarted?.Invoke(_chargeTime);
                    break;

                case SkillActivationType.Toggle:
                    if (!_toggleState && !CheckBeforeCast()) return;
                    _toggleState = !_toggleState;
                    SetState(_toggleState ? SkillState.Active : SkillState.Ready);
                    OnToggleChanged?.Invoke(_toggleState);
                    if (_toggleState) BeginCast(Vector2.zero, 0f);
                    break;
            }
        }

        public void OnInputPerformed(Vector2 position, float duration)
        {
            switch (_activationType)
            {
                case SkillActivationType.Hold:
                    if (_currentState == SkillState.PendingActivation)
                    {
                        if (!CheckBeforeCast()) return;
                        SetState(SkillState.Aiming);
                        OnAimStarted?.Invoke(_pendingPosition);
                        return;
                    }
                    if (_currentState == SkillState.Aiming)
                        OnAimUpdated?.Invoke(position);
                    break;

                case SkillActivationType.Charge:
                    if (_currentState == SkillState.PendingActivation)
                    {
                        if (!CheckBeforeCast()) return;
                        SetState(SkillState.Charging);
                        _chargeTime = 0f;
                        OnChargeStarted?.Invoke(_chargeTime);
                        return;
                    }
                    if (_currentState == SkillState.Charging)
                    {
                        _chargeTime = duration;
                        OnChargeUpdated?.Invoke(_chargeTime);
                    }
                    break;
            }
        }

        public void OnInputCompleted(Vector2 position, float duration)
        {
            switch (_activationType)
            {
                case SkillActivationType.Hold:
                    if (_currentState == SkillState.Aiming)
                    {
                        OnAimCompleted?.Invoke(position);
                        BeginCast(position, 0f);
                    }
                    break;

                case SkillActivationType.Charge:
                    if (_currentState == SkillState.Charging)
                    {
                        _chargeTime = duration;
                        OnChargeCompleted?.Invoke(_chargeTime);
                        BeginCast(Vector2.zero, _chargeTime);
                    }
                    break;
            }
        }

        public void OnInputCancelled()
        {
            switch (_activationType)
            {
                case SkillActivationType.Hold:
                    if (_currentState == SkillState.Aiming || _currentState == SkillState.PendingActivation)
                    {
                        if (_currentState == SkillState.Aiming)
                            OnAimCancelled?.Invoke();
                        SetState(SkillState.Ready);
                    }
                    else if (_currentState == SkillState.Casting)
                        CancelCast();
                    break;

                case SkillActivationType.Charge:
                    if (_currentState == SkillState.Charging || _currentState == SkillState.PendingActivation)
                        SetState(SkillState.Ready);
                    else if (_currentState == SkillState.Casting)
                        CancelCast();
                    break;

                case SkillActivationType.Instant:
                case SkillActivationType.Toggle:
                    if (_currentState == SkillState.Casting)
                        CancelCast();
                    break;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_currentState != SkillState.Casting) return;
            if (_castTime <= 0f) return;

            _castElapsed += deltaTime;
            var progress = Mathf.Clamp01(_castElapsed / _castTime);
            OnCastUpdated?.Invoke(progress);

            if (_castElapsed >= _castTime)
                FinishCast();
        }

        private void BeginCast(Vector2 target, float chargeValue)
        {
            _pendingCastTarget  = target;
            _pendingChargeValue = chargeValue;
            _castElapsed        = 0f;

            SetState(SkillState.Casting);
            OnCastStarted?.Invoke();

            if (_castTime <= 0f)
                FinishCast();
        }

        private void FinishCast()
        {
            OnCastCompleted?.Invoke();
            OnCastExecuted?.Invoke();

            if (_currentState == SkillState.Casting)
                SetState(SkillState.Ready);
        }

        private void CancelCast()
        {
            OnCastCancelled?.Invoke();
            SetState(SkillState.Ready);
        }

        private bool CheckBeforeCast() => OnBeforeCast == null || OnBeforeCast.Invoke();

        private void SetState(SkillState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            OnStateChanged?.Invoke(_currentState);
        }
    }

    public enum SkillState
    {
        Ready,
        PendingActivation,
        Aiming,
        Charging,
        Casting,
        Active,
        Cooldown
    }
}