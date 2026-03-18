using System;
using System.Collections.Generic;
using Aiming.Interface;
using UnityEngine;

namespace Aiming
{
    public class AimingStrategySelector : MonoBehaviour
    {
        [SerializeField] private List<MonoBehaviour> _aimingViews;

        private readonly Dictionary<AimType, IAimingView> _aimingByViewId = new();
        private IAimingView _currentAiming;

        private void Awake()
        {
            foreach (var view in _aimingViews)
            {
                if (view is not IAimingView aiming)
                    throw new Exception($"[AimingStrategySelector] {view.name} does not implement IAimingView");

                if (_aimingByViewId.ContainsKey(aiming.AimingType))
                    throw new Exception($"[AimingStrategySelector] Duplicate ViewId '{aiming.AimingType}'");

                _aimingByViewId[aiming.AimingType] = aiming;
            }
        }

        /// <summary>
        /// Вызывается при экипировке способности — выбирает нужную стратегию по ViewId.
        /// </summary>
        public void SetupStrategy(Transform parent)
        {
            _currentAiming?.EndAiming();
            foreach (var view in _aimingByViewId)
            {
                view.Value.SetupParent(parent);
            }
        }

        public void StartAiming(Vector2 direction, float aimParametersDistance, AimType aimType)
        {
            if (!_aimingByViewId.TryGetValue(aimType, out var view))
            {
                throw new Exception($"Don`t have a view called '{aimType}'");
            }

            _currentAiming = view;
            _currentAiming.StartAiming(direction, aimParametersDistance);
        }

        public void EndAiming(AimType aimType)
        {
            ThrowIfStrategyNotSet();
            _currentAiming.EndAiming();
        }

        public void DirectionAiming(Vector2 direction, float radius)
        {
            ThrowIfStrategyNotSet();
            _currentAiming.DirectionAiming(direction, radius);
        }

        public bool HasStrategy(AimType aimType) => _aimingByViewId.ContainsKey(aimType);

        private void ThrowIfStrategyNotSet()
        {
            if (_currentAiming == null)
                throw new Exception("[AimingStrategySelector] No aiming strategy selected. Call SetupStrategy first.");
        }
    }
}