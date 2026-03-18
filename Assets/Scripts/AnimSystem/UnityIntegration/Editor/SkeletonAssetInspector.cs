using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor
{
    [CustomEditor(typeof(SkeletonAsset))]
    public sealed class SkeletonAssetInspector : UnityEditor.Editor
    {
        private Vector2 _boneScroll;
        private Vector2 _ikScroll;
        private bool _showBones = true;
        private bool _showIK = true;
        private string _searchFilter = "";

        private int _ikEditIndex = -1;
        private string _newIKName = "";
        private IKChainType _newIKType = IKChainType.TwoBone;
        private List<int> _newIKBones = new List<int>();

        public override void OnInspectorGUI()
        {
            var asset = (SkeletonAsset)target;
            serializedObject.Update();

            EditorGUILayout.LabelField($"Skeleton: {asset.bones?.Count ?? 0} bones", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Exporter", EditorStyles.miniButton))
                    SkeletonExporterWindow.Open();
                if (GUILayout.Button("Validate", EditorStyles.miniButton))
                    ValidateSkeleton(asset);
            }

            EditorGUILayout.Space(6);

            _showBones = EditorGUILayout.BeginFoldoutHeaderGroup(_showBones, $"Bones ({asset.bones?.Count ?? 0})");
            if (_showBones)
            {
                DrawBonesSection(asset);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4);

            _showIK = EditorGUILayout.BeginFoldoutHeaderGroup(_showIK, $"IK Chains ({asset.ikChains?.Count ?? 0})");
            if (_showIK)
            {
                DrawIKSection(asset);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBonesSection(SkeletonAsset asset)
        {
            if (asset.bones == null)
            {
                EditorGUILayout.HelpBox("Bones list is null", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Search", GUILayout.Width(50));
                _searchFilter = EditorGUILayout.TextField(_searchFilter);
                if (GUILayout.Button("✕", GUILayout.Width(24))) _searchFilter = "";
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Idx", GUILayout.Width(35));
                GUILayout.Label("Par", GUILayout.Width(35));
                GUILayout.Label("Name", GUILayout.Width(140));
                GUILayout.Label("Transform Path", GUILayout.ExpandWidth(true));
                GUILayout.Label("BindPos", GUILayout.Width(160));
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_boneScroll,
                GUILayout.Height(Mathf.Min(asset.bones.Count * 20f + 10f, 300f)));
            _boneScroll = scroll.scrollPosition;

            for (int i = 0; i < asset.bones.Count; i++)
            {
                var bone = asset.bones[i];
                if (bone == null) continue;
                
                if (!string.IsNullOrEmpty(_searchFilter) &&
                    !bone.boneName.ToLower().Contains(_searchFilter.ToLower())) continue;

                int depth = GetDepth(asset, i);
                string indent = new string(' ', depth * 2);

                using var row = new EditorGUILayout.HorizontalScope(
                    i % 2 == 0 ? GUI.skin.box : GUIStyle.none);

                EditorGUILayout.LabelField(bone.boneIndex.ToString(), GUILayout.Width(35));
                EditorGUILayout.LabelField(bone.parentIndex >= 0 ? bone.parentIndex.ToString() : "—", GUILayout.Width(35));
                EditorGUILayout.LabelField(indent + bone.boneName, GUILayout.Width(140));
                EditorGUILayout.LabelField(bone.transformPath ?? "", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(
                    $"({bone.bindLocalPosition.x:F2}, {bone.bindLocalPosition.y:F2}, {bone.bindLocalPosition.z:F2})",
                    EditorStyles.miniLabel, GUILayout.Width(160));
            }
        }

        private void DrawIKSection(SkeletonAsset asset)
        {
            if (GUILayout.Button("+ Add IK Chain"))
            {
                asset.ikChains.Add(new IKChainDefinition
                {
                    chainName = "NewChain",
                    chainType = IKChainType.TwoBone,
                    boneIndices = new List<int> { 0, 1, 2 }
                });
                EditorUtility.SetDirty(asset);
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_ikScroll, GUILayout.Height(200));
            _ikScroll = scroll.scrollPosition;

            for (int i = 0; i < asset.ikChains.Count; i++)
            {
                var chain = asset.ikChains[i];
                if (chain == null) continue;
                
                using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    chain.chainName = EditorGUILayout.TextField(chain.chainName, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    chain.chainType = (IKChainType)EditorGUILayout.EnumPopup(chain.chainType, GUILayout.Width(80));
                    if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(asset);

                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        asset.ikChains.RemoveAt(i);
                        EditorUtility.SetDirty(asset);
                        i--;
                        continue;
                    }
                }

                EditorGUILayout.LabelField("Bone Chain (indices):", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;

                if (chain.boneIndices == null)
                    chain.boneIndices = new List<int>();

                for (int j = 0; j < chain.boneIndices.Count; j++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        chain.boneIndices[j] = EditorGUILayout.IntField(chain.boneIndices[j], GUILayout.Width(50));

                        int idx = chain.boneIndices[j];
                        string boneName = idx >= 0 && idx < asset.bones.Count && asset.bones[idx] != null 
                            ? asset.bones[idx].boneName 
                            : "???";
                        EditorGUILayout.LabelField(boneName, EditorStyles.miniLabel);

                        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(asset);

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            chain.boneIndices.RemoveAt(j);
                            EditorUtility.SetDirty(asset);
                            j--;
                        }
                    }
                }

                EditorGUI.indentLevel--;

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Add Bone Index", EditorStyles.miniButton))
                    {
                        chain.boneIndices.Add(0);
                        EditorUtility.SetDirty(asset);
                    }

                    if (chain.chainType == IKChainType.FABRIK)
                    {
                        EditorGUI.BeginChangeCheck();
                        chain.fabrikIterations = EditorGUILayout.IntSlider("Iterations", chain.fabrikIterations, 1, 30);
                        if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(asset);
                    }
                }

                asset.ikChains[i] = chain;
            }
        }

        private int GetDepth(SkeletonAsset asset, int boneIndex)
        {
            if (asset.bones == null || boneIndex < 0 || boneIndex >= asset.bones.Count)
                return 0;
                
            int depth = 0;
            int current = asset.bones[boneIndex].parentIndex;
            while (current >= 0 && depth < 20 && current < asset.bones.Count)
            {
                depth++;
                current = current < asset.bones.Count && asset.bones[current] != null 
                    ? asset.bones[current].parentIndex 
                    : -1;
            }
            return depth;
        }

        private void ValidateSkeleton(SkeletonAsset asset)
        {
            var errors = new List<string>();
            var names = new HashSet<string>();

            if (asset.bones == null)
            {
                errors.Add("Bones list is null");
                EditorUtility.DisplayDialog("Validation Errors", "Bones list is null", "OK");
                return;
            }

            for (int i = 0; i < asset.bones.Count; i++)
            {
                var bone = asset.bones[i];
                if (bone == null)
                {
                    errors.Add($"Bone {i}: null entry");
                    continue;
                }
                
                if (bone.boneIndex != i)
                    errors.Add($"Bone {i}: index mismatch ({bone.boneIndex} != {i})");
                if (string.IsNullOrEmpty(bone.boneName))
                    errors.Add($"Bone {i}: empty name");
                if (!string.IsNullOrEmpty(bone.boneName) && !names.Add(bone.boneName))
                    errors.Add($"Duplicate bone name: {bone.boneName}");
                if (bone.parentIndex >= i && bone.parentIndex != -1)
                    errors.Add($"Bone {bone.boneName}: parent index {bone.parentIndex} >= own index {i}");
            }

            try
            {
                var def = asset.BuildDefinition();
            }
            catch (System.Exception e)
            {
                errors.Add($"BuildDefinition failed: {e.Message}");
            }

            if (errors.Count == 0)
                EditorUtility.DisplayDialog("Validation", "✓ Skeleton is valid!", "OK");
            else
                EditorUtility.DisplayDialog("Validation Errors",
                    $"Found {errors.Count} error(s):\n\n" + string.Join("\n", errors), "OK");
        }
    }
}