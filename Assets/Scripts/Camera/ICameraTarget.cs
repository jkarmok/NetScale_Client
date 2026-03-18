using UnityEngine;

namespace NetScaleClient.Camera
{
    public interface ICameraTarget
    {
        void ChangeOwned();
        bool Owned { get; }
        Transform Pivot { get; }
    }
}
