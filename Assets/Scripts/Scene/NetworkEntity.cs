using Game.Scene;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

namespace Scene
{
    public class NetworkEntity : MonoBehaviour
    {
        public int InstanceHash { get; private set; }
        public byte TypeId { get; private set; }
        public int SpartalOwnerId { get; private set; }
        public int ProxyId { get; private set; }
        public TransformState TransformState { get; private set; }
 
        private NetworkInterpolator _interpolator;
        private NetworkInterpolatorVisualizer _visualizer;
        
        [Inject] 
        private NetworkTime _networkTime;

        private void Awake()
        {
            _interpolator = new NetworkInterpolator(transform, _networkTime);
            TryGetVisualizer();
        }

        private void TryGetVisualizer()
        {
            #if UNITY_EDITOR
            _visualizer = gameObject.GetOrAddComponent<NetworkInterpolatorVisualizer>();
            #endif
        }

        private void Update()
        {
            _interpolator.Update();
        }
  
        public void SetTargetTransform(Vector3 position, Quaternion rotation, double serverTime, int updateInterval = 1)
        {
            _interpolator.OnServerUpdate(position, rotation, serverTime, updateInterval);
#if UNITY_EDITOR
            if (_visualizer != null)
            {
                _visualizer.UpdateServerData(position, rotation, serverTime);
            }
#endif
        }

        public void SpawnSetup(int entityDtoHash, byte typeId, int entityDtoSpartalOwnerId, int entityDtoProxyId,
            TransformState entityDtoTransformState, Vector3 position, Quaternion rotation)
        {
            InstanceHash = entityDtoHash;
            TypeId = typeId;
            SpartalOwnerId = entityDtoSpartalOwnerId;
            ProxyId = entityDtoProxyId;
            TransformState = entityDtoTransformState;
            
            transform.position = position;
            transform.rotation = rotation;
            
            _interpolator.Reset(position, rotation);
            _interpolator.Teleport(position, rotation);
#if UNITY_EDITOR
            if (_visualizer != null)
            {
                _visualizer.UpdateServerData(position, rotation, _networkTime.GetLocalTime());
            }
#endif
        }

        public void ConfigureForDistance(float distanceToPlayer)
        {
            _interpolator.ConfigureForDistance(distanceToPlayer);
            var stats = _interpolator.GetStats();
            Debug.Log($"Entity {InstanceHash}: distance {distanceToPlayer:F1}, " +
                      $"interval {stats.currentUpdateInterval}, tickrate {stats.effectiveTickRate} FPS");
        }
        public void Teleport(Vector3 position, Quaternion rotation) => _interpolator.Teleport(position, rotation);
        public NetworkInterpolator.InterpolationStats GetInterpolationStats() => _interpolator.GetStats();
        public NetworkInterpolator.DebugInfo GetDebugInfo() => _interpolator.GetDebugInfo();
    }
}