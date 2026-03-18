using System.Collections.Generic;
using UnityEngine;
using static AnimationSystem.Unity.AnimationClipAsset;

namespace AnimationSystem.Unity.Conversion
{
    public static class UnityClipConverter
    {
        public static AnimationClipAsset Convert(AnimationClip unityClip, SkeletonAsset skeleton, int clipId)
        {
            var asset = CreateAsset(unityClip, clipId);
            var pathToIndex = BuildPathIndex(skeleton);
            ConvertByKeyframes(unityClip, asset, pathToIndex);
            return asset;
        }

        public static AnimationClipAsset ConvertBySampling(
            AnimationClip clip,
            GameObject referenceRoot,
            SkeletonAsset skeleton,
            int clipId,
            float sampleRate = 30f)
        {
            var asset = CreateAsset(clip, clipId);
            var pathToIndex = BuildPathIndex(skeleton);

#if UNITY_EDITOR
            var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
            var animatedPaths = new HashSet<string>();
            foreach (var b in bindings)
                animatedPaths.Add(b.path);

            float dt = 1f / sampleRate;
            int frameCount = Mathf.CeilToInt(clip.length * sampleRate) + 1;

            var snapshots = CaptureTransformSnapshots(referenceRoot, animatedPaths, pathToIndex);
            var tracksByPath = InitializeTracks(animatedPaths, pathToIndex, referenceRoot);
            var (hasPos, hasRot, hasScale) = AnalyzeBindings(bindings, pathToIndex);

            SampleAnimation(clip, referenceRoot, tracksByPath, hasPos, hasRot, hasScale, dt, frameCount);
            RestoreTransformSnapshots(referenceRoot, snapshots);

            foreach (var track in tracksByPath.Values)
            {
                if (track.positionKeys.Count > 0 || track.rotationKeys.Count > 0 || track.scaleKeys.Count > 0)
                    asset.tracks.Add(track);
            }
#else
            Debug.LogError("[UnityClipConverter] ConvertBySampling is Editor-only");
#endif
            return asset;
        }

        private static void ConvertByKeyframes(AnimationClip clip, AnimationClipAsset asset, Dictionary<string, int> pathToIndex)
        {
#if UNITY_EDITOR
            var allBindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
            var byPath = new Dictionary<string, CurveSet>();

            foreach (var binding in allBindings)
            {
                if (!pathToIndex.ContainsKey(binding.path)) continue;

                if (!byPath.TryGetValue(binding.path, out var cs))
                {
                    cs = new CurveSet();
                    byPath[binding.path] = cs;
                }

                var curve = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding);
                AssignCurve(cs, binding.propertyName, curve);
            }

            foreach (var kvp in byPath)
            {
                if (!pathToIndex.TryGetValue(kvp.Key, out int boneIndex)) continue;
                var track = BuildTrackFromCurves(kvp.Value, boneIndex);
                if (track != null)
                    asset.tracks.Add(track);
            }
#endif
        }

#if UNITY_EDITOR
        private static void AssignCurve(CurveSet cs, string propertyName, AnimationCurve curve)
        {
            switch (propertyName)
            {
                case "m_LocalPosition.x": cs.px = curve; break;
                case "m_LocalPosition.y": cs.py = curve; break;
                case "m_LocalPosition.z": cs.pz = curve; break;

                case "m_LocalRotation.x": cs.rx = curve; break;
                case "m_LocalRotation.y": cs.ry = curve; break;
                case "m_LocalRotation.z": cs.rz = curve; break;
                case "m_LocalRotation.w": cs.rw = curve; break;

                case "localEulerAnglesRaw.x": cs.ex = curve; break;
                case "localEulerAnglesRaw.y": cs.ey = curve; break;
                case "localEulerAnglesRaw.z": cs.ez = curve; break;

                case "m_LocalScale.x": cs.sx = curve; break;
                case "m_LocalScale.y": cs.sy = curve; break;
                case "m_LocalScale.z": cs.sz = curve; break;
            }
        }

        private static BoneTrackData BuildTrackFromCurves(CurveSet cs, int boneIndex)
        {
            var track = new BoneTrackData { boneIndex = boneIndex };

            BuildPositionKeys(cs, track);
            BuildRotationKeys(cs, track);
            BuildScaleKeys(cs, track);

            if (track.positionKeys.Count == 0 && track.rotationKeys.Count == 0 && track.scaleKeys.Count == 0)
                return null;

            return track;
        }

        private static void BuildPositionKeys(CurveSet cs, BoneTrackData track)
        {
            if (cs.px == null || cs.py == null || cs.pz == null) return;

            var times = CollectTimes(cs.px, cs.py, cs.pz);
            foreach (float t in times)
            {
                track.positionKeys.Add(new Vec3KeyframeData
                {
                    time = t,
                    value = new Vector3(cs.px.Evaluate(t), cs.py.Evaluate(t), cs.pz.Evaluate(t)),
                    inTangent = SampleTangent(cs.px, cs.py, cs.pz, t, false),
                    outTangent = SampleTangent(cs.px, cs.py, cs.pz, t, true)
                });
            }
            track.posInterp = InterpolationModeData.CubicHermite;
        }

        private static void BuildRotationKeys(CurveSet cs, BoneTrackData track)
        {
            if (cs.rx != null && cs.ry != null && cs.rz != null && cs.rw != null)
            {
                var times = CollectTimes(cs.rx, cs.ry, cs.rz, cs.rw);
                foreach (float t in times)
                {
                    var q = new Quaternion(cs.rx.Evaluate(t), cs.ry.Evaluate(t), cs.rz.Evaluate(t), cs.rw.Evaluate(t));
                    q.Normalize();
                    track.rotationKeys.Add(new QuatKeyframeData { time = t, value = q });
                }
                track.rotInterp = InterpolationModeData.Linear;
            }
            else if (cs.ex != null && cs.ey != null && cs.ez != null)
            {
                var times = CollectTimes(cs.ex, cs.ey, cs.ez);
                foreach (float t in times)
                {
                    var euler = new Vector3(cs.ex.Evaluate(t), cs.ey.Evaluate(t), cs.ez.Evaluate(t));
                    track.rotationKeys.Add(new QuatKeyframeData { time = t, value = Quaternion.Euler(euler) });
                }
                track.rotInterp = InterpolationModeData.Linear;
            }
        }

        private static void BuildScaleKeys(CurveSet cs, BoneTrackData track)
        {
            if (cs.sx == null || cs.sy == null || cs.sz == null) return;

            var times = CollectTimes(cs.sx, cs.sy, cs.sz);
            foreach (float t in times)
            {
                track.scaleKeys.Add(new Vec3KeyframeData
                {
                    time = t,
                    value = new Vector3(cs.sx.Evaluate(t), cs.sy.Evaluate(t), cs.sz.Evaluate(t))
                });
            }
            track.scaleInterp = InterpolationModeData.Linear;
        }

        private static Dictionary<string, (Vector3 pos, Quaternion rot, Vector3 scale)> CaptureTransformSnapshots(
            GameObject root, HashSet<string> paths, Dictionary<string, int> pathToIndex)
        {
            var snapshots = new Dictionary<string, (Vector3, Quaternion, Vector3)>();
            foreach (var path in paths)
            {
                if (!pathToIndex.ContainsKey(path)) continue;
                var t = root.transform.Find(path);
                if (t != null)
                    snapshots[path] = (t.localPosition, t.localRotation, t.localScale);
            }
            return snapshots;
        }

        private static void RestoreTransformSnapshots(
            GameObject root,
            Dictionary<string, (Vector3 pos, Quaternion rot, Vector3 scale)> snapshots)
        {
            foreach (var kvp in snapshots)
            {
                var t = root.transform.Find(kvp.Key);
                if (t == null) continue;
                t.localPosition = kvp.Value.pos;
                t.localRotation = kvp.Value.rot;
                t.localScale = kvp.Value.scale;
            }
        }

        private static Dictionary<string, BoneTrackData> InitializeTracks(
            HashSet<string> paths, Dictionary<string, int> pathToIndex, GameObject root)
        {
            var tracks = new Dictionary<string, BoneTrackData>();
            foreach (var path in paths)
            {
                if (!pathToIndex.TryGetValue(path, out int boneIndex)) continue;
                if (root.transform.Find(path) == null) continue;
                tracks[path] = new BoneTrackData
                {
                    boneIndex = boneIndex,
                    posInterp = InterpolationModeData.Linear,
                    rotInterp = InterpolationModeData.Linear,
                    scaleInterp = InterpolationModeData.Linear
                };
            }
            return tracks;
        }

        private static (HashSet<string> pos, HashSet<string> rot, HashSet<string> scale) AnalyzeBindings(
            UnityEditor.EditorCurveBinding[] bindings, Dictionary<string, int> pathToIndex)
        {
            var hasPos = new HashSet<string>();
            var hasRot = new HashSet<string>();
            var hasScale = new HashSet<string>();

            foreach (var b in bindings)
            {
                if (!pathToIndex.ContainsKey(b.path)) continue;

                if (b.propertyName.StartsWith("m_LocalPosition") || b.propertyName.StartsWith("localPosition"))
                    hasPos.Add(b.path);
                if (b.propertyName.StartsWith("m_LocalRotation") || b.propertyName.StartsWith("localEulerAngles"))
                    hasRot.Add(b.path);
                if (b.propertyName.StartsWith("m_LocalScale") || b.propertyName.StartsWith("localScale"))
                    hasScale.Add(b.path);
            }

            return (hasPos, hasRot, hasScale);
        }

        private static void SampleAnimation(
            AnimationClip clip, GameObject root,
            Dictionary<string, BoneTrackData> tracks,
            HashSet<string> hasPos, HashSet<string> hasRot, HashSet<string> hasScale,
            float dt, int frameCount)
        {
            for (int f = 0; f < frameCount; f++)
            {
                float time = Mathf.Min(f * dt, clip.length);
                clip.SampleAnimation(root, time);

                foreach (var kvp in tracks)
                {
                    var t = root.transform.Find(kvp.Key);
                    if (t == null) continue;
                    var track = kvp.Value;

                    if (hasPos.Contains(kvp.Key))
                        track.positionKeys.Add(new Vec3KeyframeData { time = time, value = t.localPosition });

                    if (hasRot.Contains(kvp.Key))
                        track.rotationKeys.Add(new QuatKeyframeData { time = time, value = t.localRotation });

                    if (hasScale.Contains(kvp.Key))
                        track.scaleKeys.Add(new Vec3KeyframeData { time = time, value = t.localScale });
                }
            }
        }
#endif

        private static AnimationClipAsset CreateAsset(AnimationClip clip, int clipId)
        {
            var asset = ScriptableObject.CreateInstance<AnimationClipAsset>();
            asset.clipId = clipId;
            asset.clipName = clip.name;
            asset.duration = clip.length;
            asset.isLooping = clip.isLooping;
            asset.tracks = new List<BoneTrackData>();
            return asset;
        }

        private static Dictionary<string, int> BuildPathIndex(SkeletonAsset skeleton)
        {
            var map = new Dictionary<string, int>();
            foreach (var entry in skeleton.bones)
            {
                if (!string.IsNullOrEmpty(entry.transformPath))
                    map[entry.transformPath] = entry.boneIndex;
            }
            return map;
        }

        private static List<float> CollectTimes(params AnimationCurve[] curves)
        {
            var set = new SortedSet<float>();
            foreach (var c in curves)
            {
                if (c != null)
                {
                    foreach (var k in c.keys)
                        set.Add(k.time);
                }
            }
            return new List<float>(set);
        }

        private static Vector3 SampleTangent(AnimationCurve cx, AnimationCurve cy, AnimationCurve cz, float t, bool outDir)
        {
            const float eps = 0.0001f;
            float t0 = outDir ? t : t - eps;
            float t1 = outDir ? t + eps : t;
            return new Vector3(
                (cx.Evaluate(t1) - cx.Evaluate(t0)) / eps,
                (cy.Evaluate(t1) - cy.Evaluate(t0)) / eps,
                (cz.Evaluate(t1) - cz.Evaluate(t0)) / eps);
        }

        private sealed class CurveSet
        {
            public AnimationCurve px, py, pz;
            public AnimationCurve rx, ry, rz, rw;
            public AnimationCurve ex, ey, ez;
            public AnimationCurve sx, sy, sz;
        }
    }
}