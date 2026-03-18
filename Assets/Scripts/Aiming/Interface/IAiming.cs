using UnityEngine;

namespace Aiming.Interface
{
    public interface IAiming
    {
        void SetupParent(Transform parent);
        void StartAiming(Vector2 direction, float aimParametersDistance);
        void EndAiming();
        void DirectionAiming(Vector2 direction, float radius);
    }
}