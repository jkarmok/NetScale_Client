using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor
{
    [CustomEditor(typeof(AnimationClipAsset))]
    public sealed class AnimationClipAssetInspector : UnityEditor.Editor
    {
        private SerializedProperty _idProp;
        private SerializedProperty _nameProp;
        private SerializedProperty _durationProp;
        private SerializedProperty _loopProp;
        private SerializedProperty _tracksProp;

        private Vector2 _trackScroll;
        private bool _showTracks = true;

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("clipId");
            _nameProp = serializedObject.FindProperty("clipName");
            _durationProp = serializedObject.FindProperty("duration");
            _loopProp = serializedObject.FindProperty("isLooping");
            _tracksProp = serializedObject.FindProperty("tracks");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var asset = (AnimationClipAsset)target;

            // ── Header ────────────────────────────────────────────────────────
            using var header = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🎞", new GUIStyle(EditorStyles.boldLabel) { fontSize = 20 },
                GUILayout.Width(28), GUILayout.Height(28));
            using var headerCols = new EditorGUILayout.VerticalScope();
            EditorGUILayout.LabelField($"{asset.clipName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Duration: {asset.duration:F3}s  |  Tracks: {asset.tracks.Count}  |  ID: {asset.clipId}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // ── Meta ──────────────────────────────────────────────────────────
            EditorGUILayout.LabelField("Clip Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_idProp, new GUIContent("Clip ID"));
            EditorGUILayout.PropertyField(_nameProp, new GUIContent("Name"));
            EditorGUILayout.PropertyField(_durationProp, new GUIContent("Duration (sec)"));
            EditorGUILayout.PropertyField(_loopProp, new GUIContent("Is Looping"));

            EditorGUILayout.Space(6);

            // ── Tracks Summary ────────────────────────────────────────────────
            _showTracks = EditorGUILayout.BeginFoldoutHeaderGroup(_showTracks,
                $"Bone Tracks ({asset.tracks.Count})");

            if (_showTracks)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Bone Idx", GUILayout.Width(65));
                    GUILayout.Label("Pos Keys", GUILayout.Width(65));
                    GUILayout.Label("Rot Keys", GUILayout.Width(65));
                    GUILayout.Label("Scale Keys", GUILayout.Width(75));
                    GUILayout.Label("Interp", GUILayout.ExpandWidth(true));
                }

                using var scroll = new EditorGUILayout.ScrollViewScope(_trackScroll,
                    GUILayout.Height(Mathf.Min(asset.tracks.Count * 22f + 8f, 250f)));
                _trackScroll = scroll.scrollPosition;

                for (int i = 0; i < asset.tracks.Count; i++)
                {
                    var t = asset.tracks[i];
                    using (new EditorGUILayout.HorizontalScope(i % 2 == 0 ? GUI.skin.box : GUIStyle.none))
                    {
                        EditorGUILayout.LabelField(t.boneIndex.ToString(), GUILayout.Width(65));
                        EditorGUILayout.LabelField(t.positionKeys.Count.ToString(), GUILayout.Width(65));
                        EditorGUILayout.LabelField(t.rotationKeys.Count.ToString(), GUILayout.Width(65));
                        EditorGUILayout.LabelField(t.scaleKeys.Count.ToString(), GUILayout.Width(75));
                        EditorGUILayout.LabelField(t.posInterp.ToString(), GUILayout.ExpandWidth(true));
                    }
                }

                // Редактирование треков через стандартный PropertyField
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_tracksProp, new GUIContent("Edit Tracks"), true);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4);

            // ── Utilities ─────────────────────────────────────────────────────
            if (GUILayout.Button("Open Clip Exporter", EditorStyles.miniButton))
                ClipExporterWindow.Open();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
