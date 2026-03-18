using System.Collections.Generic;
using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity
{
    [CreateAssetMenu(menuName = "AnimationSystem/Database Asset", fileName = "AnimationDatabase")]
    public sealed class AnimationDatabaseAsset : ScriptableObject
    {
        [SerializeField] private List<AnimationClipAsset> _clips = new();

        public IReadOnlyList<AnimationClipAsset> Clips => _clips;

        public void RegisterForSkeleton(SkeletonAsset skeletonAsset)
        {
            if (skeletonAsset == null)
            {
                Debug.LogError("[AnimationDatabase] Skeleton asset is null", this);
                return;
            }

            var db = ANS.Animation.AnimationDatabase.Instance;
            var skeletonDef = skeletonAsset.BuildDefinition();
            int boneCount = skeletonAsset.bones.Count;

            db.SetSkeleton(skeletonDef);

            int registered = 0;
            foreach (var clipAsset in _clips)
            {
                if (clipAsset == null) continue;

                try
                {
                    var clip = clipAsset.BuildClip(boneCount);
                    db.Register(clip);
                    registered++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AnimationDatabase] Failed to register '{clipAsset.clipName}': {e.Message}", clipAsset);
                }
            }

            Debug.Log($"[AnimationDatabase] Registered {registered}/{_clips.Count} clips for skeleton '{skeletonAsset.name}'");
        }

        public void AddClip(AnimationClipAsset clip)
        {
            if (clip != null && !_clips.Contains(clip))
                _clips.Add(clip);
        }

        public void RemoveClip(AnimationClipAsset clip) => _clips.Remove(clip);

        public bool ContainsClip(AnimationClipAsset clip) => _clips.Contains(clip);
    }
}