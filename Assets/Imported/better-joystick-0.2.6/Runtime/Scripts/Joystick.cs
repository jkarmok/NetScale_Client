using System;
using BetterJoystick.Runtime.JoystickRect.Interfaces;
using BetterJoystick.Runtime.JoystickRect.Models;
using BetterJoystick.Runtime.Models;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime
{
    public class Joystick : VisualElement, INotifyValueChanged<Vector2>
    {
        [Preserve]
        public new class UxmlFactory : UxmlFactory<Joystick, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription _normalizeAttr =
                new UxmlBoolAttributeDescription { name = "Normalize", defaultValue = false };

            private readonly UxmlBoolAttributeDescription _recenterAttr =
                new UxmlBoolAttributeDescription { name = "Recenter", defaultValue = true };

            private readonly UxmlBoolAttributeDescription _centerByPressAttr =
                new UxmlBoolAttributeDescription { name = "CenterByPress", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var joystick = (Joystick)ve;

                joystick.Normalize     = _normalizeAttr.GetValueFromBag(bag, cc);
                joystick.Recenter      = _recenterAttr.GetValueFromBag(bag, cc);
                joystick.CenterByPress = _centerByPressAttr.GetValueFromBag(bag, cc);
                joystick.RebuildInner();
            }
        }

        public const string StyleClassName = "better-joystick";

        public event Action<Vector2> Started;
        public event Action<Vector2> Performed;
        public event Action<Vector2> Completed;

        public bool    Normalize     { get; set; } = false;
        public bool    Recenter      { get; set; } = true;
        public bool    CenterByPress { get; set; } = false;
        public bool Interactable
        {
            get => _interactable;
            set
            {
                if (_interactable == value) return;
                _interactable = value;
                if (!_interactable && _isActive)
                    HandleRelease();   // force-release if locked mid-drag
            }
        }
        private bool _interactable = true;
        
        private const float Threshold = 0.01f;
        public Vector2 Value { get; private set; }

        private InnerJoystickImage   _inner;
        private IJoystickRect        _joystickRect;

        // ─── Новые поля для Update-loop ──────────────────────────────────────────
        private bool                 _isActive;        // джойстик зажат
        private IVisualElementScheduledItem _updateLoop; // "Update" через scheduler

        public Joystick()
        {
            AddToClassList(StyleClassName);
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/JoystickStyles"));
            RebuildInner();
        }

        public bool HasValue(out Vector2 position)
        {
            position = Value;
            return position.magnitude > Threshold;
        }

        // ─── Build ───────────────────────────────────────────────────────────────

        internal void RebuildInner()
        {
            StopUpdateLoop(); // на случай пересборки

            if (_inner != null)
            {
                _inner.DragEvent = null;
                Remove(_inner);
            }

            _inner           = new InnerJoystickImage(CenterByPress);
            _inner.DragEvent = OnDragEvent;
            Add(_inner);
            _inner.BringToFront();

            _joystickRect = new CircleRect(this);
            Value         = Vector2.zero;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        // ─── Update loop ─────────────────────────────────────────────────────────

        private void StartUpdateLoop()
        {
            if (_updateLoop != null) return;

            // intervalMs = 0 → каждый кадр (аналог Update)
            _updateLoop = schedule.Execute(OnUpdate).Every(0);
        }

        private void StopUpdateLoop()
        {
            _updateLoop?.Pause();
            _updateLoop = null;
        }

        private void OnUpdate()
        {
            if (_isActive)
                Performed?.Invoke(Value);
        }

        // ─── Geometry ────────────────────────────────────────────────────────────

        private void OnGeometryChanged(GeometryChangedEvent _) => PlaceThumbAtCenter();

        private void PlaceThumbAtCenter()
        {
            var outer = resolvedStyle;
            float halfW = (outer.width  - GetLeftOffset(outer)) / 2f;
            float halfH = (outer.height - GetTopOffset(outer))  / 2f;

            var thumb = _inner.resolvedStyle;
            float thumbHalfW = (thumb.width  + GetLeftOffset(thumb)) / 2f;
            float thumbHalfH = (thumb.height + GetTopOffset(thumb))  / 2f;

            _inner.style.left = halfW - thumbHalfW;
            _inner.style.top  = halfH - thumbHalfH;
        }

        private Vector2 GetThumbHalfSize()
        {
            var s = _inner.resolvedStyle;
            return new Vector2(
                (s.width  + GetLeftOffset(s)) / 2f,
                (s.height + GetTopOffset(s))  / 2f
            );
        }

        private static float GetTopOffset(IResolvedStyle s)
            => s.paddingTop  + s.marginTop  + s.borderTopWidth;

        private static float GetLeftOffset(IResolvedStyle s)
            => s.paddingLeft + s.marginLeft + s.borderLeftWidth;

        // ─── Drag handling ───────────────────────────────────────────────────────

        private void OnDragEvent(MouseDragEvent drag)
        {
            if (!_interactable) return;
            
            switch (drag.State)
            {
                case DragState.AtRest:
                    HandleRelease();
                    break;

                case DragState.Ready:
                    if (CenterByPress)
                    {
                        HandleMove(drag, firePerformed: false);
                        _isActive = true;
                        StartUpdateLoop();
                        Started?.Invoke(Value);
                    }
                    break;

                case DragState.Started:
                    HandleMove(drag, firePerformed: false);
                    _isActive = true;
                    StartUpdateLoop();
                    Started?.Invoke(Value);
                    break;

                case DragState.Dragging:
                    HandleMove(drag, firePerformed: false); // Performed теперь в OnUpdate
                    break;
            }
        }

        private void HandleRelease()
        {
            _isActive = false;
            StopUpdateLoop();

            if (Recenter)
                PlaceThumbAtCenter();

            DispatchValueChange(previousValue: Value, newValue: Vector2.zero);
            Completed?.Invoke(Vector2.zero);
        }

        private void HandleMove(MouseDragEvent drag, bool firePerformed)
        {
            Vector2 mousePos = drag.MousePosition;

            if (!_joystickRect.InRange(mousePos))
                mousePos = _joystickRect.GetPointOnEdge(mousePos);

            Vector2 thumbOffset = GetThumbHalfSize();
            Vector2 localPos    = this.WorldToLocal(mousePos);
            _inner.style.left = localPos.x - thumbOffset.x;
            _inner.style.top  = localPos.y - thumbOffset.y;

            Vector2 rawValue = mousePos - _joystickRect.Center;
            rawValue.y = -rawValue.y;

            Vector2 newValue = Normalize
                ? rawValue.normalized * Mathf.InverseLerp(0f, _joystickRect.Radius, rawValue.magnitude)
                : rawValue;

            DispatchValueChange(previousValue: Value, newValue: newValue);

            if (firePerformed)
                Performed?.Invoke(Value);
        }

        private void DispatchValueChange(Vector2 previousValue, Vector2 newValue)
        {
            Value = newValue;

            using (var evt = JoystickEvent.GetPooled(previousValue, newValue))
            {
                evt.target = this;
                panel?.visualTree.SendEvent(evt);
            }
        }

        // ─── INotifyValueChanged ─────────────────────────────────────────────────

        Vector2 INotifyValueChanged<Vector2>.value
        {
            get => Value;
            set
            {
                if (Value == value) return;
                using (var evt = JoystickEvent.GetPooled(Value, value))
                {
                    evt.target = this;
                    panel?.visualTree.SendEvent(evt);
                }
                Value = value;
            }
        }

        public void SetValueWithoutNotify(Vector2 newValue) => Value = newValue;

        public void SetJoystickRect(IJoystickRect joystickRect)
        {
            _joystickRect = joystickRect;
            PlaceThumbAtCenter();
        }
    }
}