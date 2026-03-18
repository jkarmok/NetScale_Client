using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [DefaultExecutionOrder(100)]
    public sealed class AnimationSystemManager : MonoBehaviour
    {
        public static AnimationSystemManager Instance { get; private set; }

        private readonly List<AnimatorComponent> _animators = new();
        private readonly List<TransformAccessArray> _transformArrays = new();
        private readonly List<NativeArray<ANS.Core.Transform>> _poseBuffers = new();
        private readonly List<NativeArray<byte>> _skipPositionFlags = new();
        private readonly List<NativeArray<byte>> _skipScaleFlags = new();
        private readonly List<int> _rootBoneIndices = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Register(AnimatorComponent anim)
        {
            if (_animators.Contains(anim)) return;

            var bones = anim.BoneTransforms;
            if (bones == null || bones.Length == 0) return;

            int rootIndex = FindRootBoneIndex(bones, anim.skeletonRoot);

            _animators.Add(anim);
            _rootBoneIndices.Add(rootIndex);
            _transformArrays.Add(new TransformAccessArray(bones));
            _poseBuffers.Add(new NativeArray<ANS.Core.Transform>(bones.Length, Allocator.Persistent));

            var (posFlags, scaleFlags) = CreateSkipFlags(bones.Length, rootIndex, anim);
            _skipPositionFlags.Add(posFlags);
            _skipScaleFlags.Add(scaleFlags);
        }

        public void Unregister(AnimatorComponent anim)
        {
            int idx = _animators.IndexOf(anim);
            if (idx < 0) return;

            DisposeAtIndex(idx);
            RemoveAtIndex(idx);
        }

        private void LateUpdate()
        {
            if (_animators.Count == 0) return;

            float dt = Time.deltaTime;

            for (int i = 0; i < _animators.Count; i++)
            {
                _animators[i].ManualControllerUpdate(dt);
                CopyPoseAndScheduleJob(i);
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _transformArrays.Count; i++)
                DisposeAtIndex(i);

            if (Instance == this)
                Instance = null;
        }

        private static int FindRootBoneIndex(Transform[] bones, Transform skeletonRoot)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == skeletonRoot)
                    return i;
            }
            return -1;
        }

        private static (NativeArray<byte> pos, NativeArray<byte> scale) CreateSkipFlags(
            int boneCount, int rootIndex, AnimatorComponent anim)
        {
            var posFlags = new NativeArray<byte>(boneCount, Allocator.Persistent);
            var scaleFlags = new NativeArray<byte>(boneCount, Allocator.Persistent);

            for (int i = 0; i < boneCount; i++)
            {
                if (i == rootIndex)
                {
                    posFlags[i] = 1;
                    scaleFlags[i] = 1;
                }
                else
                {
                    posFlags[i] = (byte)(anim.skipPositionWriteWhenUnused && i > 0 ? 1 : 0);
                    scaleFlags[i] = (byte)(anim.skipScaleWriteWhenUnused && i > 0 ? 1 : 0);
                }
            }

            return (posFlags, scaleFlags);
        }

        private void CopyPoseAndScheduleJob(int index)
        {
            var anim = _animators[index];
            var pose = anim.Controller.GetCurrentPose().Local;
            var buffer = _poseBuffers[index];

            for (int j = 0; j < pose.Length; j++)
                buffer[j] = pose[j];

            var job = new ApplyPoseJob
            {
                PoseBuffer = buffer,
                SkipPositionFlags = _skipPositionFlags[index],
                SkipScaleFlags = _skipScaleFlags[index],
                RootBoneIndex = _rootBoneIndices[index]
            };

            job.Schedule(_transformArrays[index]).Complete();
        }

        private void DisposeAtIndex(int idx)
        {
            if (_transformArrays[idx].isCreated)
                _transformArrays[idx].Dispose();
            if (_poseBuffers[idx].IsCreated)
                _poseBuffers[idx].Dispose();
            if (_skipPositionFlags[idx].IsCreated)
                _skipPositionFlags[idx].Dispose();
            if (_skipScaleFlags[idx].IsCreated)
                _skipScaleFlags[idx].Dispose();
        }

        private void RemoveAtIndex(int idx)
        {
            _animators.RemoveAt(idx);
            _rootBoneIndices.RemoveAt(idx);
            _transformArrays.RemoveAt(idx);
            _poseBuffers.RemoveAt(idx);
            _skipPositionFlags.RemoveAt(idx);
            _skipScaleFlags.RemoveAt(idx);
        }

        [BurstCompile]
        private struct ApplyPoseJob : IJobParallelForTransform
        {
            [ReadOnly] public NativeArray<ANS.Core.Transform> PoseBuffer;
            [ReadOnly] public NativeArray<byte> SkipPositionFlags;
            [ReadOnly] public NativeArray<byte> SkipScaleFlags;
            [ReadOnly] public int RootBoneIndex;

            public void Execute(int index, TransformAccess transform)
            {
                if (index >= PoseBuffer.Length || index == RootBoneIndex) return;

                var pose = PoseBuffer[index];
                transform.localRotation = ToUnity(pose.Rotation);

                if (SkipPositionFlags[index] == 0)
                    transform.localPosition = ToUnity(pose.Position);

                if (SkipScaleFlags[index] == 0)
                    transform.localScale = ToUnity(pose.Scale);
            }

            private static Vector3 ToUnity(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
            private static Quaternion ToUnity(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);
        }
    }
}