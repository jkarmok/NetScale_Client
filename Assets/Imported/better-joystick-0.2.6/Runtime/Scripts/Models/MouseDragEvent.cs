using UnityEngine;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime.Models
{
    /// <summary>
    /// Struct-based drag event passed from InnerJoystickImage to Joystick.
    /// Using struct eliminates heap allocations on every pointer move.
    /// Now uses IPointerEvent instead of IMouseEvent to support multi-touch.
    /// </summary>
    public readonly struct MouseDragEvent
    {
        public readonly IEventHandler Target;
        public readonly Vector2 MousePosition;
        public readonly Vector2 LocalMousePosition;
        public readonly Vector2 DeltaMousePosition;
        public readonly DragState State;

        public MouseDragEvent(IPointerEvent pointerEvent, IEventHandler target, DragState state)
        {
            Target             = target;
            State              = state;
            MousePosition      = pointerEvent.position;
            LocalMousePosition = pointerEvent.localPosition;
            DeltaMousePosition = pointerEvent.deltaPosition;
        }
    }
}