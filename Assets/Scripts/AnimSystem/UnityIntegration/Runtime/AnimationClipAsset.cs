using System;
using System.Collections.Generic;
using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [CreateAssetMenu(menuName = "AnimationSystem/Clip Asset", fileName = "NewClipAsset")]
    public sealed class AnimationClipAsset : ScriptableObject
    {
        [Header("Clip Info")]
        public int clipId;
        public string clipName = "";
        public float duration = 1f;
        public bool isLooping = true;

        [Header("Tracks")]
        public List<BoneTrackData> tracks = new();

        public ANS.Animation.AnimationClip BuildClip(int boneCount)
        {
            var builtTracks = new ANS.Animation.BoneTrack[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                builtTracks[i] = tracks[i].Build();

            return new ANS.Animation.AnimationClip(clipId, clipName, duration, isLooping, builtTracks, boneCount);
        }

        [Serializable]
        public sealed class BoneTrackData
        {
            public int boneIndex;
            public List<Vec3KeyframeData> positionKeys = new();
            public InterpolationModeData posInterp = InterpolationModeData.Linear;
            public List<QuatKeyframeData> rotationKeys = new();
            public InterpolationModeData rotInterp = InterpolationModeData.Linear;
            public List<Vec3KeyframeData> scaleKeys = new();
            public InterpolationModeData scaleInterp = InterpolationModeData.Linear;

            public ANS.Animation.BoneTrack Build()
            {
                ANS.Animation.Vec3Curve? posCurve = null;
                if (positionKeys.Count > 0)
                {
                    var keys = new ANS.Animation.Vec3Keyframe[positionKeys.Count];
                    for (int i = 0; i < positionKeys.Count; i++)
                        keys[i] = positionKeys[i].ToANS();
                    posCurve = new ANS.Animation.Vec3Curve(keys, (ANS.Animation.InterpolationMode)posInterp);
                }

                ANS.Animation.QuatCurve? rotCurve = null;
                if (rotationKeys.Count > 0)
                {
                    var keys = new ANS.Animation.QuatKeyframe[rotationKeys.Count];
                    for (int i = 0; i < rotationKeys.Count; i++)
                        keys[i] = rotationKeys[i].ToANS();
                    rotCurve = new ANS.Animation.QuatCurve(keys, (ANS.Animation.InterpolationMode)rotInterp);
                }

                ANS.Animation.Vec3Curve? scaleCurve = null;
                if (scaleKeys.Count > 0)
                {
                    var keys = new ANS.Animation.Vec3Keyframe[scaleKeys.Count];
                    for (int i = 0; i < scaleKeys.Count; i++)
                        keys[i] = scaleKeys[i].ToANS();
                    scaleCurve = new ANS.Animation.Vec3Curve(keys, (ANS.Animation.InterpolationMode)scaleInterp);
                }

                return new ANS.Animation.BoneTrack(boneIndex, posCurve, rotCurve, scaleCurve);
            }
        }

        [Serializable]
        public sealed class Vec3KeyframeData
        {
            public float time;
            public Vector3 value;
            public Vector3 inTangent;
            public Vector3 outTangent;

            public ANS.Animation.Vec3Keyframe ToANS() => new(
                time,
                SkeletonAsset.ToANSVec3(value),
                SkeletonAsset.ToANSVec3(inTangent),
                SkeletonAsset.ToANSVec3(outTangent));
        }

        [Serializable]
        public sealed class QuatKeyframeData
        {
            public float time;
            public Quaternion value = Quaternion.identity;

            public ANS.Animation.QuatKeyframe ToANS() => new(time, SkeletonAsset.ToANSQuat(value));
        }

        public enum InterpolationModeData { Step = 0, Linear = 1, CubicHermite = 2 }
    }
}