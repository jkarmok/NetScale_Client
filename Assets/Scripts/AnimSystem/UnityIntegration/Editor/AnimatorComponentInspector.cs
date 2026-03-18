using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace AnimationSystem.Unity.Editor
{
    [CustomEditor(typeof(AnimatorComponent))]
    public sealed class AnimatorComponentInspector : UnityEditor.Editor
    {
        private SerializedProperty _skeletonProp;
        private SerializedProperty _databaseProp;
        private SerializedProperty _defaultClipProp;
        private SerializedProperty _speedProp;
        private SerializedProperty _layerConfigsProp;
        private SerializedProperty _debugProp;
        private SerializedProperty _updateEveryNFramesProp;
        private SerializedProperty _autoUpdateProp;
        private SerializedProperty _useJobSystemProp;
        private SerializedProperty _skipScaleWriteProp;
        private SerializedProperty _skipPositionWriteProp;
        private SerializedProperty _skeletonRootProp;

        private bool _showSetupHelp = false;
        private string _testClipName = "";

        private void OnEnable()
        {
            _skeletonProp = serializedObject.FindProperty("_skeletonAsset");
            _databaseProp = serializedObject.FindProperty("_databaseAsset");
            _defaultClipProp = serializedObject.FindProperty("_defaultClip");
            _speedProp = serializedObject.FindProperty("_playbackSpeed");
            _layerConfigsProp = serializedObject.FindProperty("_layerConfigs");
            _debugProp = serializedObject.FindProperty("_debugDrawSkeleton");
            _updateEveryNFramesProp = serializedObject.FindProperty("_updateEveryNFrames");
            _autoUpdateProp = serializedObject.FindProperty("_autoUpdate");
            _useJobSystemProp = serializedObject.FindProperty("_useJobSystem");
            _skipScaleWriteProp = serializedObject.FindProperty("_skipScaleWriteWhenUnused");
            _skipPositionWriteProp = serializedObject.FindProperty("_skipPositionWriteWhenUnused");
            _skeletonRootProp = serializedObject.FindProperty("_skeletonRoot");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var comp = (AnimatorComponent)target;

            // ── Header ────────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            DrawHeader(comp);
            EditorGUILayout.Space(6);

            // ── Assets ────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_skeletonProp, new GUIContent("Skeleton Asset"));
            if (_skeletonProp.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Skeleton Asset обязателен!", MessageType.Error);
            
            EditorGUILayout.PropertyField(_skeletonRootProp, new GUIContent("Skeleton Root"));
            if (_skeletonRootProp.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Skeleton Root обязателен!", MessageType.Error);
            
            EditorGUILayout.PropertyField(_databaseProp, new GUIContent("Database Asset"));

            EditorGUILayout.Space(4);

            // ── Quick links ───────────────────────────────────────────────────
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Skeleton Exporter", EditorStyles.miniButton))
                    SkeletonExporterWindow.Open();
                if (GUILayout.Button("Open Clip Exporter", EditorStyles.miniButton))
                    ClipExporterWindow.Open();
                if (GUILayout.Button("Debug Window", EditorStyles.miniButton))
                    AnimationSystemDebugWindow.Open();
            }

            EditorGUILayout.Space(6);

            // ── Playback ──────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_defaultClipProp, new GUIContent("Default Clip"));
            EditorGUILayout.PropertyField(_speedProp, new GUIContent("Playback Speed"));

            EditorGUILayout.Space(6);

            // ── Layers ────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Оставьте пустым для одного слоя (полное тело).", MessageType.None);
            EditorGUILayout.PropertyField(_layerConfigsProp, new GUIContent("Layer Configs"), true);

            EditorGUILayout.Space(6);

            // ── Performance ───────────────────────────────────────────────────
            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_updateEveryNFramesProp, new GUIContent("Update Every N Frames"));
            EditorGUILayout.PropertyField(_autoUpdateProp, new GUIContent("Auto Update"));
            EditorGUILayout.PropertyField(_useJobSystemProp, new GUIContent("Use Job System"));
            EditorGUILayout.PropertyField(_skipPositionWriteProp, new GUIContent("Skip Position Write When Unused"));
            EditorGUILayout.PropertyField(_skipScaleWriteProp, new GUIContent("Skip Scale Write When Unused"));

            EditorGUILayout.Space(6);

            // ── IK (info) ─────────────────────────────────────────────────────
            if (_skeletonProp.objectReferenceValue is SkeletonAsset skeleton && skeleton.ikChains != null && skeleton.ikChains.Count > 0)
            {
                EditorGUILayout.LabelField("IK Chains", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    foreach (var chain in skeleton.ikChains)
                    {
                        if (chain != null)
                            EditorGUILayout.LabelField($"  • {chain.chainName} ({chain.chainType})");
                    }
                }
            }

            EditorGUILayout.Space(6);

            // ── Debug ─────────────────────────────────────────────────────────
            EditorGUILayout.PropertyField(_debugProp, new GUIContent("Draw Skeleton Gizmos"));

            EditorGUILayout.Space(8);

            // ── Runtime Controls (Play Mode only) ─────────────────────────────
            if (EditorApplication.isPlaying && comp.IsInitialized)
            {
                DrawRuntimeControls(comp);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(AnimatorComponent comp)
        {
            var style = new GUIStyle(EditorStyles.helpBox);
            using var box = new EditorGUILayout.HorizontalScope(style);

            var iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("🎬", iconStyle, GUILayout.Width(32), GUILayout.Height(32));

            using var col = new EditorGUILayout.VerticalScope();
            EditorGUILayout.LabelField("AnimationSystem Animator", EditorStyles.boldLabel);

            if (EditorApplication.isPlaying)
            {
                var statusStyle = new GUIStyle(EditorStyles.miniLabel);
                statusStyle.normal.textColor = comp.IsInitialized ? Color.green : Color.red;
                EditorGUILayout.LabelField(comp.IsInitialized ? "● Running" : "● Not Initialized", statusStyle);
            }
            else
            {
                EditorGUILayout.LabelField("Edit Mode", EditorStyles.miniLabel);
            }
        }

        private void DrawRuntimeControls(AnimatorComponent comp)
        {
            EditorGUILayout.LabelField("▶ Runtime Controls", EditorStyles.boldLabel);

            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            using (new EditorGUILayout.HorizontalScope())
            {
                _testClipName = EditorGUILayout.TextField("Clip Name", _testClipName);
                if (GUILayout.Button("Play", GUILayout.Width(50)))
                {
                    if (!string.IsNullOrEmpty(_testClipName))
                        comp.Play(_testClipName);
                }
                if (GUILayout.Button("Crossfade", GUILayout.Width(72)))
                {
                    if (!string.IsNullOrEmpty(_testClipName))
                        comp.Play(_testClipName, 0.3f);
                }
            }

            // Layer weight sliders
            if (comp.Controller != null)
            {
                int layerCount = comp.Controller.LayerCount;
                for (int i = 0; i < layerCount; i++)
                {
                    float currentWeight = 1f; // Default value
                    try
                    {
                        // GetLayerWeight might not exist - check if method exists via reflection or use try-catch
                        var method = comp.Controller.GetType().GetMethod("GetLayerWeight");
                        if (method != null)
                            currentWeight = (float)method.Invoke(comp.Controller, new object[] { i });
                    }
                    catch { /* Ignore */ }
                    
                    EditorGUI.BeginChangeCheck();
                    float w = EditorGUILayout.Slider($"Layer {i} Weight", currentWeight, 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                        comp.SetLayerWeight(i, w);
                }
            }
        }
    }
}