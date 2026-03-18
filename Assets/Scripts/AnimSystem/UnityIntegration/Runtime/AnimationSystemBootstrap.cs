using UnityEngine;
using AnimationSystem.Core;
using AnimationSystem.Unity.Burst;

namespace AnimationSystem.Unity
{
    [AddComponentMenu("AnimationSystem/Bootstrap")]
    [DefaultExecutionOrder(-200)]
    public sealed class AnimationSystemBootstrap : MonoBehaviour
    {
        [Header("Math Provider")]
        [SerializeField] private bool _useBurstProvider = true;
        [SerializeField] private bool _logProviderInfo = true;

        [Header("Manager")]
        [SerializeField] private bool _useJobSystem = true;

        private BurstMathProvider _burstProvider;
        private AnimationSystemManager _manager;

        private void Awake()
        {
            RegisterMathProvider();
            CreateManager();
        }

        private void RegisterMathProvider()
        {
            if (_useBurstProvider)
            {
                _burstProvider = new BurstMathProvider();
                MathProviderRegistry.Current = _burstProvider;
                if (_logProviderInfo)
                    Debug.Log("[AnimationSystem] BurstMathProvider registered");
            }
            else
            {
                MathProviderRegistry.Current = new DefaultMathProvider();
                if (_logProviderInfo)
                    Debug.Log("[AnimationSystem] DefaultMathProvider registered");
            }
        }

        private void CreateManager()
        {
            if (!_useJobSystem) return;

            var go = new GameObject("AnimationSystemManager");
            _manager = go.AddComponent<AnimationSystemManager>();
            DontDestroyOnLoad(go);
            
            if (_logProviderInfo)
                Debug.Log("[AnimationSystem] Manager created with Job System");
        }

        private void OnDestroy()
        {
            if (_burstProvider != null)
            {
                _burstProvider = null;
                MathProviderRegistry.Current = new DefaultMathProvider();
            }
        }
    }
}