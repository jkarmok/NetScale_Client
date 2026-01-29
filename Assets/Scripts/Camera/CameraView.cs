using UnityEngine;

namespace Camera
{
    public class CameraView : MonoBehaviour
    {
        [SerializeField] private Transform _cameraPivot;

        private ICameraTarget _cameraTarget;
 
        public void SetTarget(ICameraTarget entity)
        {
            _cameraTarget = entity;
            _cameraPivot.parent = entity.Pivot;
            _cameraPivot.localPosition = Vector3.zero;
        }
        public void UnSetTarget()
        {
            _cameraTarget = null;
            _cameraPivot.parent = null;
        }
        public bool IsTarget(ICameraTarget entity)
        {
            return _cameraTarget == entity;
        }
    }
}