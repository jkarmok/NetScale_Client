using UnityEngine;

namespace NetScaleClient.Camera
{
    public class CameraTarget : MonoBehaviour, ICameraTarget
    {
        [SerializeField] private bool _owned = false;

        public void ChangeOwned()
        {
            _owned = true;
        }

        public bool Owned => _owned;
        public Transform Pivot => transform;
    }
}