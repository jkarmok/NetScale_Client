using System;
using BetterJoystick.Runtime.Models;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime
{
    /// <summary>
    /// Inner thumb image of the joystick.
    /// Tracks pointer/touch drag and reports state changes via <see cref="DragEvent"/>.
    /// Uses Pointer events to support simultaneous multi-touch / multi-joystick.
    /// Works with mouse in Editor as well (mouse generates pointerId = PointerId.mousePointerId).
    ///
    /// State machine:
    ///   AtRest → (PointerDown) → Ready
    ///   Ready  → (PointerMove) → Started → (next PointerMove) → Dragging
    ///   Dragging / Started → (PointerUp / PointerCancel) → AtRest
    /// </summary>
    public class InnerJoystickImage : Image
    {
        public const string StyleClassName    = "better-inner-joystick";
        private const string DraggedClassName = "dragged";

        /// <summary>Fired on every relevant pointer event. Uses struct to avoid heap allocation.</summary>
        public Action<MouseDragEvent> DragEvent;

        private readonly bool _centerByPress;
        private DragState     _dragState;

        /// <summary>The pointer id currently tracked by this thumb (-1 = none).</summary>
        private int _trackedPointerId = -1;

        public InnerJoystickImage(bool centerByPress)
        {
            _centerByPress = centerByPress;
            _dragState     = DragState.AtRest;

            AddToClassList(StyleClassName);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void OnAttach(AttachToPanelEvent evt)
        {
            UnregisterCallback<AttachToPanelEvent>(OnAttach);

            // PointerDown: listen on self and parent so both normal & CenterByPress modes work.
            // Using TrickleDown so we catch it before children consume it.
            RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            parent?.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);

            // Move/Up: listen on visualTree so we don't lose the pointer when it slides outside.
            panel.visualTree.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            panel.visualTree.RegisterCallback<PointerUpEvent>(OnPointerUp);
            panel.visualTree.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        // ─── Pointer handlers ────────────────────────────────────────────────────

        private void OnPointerDown(PointerDownEvent e)
        {
            // Already tracking a pointer — ignore
            if (_trackedPointerId != -1) return;

            bool validTarget = _centerByPress
                ? e.currentTarget == parent
                : e.currentTarget == this;

            if (!validTarget) return;

            _trackedPointerId = e.pointerId;
            _dragState        = DragState.Ready;
            FireEvent(e);

            e.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            if (e.pointerId != _trackedPointerId) return;

            switch (_dragState)
            {
                case DragState.Ready:
                    _dragState = DragState.Started;
                    AddToClassList(DraggedClassName);
                    FireEvent(e);
                    _dragState = DragState.Dragging;
                    break;

                case DragState.Dragging:
                    FireEvent(e);
                    break;
            }
        }

        private void OnPointerUp(PointerUpEvent e)
        {
            if (e.pointerId != _trackedPointerId || _dragState == DragState.AtRest) return;
            ResetState();
            FireEvent(e);
        }

        private void OnPointerCancel(PointerCancelEvent e)
        {
            if (e.pointerId != _trackedPointerId || _dragState == DragState.AtRest) return;
            ResetState();
            FireEvent(e);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private void ResetState()
        {
            _trackedPointerId = -1;
            _dragState        = DragState.AtRest;
            RemoveFromClassList(DraggedClassName);
        }

        private void FireEvent<T>(PointerEventBase<T> e) where T : PointerEventBase<T>, new()
        {
            DragEvent?.Invoke(new MouseDragEvent(e, e.target, _dragState));
        }
    }
}