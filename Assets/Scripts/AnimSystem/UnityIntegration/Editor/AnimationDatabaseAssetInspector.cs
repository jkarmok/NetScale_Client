using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor
{
    [CustomEditor(typeof(AnimationDatabaseAsset))]
    public sealed class AnimationDatabaseAssetInspector : UnityEditor.Editor
    {
        private SerializedProperty _clipsProp;
        private Vector2 _scroll;
        private string _filter = "";

        private void OnEnable()
        {
            _clipsProp = serializedObject.FindProperty("_clips");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var db = (AnimationDatabaseAsset)target;

            EditorGUILayout.LabelField($"Animation Database — {_clipsProp.arraySize} clips", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Clip Exporter", EditorStyles.miniButton))
                    ClipExporterWindow.Open();
                if (GUILayout.Button("Find All Clips", EditorStyles.miniButton))
                    FindAllClipsInProject(db);
            }

            EditorGUILayout.Space(4);

            // Filter
            using (new EditorGUILayout.HorizontalScope())
            {
                _filter = EditorGUILayout.TextField("Filter", _filter);
                if (GUILayout.Button("✕", GUILayout.Width(24))) _filter = "";
            }

            EditorGUILayout.Space(4);

            // Header
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ID", GUILayout.Width(45));
                GUILayout.Label("Name", GUILayout.Width(160));
                GUILayout.Label("Duration", GUILayout.Width(70));
                GUILayout.Label("Loop", GUILayout.Width(40));
                GUILayout.Label("Tracks", GUILayout.Width(50));
                GUILayout.Label("", GUILayout.Width(24));
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll,
                GUILayout.Height(Mathf.Min(_clipsProp.arraySize * 22f + 10f, 350f)));
            _scroll = scroll.scrollPosition;

            for (int i = 0; i < _clipsProp.arraySize; i++)
            {
                var elem = _clipsProp.GetArrayElementAtIndex(i);
                var clipAsset = elem.objectReferenceValue as AnimationClipAsset;
                if (clipAsset == null) continue;

                if (!string.IsNullOrEmpty(_filter) &&
                    !clipAsset.clipName.ToLower().Contains(_filter.ToLower())) continue;

                using (new EditorGUILayout.HorizontalScope(i % 2 == 0 ? GUI.skin.box : GUIStyle.none))
                {
                    EditorGUILayout.LabelField(clipAsset.clipId.ToString(), GUILayout.Width(45));

                    if (GUILayout.Button(clipAsset.clipName, EditorStyles.linkLabel, GUILayout.Width(160)))
                        Selection.activeObject = clipAsset;

                    EditorGUILayout.LabelField($"{clipAsset.duration:F2}s", GUILayout.Width(70));
                    EditorGUILayout.LabelField(clipAsset.isLooping ? "✓" : "—", GUILayout.Width(40));
                    EditorGUILayout.LabelField(clipAsset.tracks.Count.ToString(), GUILayout.Width(50));

                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        db.RemoveClip(clipAsset);
                        EditorUtility.SetDirty(db);
                        serializedObject.Update();
                        i--;
                    }
                }
            }

            EditorGUILayout.Space(4);

            // Drop zone
            var dropRect = EditorGUILayout.GetControlRect(false, 36f);
            EditorGUI.DrawRect(dropRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            GUI.Label(dropRect, "← Drop AnimationClipAssets Here to Add →",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
            HandleDrop(dropRect, db);

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleDrop(Rect rect, AnimationDatabaseAsset db)
        {
            var ev = Event.current;
            if ((ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform) &&
                rect.Contains(ev.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (ev.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is AnimationClipAsset clip)
                        {
                            db.AddClip(clip);
                            EditorUtility.SetDirty(db);
                        }
                    }
                    ev.Use();
                    Repaint();
                }
            }
        }

        private void FindAllClipsInProject(AnimationDatabaseAsset db)
        {
            var guids = AssetDatabase.FindAssets("t:AnimationClipAsset");
            int added = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(path);
                if (clip != null && !db.ContainsClip(clip))
                {
                    db.AddClip(clip);
                    added++;
                }
            }
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationDatabase] Added {added} clips from project.");
        }
    }
}
