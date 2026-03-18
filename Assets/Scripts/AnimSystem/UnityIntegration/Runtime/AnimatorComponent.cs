using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [AddComponentMenu("AnimationSystem/Animator Component")]
    public sealed class AnimatorComponent : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private SkeletonAsset _skeletonAsset;
        [SerializeField] private AnimationDatabaseAsset _databaseAsset;
        [SerializeField] private Transform _skeletonRoot;

        [Header("Startup")]
        [SerializeField] private string _defaultClip = "";
        [SerializeField, Range(0f, 4f)] private float _playbackSpeed = 1f;

        [Header("Layer Configuration")]
        [SerializeField] private List<LayerConfig> _layerConfigs = new();

        [Header("Performance")]
        [SerializeField, Range(1, 4)] private int _updateEveryNFrames = 1;
        [SerializeField] private bool _autoUpdate = true;
        [SerializeField] private bool _skipScaleWriteWhenUnused = true;
        [SerializeField] private bool _skipPositionWriteWhenUnused;

        [Header("Job System")]
        [SerializeField] private bool _useJobSystem = true;

        [Header("Debug")]
        [SerializeField] private bool _debugDrawSkeleton;

        private ANS.Controller.AnimationController _controller;
        private ANS.Skeleton.SkeletonDefinition _skeletonDef;
        private Transform[] _boneTransforms;
        private int[] _activeIndices;
        private bool _initialized;
        private int _frameCounter;
        private bool _hasPositionAnims;
        private bool _hasScaleAnims;

        public Transform[] BoneTransforms => _boneTransforms;
        public int BoneCount => _boneTransforms?.Length ?? 0;
        public ANS.Controller.AnimationController Controller => _controller;
        public bool IsInitialized => _initialized;

        public bool skipPositionWriteWhenUnused => _skipPositionWriteWhenUnused;
        public bool skipScaleWriteWhenUnused => _skipScaleWriteWhenUnused;
        public Transform skeletonRoot => _skeletonRoot;
        public SkeletonAsset SkeletonAsset => _skeletonAsset;

        private void Awake() => Initialize();

        private void OnEnable()
        {
            if (_initialized && _useJobSystem && AnimationSystemManager.Instance != null)
                AnimationSystemManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (_initialized && _useJobSystem && AnimationSystemManager.Instance != null)
                AnimationSystemManager.Instance.Unregister(this);
        }

        private void LateUpdate()
        {
            if (!_initialized || !_autoUpdate) return;
            if (_useJobSystem && AnimationSystemManager.Instance != null) return;

            if (++_frameCounter < _updateEveryNFrames) return;
            _frameCounter = 0;

            float dt = Time.deltaTime * _playbackSpeed * _updateEveryNFrames;
            _controller.Update(dt);
            ApplyPose();
        }

        public void ManualUpdate(float deltaTime)
        {
            if (!_initialized) return;
            _controller.Update(deltaTime * _playbackSpeed);
            ApplyPose();
        }

        public void ManualControllerUpdate(float deltaTime)
        {
            if (!_initialized) return;

            _controller.Update(deltaTime * _playbackSpeed);

            var currentClip = _controller.GetActiveClip();
            if (currentClip != null)
            {
                _hasPositionAnims = currentClip.HasPositionAnimation;
                _hasScaleAnims = currentClip.HasScaleAnimation;
            }
        }

        public void Initialize()
        {
            if (_initialized) return;
            if (!ValidateAssets()) return;

            _skeletonDef = _skeletonAsset.BuildDefinition();
            _boneTransforms = BuildBoneTransforms();
            if (_boneTransforms == null) return;

            BuildActiveIndices();

            if (_databaseAsset != null)
                _databaseAsset.RegisterForSkeleton(_skeletonAsset);

            _controller = new ANS.Controller.AnimationController(
                _skeletonDef, ANS.Animation.AnimationDatabase.Instance);

            SetupLayers();
            RegisterIKChains();
            ApplyBindPose();

            _initialized = true;

            if (!string.IsNullOrEmpty(_defaultClip))
                Play(_defaultClip);

            if (_useJobSystem && AnimationSystemManager.Instance != null && isActiveAndEnabled)
                AnimationSystemManager.Instance.Register(this);
        }

        private bool ValidateAssets()
        {
            if (_skeletonAsset == null)
            {
                Debug.LogError($"[AnimatorComponent] SkeletonAsset not assigned on '{name}'", this);
                return false;
            }
            if (_skeletonRoot == null)
            {
                Debug.LogError($"[AnimatorComponent] skeletonRoot not assigned on '{name}'", this);
                return false;
            }
            return true;
        }

        private void BuildActiveIndices()
        {
            var list = new List<int>(_boneTransforms.Length);
            for (int i = 0; i < _boneTransforms.Length; i++)
            {
                if (_boneTransforms[i] != null && _boneTransforms[i] != _skeletonRoot)
                    list.Add(i);
            }
            _activeIndices = list.ToArray();
        }

        private Transform[] BuildBoneTransforms()
        {
            var result = new Transform[_skeletonAsset.bones.Count];
            var seen = new HashSet<Transform>();
            bool hasErrors = false;
            bool rootFound = false;

            foreach (var entry in _skeletonAsset.bones)
            {
                if (entry.boneIndex < 0 || entry.boneIndex >= result.Length)
                {
                    Debug.LogError($"[AnimatorComponent] Invalid boneIndex={entry.boneIndex}", this);
                    hasErrors = true;
                    continue;
                }

                if (string.IsNullOrEmpty(entry.transformPath))
                {
                    result[entry.boneIndex] = null;
                    continue;
                }

                Transform bone = transform.Find(entry.transformPath);
                if (bone == null)
                {
                    Debug.LogError($"[AnimatorComponent] Bone '{entry.boneName}' not found at path '{entry.transformPath}'", this);
                    hasErrors = true;
                    continue;
                }

                if (!seen.Add(bone))
                    Debug.LogWarning($"[AnimatorComponent] Transform '{bone.name}' maps to multiple bone indices", this);

                result[entry.boneIndex] = bone;

                if (bone == _skeletonRoot)
                    rootFound = true;
            }

            if (!rootFound)
            {
                Debug.LogError($"[AnimatorComponent] skeletonRoot '{_skeletonRoot.name}' not found in bone mappings", this);
                return null;
            }

            return hasErrors ? null : result;
        }

        private void SetupLayers()
        {
            if (_layerConfigs.Count == 0)
            {
                _controller.AddLayer(ANS.Skeleton.BoneMask.Full(_skeletonDef.BoneCount));
                return;
            }

            foreach (var cfg in _layerConfigs)
            {
                var mask = cfg.affectsAllBones
                    ? ANS.Skeleton.BoneMask.Full(_skeletonDef.BoneCount)
                    : CreateMaskFromBoneNames(cfg.boneNames);

                var blendMode = cfg.additive ? ANS.Blending.BlendMode.Additive : ANS.Blending.BlendMode.Override;
                _controller.AddLayer(mask, blendMode, cfg.weight);
            }
        }

        private ANS.Skeleton.BoneMask CreateMaskFromBoneNames(List<string> boneNames)
        {
            var indices = new List<int>();
            foreach (var boneName in boneNames)
            {
                if (_skeletonDef.TryGetBoneIndex(boneName, out int idx))
                    indices.Add(idx);
            }
            return new ANS.Skeleton.BoneMask(_skeletonDef.BoneCount, indices);
        }

        private void RegisterIKChains()
        {
            foreach (var chain in _skeletonAsset.BuildIKChains())
                _controller.RegisterIKChain(chain);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 ToUnityVec3(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Quaternion ToUnityQuat(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

        public void ApplyPose()
        {
            var pose = _controller.GetCurrentPose();
            var local = pose.Local;
            var bones = _boneTransforms;
            var active = _activeIndices;

            bool writePos = !_skipPositionWriteWhenUnused || _hasPositionAnims;
            bool writeScale = !_skipScaleWriteWhenUnused || _hasScaleAnims;

            if (writePos && writeScale)
            {
                foreach (int i in active)
                {
                    ref readonly var t = ref local[i];
                    var bone = bones[i];
                    bone.localPosition = ToUnityVec3(t.Position);
                    bone.localRotation = ToUnityQuat(t.Rotation);
                    bone.localScale = ToUnityVec3(t.Scale);
                }
            }
            else if (writePos)
            {
                foreach (int i in active)
                {
                    ref readonly var t = ref local[i];
                    var bone = bones[i];
                    bone.localPosition = ToUnityVec3(t.Position);
                    bone.localRotation = ToUnityQuat(t.Rotation);
                }
            }
            else if (writeScale)
            {
                foreach (int i in active)
                {
                    ref readonly var t = ref local[i];
                    var bone = bones[i];
                    bone.localRotation = ToUnityQuat(t.Rotation);
                    bone.localScale = ToUnityVec3(t.Scale);
                }
            }
            else
            {
                foreach (int i in active)
                {
                    bones[i].localRotation = ToUnityQuat(local[i].Rotation);
                }
            }
        }

        private void ApplyBindPose()
        {
            foreach (var entry in _skeletonAsset.bones)
            {
                var t = _boneTransforms[entry.boneIndex];
                if (t == null) continue;
                t.localPosition = entry.bindLocalPosition;
                t.localRotation = entry.bindLocalRotation;
                t.localScale = entry.bindLocalScale;
            }
        }

        public void Play(string clipName, float transitionDuration = 0f)
        {
            if (!_initialized) return;
            try
            {
                _controller.Play(clipName, 0, transitionDuration);
                UpdateAnimationFlags();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimatorComponent] Play('{clipName}'): {e.Message}");
            }
        }

        public void PlayOnLayer(string clipName, int layer, float transitionDuration = 0f)
        {
            if (!_initialized) return;
            try
            {
                _controller.Play(clipName, layer, transitionDuration);
                UpdateAnimationFlags();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AnimatorComponent] PlayOnLayer('{clipName}', {layer}): {e.Message}");
            }
        }

        private void UpdateAnimationFlags()
        {
            var clip = _controller.GetActiveClip();
            if (clip != null)
            {
                _hasPositionAnims = clip.HasPositionAnimation;
                _hasScaleAnims = clip.HasScaleAnimation;
            }
        }

        public void SetLayerWeight(int layer, float weight)
        {
            if (!_initialized) return;
            _controller.SetLayerWeight(layer, Mathf.Clamp01(weight));
        }

        public void SetIKTarget(string chainName, Vector3 worldPosition, Quaternion worldRotation,
            float posWeight = 1f, float rotWeight = 0f)
        {
            if (!_initialized) return;
            int effectorIdx = FindIKEffectorIndex(chainName);
            _controller.SetIKTarget(chainName, new ANS.IK.IKTarget(
                effectorIdx,
                SkeletonAsset.ToANSVec3(worldPosition),
                SkeletonAsset.ToANSQuat(worldRotation),
                posWeight, rotWeight));
        }

        public void DisableIK(string chainName)
        {
            if (!_initialized) return;
            _controller.DisableIK(chainName);
        }

        public void ApplyNetworkState(ANS.Controller.AnimationNetworkState state)
        {
            if (!_initialized) return;
            _controller.ApplyNetworkState(state);
            _controller.Evaluate();
            ApplyPose();
        }

        private int FindIKEffectorIndex(string chainName)
        {
            foreach (var def in _skeletonAsset.ikChains)
                if (def.chainName == chainName && def.boneIndices.Count > 0)
                    return def.boneIndices[^1];
            return 0;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_debugDrawSkeleton || !_initialized || _boneTransforms == null) return;

            for (int i = 0; i < _skeletonDef.BoneCount; i++)
            {
                var bone = _skeletonDef.GetBone(i);
                var t = _boneTransforms[i];
                if (t == null) continue;

                Gizmos.color = t == _skeletonRoot ? Color.red : Color.green;
                Gizmos.DrawWireSphere(t.position, 0.02f);

                if (!bone.IsRoot)
                {
                    var parentT = _boneTransforms[bone.ParentIndex];
                    if (parentT != null)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(parentT.position, t.position);
                    }
                }
            }
        }
    }

    [Serializable]
    public sealed class LayerConfig
    {
        public string layerName = "Layer";
        [Range(0f, 1f)] public float weight = 1f;
        public bool additive;
        public bool affectsAllBones = true;
        public List<string> boneNames = new();
    }
}