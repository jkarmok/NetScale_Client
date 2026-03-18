using System;
using BetterJoystick.Runtime.JoystickRect.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetterJoystick.Runtime.JoystickRect.Models
{
    [Serializable]
    public class CircleRect : IJoystickRect
    {
        private readonly VisualElement _root;

        public CircleRect(VisualElement root) => _root = root;

        public float   Radius => _root.worldBound.width / 2f;
        public Vector2 Center => _root.worldBound.center;

        public bool InRange(Vector2 point)
        {
            float radius = Radius;
            return (point - Center).sqrMagnitude <= radius * radius;
        }

        public Vector2 GetPointOnEdge(Vector2 point)
        {
            Vector2 center = Center;
            return center + (point - center).normalized * Radius;
        }
    }
}