using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [AddComponentMenu("AnimationSystem/Network Animator")]
    [RequireComponent(typeof(AnimatorComponent))]
    public sealed class NetworkAnimatorComponent : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private bool _extrapolate = true;
        [SerializeField] private float _maxExtrapolationTime = 0.3f;

        [Header("Interpolation")]
        [SerializeField] private bool _interpolateSnapshots;
        [SerializeField, Range(0f, 0.5f)] private float _interpolationDelay = 0.1f;

        private AnimatorComponent _animator;
        private float _timeSinceLastPacket;
        private bool _hasFirstPacket;

        private ANS.Controller.AnimationNetworkState _snapshotFrom;
        private ANS.Controller.AnimationNetworkState _snapshotTo;
        private float _snapshotInterval;
        private float _interpTimer;

        public float TimeSinceLastPacket => _timeSinceLastPacket;
        public bool HasFirstPacket => _hasFirstPacket;

        private void Awake() => _animator = GetComponent<AnimatorComponent>();

        private void LateUpdate()
        {
            if (!_hasFirstPacket || !_animator.IsInitialized) return;

            _timeSinceLastPacket += Time.deltaTime;

            if (_timeSinceLastPacket > _maxExtrapolationTime) return;

            if (_extrapolate && !_interpolateSnapshots)
            {
                _animator.Controller.Update(Time.deltaTime);
                ApplyCurrentPose();
            }
            else if (_interpolateSnapshots && _snapshotFrom != null && _snapshotTo != null)
            {
                _interpTimer += Time.deltaTime;
                float t = _snapshotInterval > 0f ? Mathf.Clamp01(_interpTimer / _snapshotInterval) : 1f;
                ApplyInterpolatedState(t);
            }
        }

        public void OnPacketReceived(byte[] rawPacket)
        {
            if (!_animator.IsInitialized)
            {
                _animator.Initialize();
                if (!_animator.IsInitialized) return;
            }

            try
            {
                var state = ANS.Serialization.NetworkStateSerializer.Deserialize(rawPacket);
                OnStateReceived(state);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[NetworkAnimator] Failed to deserialize: {e.Message}");
            }
        }

        public void OnStateReceived(ANS.Controller.AnimationNetworkState state)
        {
            if (_interpolateSnapshots)
            {
                _snapshotInterval = _snapshotFrom != null ? _timeSinceLastPacket : 0.033f;
                _snapshotFrom = _snapshotTo;
                _snapshotTo = state;
                _interpTimer = 0f;

                if (_snapshotFrom == null)
                    _animator.ApplyNetworkState(state);
            }
            else
            {
                _animator.ApplyNetworkState(state);
            }

            _timeSinceLastPacket = 0f;
            _hasFirstPacket = true;
        }

        private void ApplyCurrentPose()
        {
            var state = _animator.Controller.GetNetworkState();
            _animator.ApplyNetworkState(state);
        }

        private void ApplyInterpolatedState(float t)
        {
            if (_snapshotFrom == null || _snapshotTo == null) return;

            var interp = new ANS.Controller.AnimationNetworkState
            {
                ServerTime = Mathf.Lerp(_snapshotFrom.ServerTime, _snapshotTo.ServerTime, t)
            };

            int layerCount = Mathf.Min(_snapshotFrom.Layers.Length, _snapshotTo.Layers.Length);
            interp.Layers = new ANS.Blending.LayerParameters[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                var lf = _snapshotFrom.Layers[i];
                var lt = _snapshotTo.Layers[i];
                var cur = new ANS.Blending.StateParameters(
                    lt.CurrentState.ClipId,
                    Mathf.Lerp(lf.CurrentState.Time, lt.CurrentState.Time, t),
                    lt.CurrentState.Speed,
                    lt.CurrentState.Weight);
                interp.Layers[i] = new ANS.Blending.LayerParameters(
                    lt.LayerIndex,
                    Mathf.Lerp(lf.LayerWeight, lt.LayerWeight, t),
                    cur, default, false, 0f, 0f);
            }

            interp.IKTargets = _snapshotTo.IKTargets;
            _animator.ApplyNetworkState(interp);
        }
    }
}