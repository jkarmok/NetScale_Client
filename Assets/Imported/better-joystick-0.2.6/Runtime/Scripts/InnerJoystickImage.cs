using System;
using BetterJoystick.Runtime.Models;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime
{
    public class InnerJoystickImage : Image
    {
        private readonly bool _centerByPress;
        public event Action<MouseDragEvent> DragEvent;

        private bool _isInitialized;

        private DragState _dragState;
        public const string StyleClassName = "better-inner-joystick";

        public InnerJoystickImage(bool centerByPress)
        {
            _centerByPress = centerByPress;
            _dragState = DragState.AtRest;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            AddToClassList(StyleClassName);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            UnregisterCallback<AttachToPanelEvent>(OnAttach);
            panel.visualTree.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            panel.visualTree.RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            panel.visualTree.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if (_centerByPress)
            {
                if (e.target != parent || e.button != 0) return;
            }
            else
            {
                if (e.target != this || e.button != 0) return;
            }


            PrepareDraggingBox();
            CallEvent(e);
        }

        public void PrepareDraggingBox()
        {
            _dragState = DragState.Ready;
        }

        private void OnMouseMoveEvent(MouseMoveEvent e)
        {
            if (_dragState == DragState.Ready)
            {
                StartDraggingBox();
            }

            if (_dragState == DragState.Dragging)
            {
                CallEvent(e);
            }
        }

        private void OnMouseUpEvent(MouseUpEvent e)
        {
            if (_dragState != DragState.AtRest && e.button == 0)
            {
                StopDraggingBox();
                CallEvent(e);
            }
        }

        private void StartDraggingBox()
        {
            AddToClassList("dragged");
            _dragState = DragState.Dragging;
        }

        private void CallEvent<T>(MouseEventBase<T> e) where T : MouseEventBase<T>, new()
        {
            DragEvent?.Invoke(new MouseDragEvent(e, e.target, _dragState));
        }

        private void StopDraggingBox()
        {
            RemoveFromClassList("dragged");
            _dragState = DragState.AtRest;
        }
    }
}