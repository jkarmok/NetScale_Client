using System;
using Aiming.Interface;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aiming
{
    public class AbilityAimingViewByCircle : MonoBehaviour, IAimingView
    {
 
        [SerializeField] private DecalProjector _decalProjector;
        private DecalProjector _decalProjectorInstance;
        private Transform _parent;
        public AimType AimingType => AimType.AreaAround;

        private void Awake()
        {
            _decalProjectorInstance = _decalProjector;
            EndAiming();
        }
        private void OnDestroy()
        {
            if (_decalProjectorInstance != null)
            {
                Destroy(_decalProjectorInstance.gameObject);
                _decalProjectorInstance = null;
            }
        }
        public void SetupParent(Transform parent)
        {
            if (parent == null)
            {
                throw new Exception($"Parent cannot be null");
            }
            _parent = parent;
        }

        private void ThrowIfParentNull()
        {
            if (_parent == null)
            {
                throw new Exception($"Parent is null. Need setup parent");
            }
        }
        public void StartAiming(Vector2 direction, float aimParametersDistance)
        {
            ThrowIfParentNull();
            _decalProjectorInstance.transform.position = _parent.position;
            _decalProjectorInstance.gameObject.SetActive(true);
            DirectionAiming(direction, aimParametersDistance);
        }
        public void EndAiming()
        {
            _decalProjectorInstance.gameObject.SetActive(false);
        }

        public void DirectionAiming(Vector2 direction, float radius)
        {
            _decalProjectorInstance.transform.position = _parent.position + (new Vector3(direction.x, 0, direction.y) * radius);
        }
    }
}