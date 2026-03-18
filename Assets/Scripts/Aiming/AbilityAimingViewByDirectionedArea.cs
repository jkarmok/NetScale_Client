using System;
using Aiming.Interface;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Aiming
{
    public class AbilityAimingViewByDirectionedArea : MonoBehaviour, IAimingView
    {
        [SerializeField] private DecalProjector _decalProjector;
        [SerializeField] private Vector3 _offsetRotation = new Vector3 (90f, 0f, 0f);
 
        private Transform _parent;
        private DecalProjector _decalProjectorInstance;
        public AimType AimingType => AimType.Cone;

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
            ThrowIfParentNull();
            _decalProjectorInstance.transform.position = _parent.position;
    
            if (direction == Vector2.zero) return;
    
            var lookDirection = new Vector3(direction.x, 0, direction.y);
            var lookRotation = Quaternion.LookRotation(lookDirection);
            _decalProjectorInstance.transform.rotation = lookRotation * Quaternion.Euler(_offsetRotation);
        }

        private void Update()
        {
            if (_parent != null)
            {
                _decalProjectorInstance.transform.position = _parent.position;
            }
        }
    }
}