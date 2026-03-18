using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using AnimationSystem.Core;

namespace AnimationSystem.Unity.Burst
{
    public sealed unsafe class BurstMathProvider : IMathProvider
    {
        public BurstMathProvider()
        {
            if (UnsafeUtility.SizeOf<Transform>() !=
                UnsafeUtility.SizeOf<float3>() + UnsafeUtility.SizeOf<quaternion>() + UnsafeUtility.SizeOf<float3>())
            {
                throw new InvalidOperationException("Transform size mismatch");
            }
        }

        public void BlendPoses(Span<Transform> dst, ReadOnlySpan<Transform> a, ReadOnlySpan<Transform> b, float t)
        {
            fixed (Transform* pDst = dst, pA = a, pB = b)
            {
                BurstKernels.BlendPoses(pDst, pA, pB, t, dst.Length);
            }
        }

        public void BlendPosesWeighted(Span<Transform> dst, ReadOnlySpan<Transform> src, float weight)
        {
            fixed (Transform* pDst = dst, pSrc = src)
            {
                BurstKernels.BlendPosesWeighted(pDst, pDst, pSrc, weight, dst.Length);
            }
        }

        public void ComputeGlobalPose(ReadOnlySpan<Transform> local, Span<Transform> global,
            ReadOnlySpan<int> parents, int count)
        {
            fixed (Transform* pLocal = local, pGlobal = global)
            fixed (int* pParents = parents)
            {
                BurstKernels.ComputeGlobalPose(pLocal, pGlobal, pParents, count);
            }
        }

        public void BlendPosesAdditive(Span<Transform> dst, ReadOnlySpan<Transform> src, float weight)
        {
            fixed (Transform* pDst = dst, pSrc = src)
            {
                BurstKernels.BlendPosesAdditive(pDst, pDst, pSrc, weight, dst.Length);
            }
        }
    }

    [BurstCompile(FloatPrecision.Low, FloatMode.Fast)]
    public static unsafe class BurstKernels
    {
        [BurstCompile]
        public static void BlendPoses([NoAlias] Transform* dst, [NoAlias] Transform* a, [NoAlias] Transform* b,
            float t, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float3 posA = *(float3*)&a[i].Position;
                float3 posB = *(float3*)&b[i].Position;
                quaternion rotA = *(quaternion*)&a[i].Rotation;
                quaternion rotB = *(quaternion*)&b[i].Rotation;
                float3 scaleA = *(float3*)&a[i].Scale;
                float3 scaleB = *(float3*)&b[i].Scale;

                *(float3*)&dst[i].Position = math.lerp(posA, posB, t);
                *(quaternion*)&dst[i].Rotation = math.slerp(rotA, rotB, t);
                *(float3*)&dst[i].Scale = math.lerp(scaleA, scaleB, t);
            }
        }

        [BurstCompile]
        public static void BlendPosesWeighted([NoAlias] Transform* dst, [NoAlias] Transform* baseArr,
            [NoAlias] Transform* src, float weight, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float3 posBase = *(float3*)&baseArr[i].Position;
                float3 posSrc = *(float3*)&src[i].Position;
                quaternion rotBase = *(quaternion*)&baseArr[i].Rotation;
                quaternion rotSrc = *(quaternion*)&src[i].Rotation;
                float3 scaleBase = *(float3*)&baseArr[i].Scale;
                float3 scaleSrc = *(float3*)&src[i].Scale;

                *(float3*)&dst[i].Position = math.lerp(posBase, posSrc, weight);
                *(quaternion*)&dst[i].Rotation = math.slerp(rotBase, rotSrc, weight);
                *(float3*)&dst[i].Scale = math.lerp(scaleBase, scaleSrc, weight);
            }
        }

        [BurstCompile]
        public static void ComputeGlobalPose([NoAlias] Transform* local, [NoAlias] Transform* global,
            [NoAlias] int* parents, int count)
        {
            for (int i = 0; i < count; i++)
            {
                int p = parents[i];
                if (p < 0)
                {
                    *(float3*)&global[i].Position = *(float3*)&local[i].Position;
                    *(quaternion*)&global[i].Rotation = *(quaternion*)&local[i].Rotation;
                    *(float3*)&global[i].Scale = *(float3*)&local[i].Scale;
                }
                else
                {
                    float3 parentPos = *(float3*)&global[p].Position;
                    quaternion parentRot = *(quaternion*)&global[p].Rotation;
                    float3 parentScale = *(float3*)&global[p].Scale;

                    float3 localPos = *(float3*)&local[i].Position;
                    quaternion localRot = *(quaternion*)&local[i].Rotation;
                    float3 localScale = *(float3*)&local[i].Scale;

                    float3 scaledLocalPos = parentScale * localPos;
                    float3 worldPos = parentPos + math.rotate(parentRot, scaledLocalPos);
                    quaternion worldRot = math.normalize(math.mul(parentRot, localRot));
                    float3 worldScale = parentScale * localScale;

                    *(float3*)&global[i].Position = worldPos;
                    *(quaternion*)&global[i].Rotation = worldRot;
                    *(float3*)&global[i].Scale = worldScale;
                }
            }
        }

        [BurstCompile]
        public static void BlendPosesAdditive([NoAlias] Transform* dst, [NoAlias] Transform* baseArr,
            [NoAlias] Transform* src, float weight, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float3 posBase = *(float3*)&baseArr[i].Position;
                float3 posSrc = *(float3*)&src[i].Position;
                quaternion rotBase = *(quaternion*)&baseArr[i].Rotation;
                quaternion rotSrc = *(quaternion*)&src[i].Rotation;
                float3 scaleBase = *(float3*)&baseArr[i].Scale;
                float3 scaleSrc = *(float3*)&src[i].Scale;

                float3 newPos = posBase + posSrc * weight;
                quaternion addRot = math.slerp(quaternion.identity, rotSrc, weight);
                quaternion newRot = math.normalize(math.mul(rotBase, addRot));
                float3 newScale = math.lerp(scaleBase, scaleBase * scaleSrc, weight);

                *(float3*)&dst[i].Position = newPos;
                *(quaternion*)&dst[i].Rotation = newRot;
                *(float3*)&dst[i].Scale = newScale;
            }
        }
    }
}