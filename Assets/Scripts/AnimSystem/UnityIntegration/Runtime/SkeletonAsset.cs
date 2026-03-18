using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [CreateAssetMenu(menuName = "AnimationSystem/Skeleton Asset", fileName = "NewSkeletonAsset")]
    public sealed class SkeletonAsset : ScriptableObject
    {
        [SerializeField] private List<BoneEntry> _bones = new();
        [SerializeField] private List<IKChainDefinition> _ikChains = new();

        public List<BoneEntry> bones => _bones;
        public List<IKChainDefinition> ikChains => _ikChains;

        public ANS.Skeleton.SkeletonDefinition BuildDefinition()
        {
            var boneArray = new ANS.Skeleton.Bone[_bones.Count];
            for (int i = 0; i < _bones.Count; i++)
            {
                var e = _bones[i];
                var t = new ANS.Core.Transform(
                    ToANSVec3(e.bindLocalPosition),
                    ToANSQuat(e.bindLocalRotation),
                    ToANSVec3(e.bindLocalScale));
                boneArray[i] = new ANS.Skeleton.Bone(e.boneIndex, e.boneName, e.parentIndex, t);
            }
            return new ANS.Skeleton.SkeletonDefinition(boneArray);
        }

        public List<ANS.Controller.IKChain> BuildIKChains()
        {
            var result = new List<ANS.Controller.IKChain>(_ikChains.Count);
            foreach (var def in _ikChains)
            {
                ANS.Controller.IKChain chain;
                if (def.chainType == IKChainType.TwoBone && def.boneIndices.Count >= 3)
                {
                    chain = new ANS.Controller.IKChain(
                        def.chainName,
                        def.boneIndices[0],
                        def.boneIndices[1],
                        def.boneIndices[2]);
                }
                else if (def.boneIndices.Count >= 2)
                {
                    chain = new ANS.Controller.IKChain(def.chainName, def.boneIndices.ToArray())
                    {
                        FABRIKIterations = def.fabrikIterations
                    };
                }
                else continue;

                result.Add(chain);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToANSVec3(Vector3 v) => new(v.x, v.y, v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Quaternion ToANSQuat(Quaternion q) => new(q.x, q.y, q.z, q.w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToUnityVec3(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToUnityQuat(System.Numerics.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

        [Serializable]
        public sealed class BoneEntry
        {
            public string boneName = "";
            public int boneIndex;
            public int parentIndex = -1;
            public string transformPath = "";
            public Vector3 bindLocalPosition;
            public Quaternion bindLocalRotation = Quaternion.identity;
            public Vector3 bindLocalScale = Vector3.one;
        }
    }

    public enum IKChainType { TwoBone, FABRIK }

    [Serializable]
    public sealed class IKChainDefinition
    {
        public string chainName = "";
        public IKChainType chainType = IKChainType.TwoBone;
        public List<int> boneIndices = new();
        [Range(1, 30)] public int fabrikIterations = 10;
    }
}