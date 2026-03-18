using System;
using BetterJoystick.Runtime.JoystickRect.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime.JoystickRect.Models
{
    [Serializable]
    public class RectangleRect : IJoystickRect
    {
        private readonly VisualElement _root;

        public RectangleRect(VisualElement root) => _root = root;

        public float   Radius => Mathf.Max(_root.layout.width, _root.layout.height) / 2f;
        public Vector2 Center => _root.layout.center;

        public bool InRange(Vector2 point) => _root.layout.Contains(point);

        public Vector2 GetPointOnEdge(Vector2 point)
        {
            Rect    rect       = _root.layout;
            Vector2 center     = rect.center;
            Vector2 direction  = point - center;
            float   halfWidth  = rect.width  / 2f;
            float   halfHeight = rect.height / 2f;

            float dx = Mathf.Clamp(direction.x, -halfWidth,  halfWidth);
            float dy = Mathf.Clamp(direction.y, -halfHeight, halfHeight);

            return center + new Vector2(dx, dy);
        }
    }
}