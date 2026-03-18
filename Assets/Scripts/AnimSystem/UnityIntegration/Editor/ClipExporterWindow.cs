using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimationSystem.Unity.Conversion;

namespace AnimationSystem.Unity.Editor
{
    public sealed class ClipExporterWindow : EditorWindow
    {
        [MenuItem("AnimationSystem/Clip Exporter", priority = 2)]
        public static void Open() => GetWindow<ClipExporterWindow>("Clip Exporter");

        private SkeletonAsset _skeleton;
        private AnimationDatabaseAsset _database;
        private GameObject _referenceRoot;

        private List<ClipEntry> _clips = new List<ClipEntry>();
        private Vector2 _scroll;

        private string _outputFolder = "Assets/AnimationSystem/Clips";
        private float _sampleRate = 30f;
        private bool _useSampling = true;
        private int _nextClipId = 100;

        private bool _addToDatabase = true;

        [Serializable]
        private struct ClipEntry
        {
            public AnimationClip UnityClip;
            public string ClipName;
            public int ClipId;
            public bool IsLooping;
            public bool Selected;
            public string Status;
        }

        private void OnGUI()
        {
            GUILayout.Label("Clip Exporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Конвертирует Unity AnimationClip в AnimationClipAsset.\n" +
                "Drag & Drop клипы снизу или нажмите 'Add Clips from Selection'.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            DrawConfigSection();
            EditorGUILayout.Space(8);
            DrawClipList();
            EditorGUILayout.Space(8);
            DrawExportSection();
        }

        private void DrawConfigSection()
        {
            using var foldout = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            _skeleton = (SkeletonAsset)EditorGUILayout.ObjectField(
                "Skeleton Asset *", _skeleton, typeof(SkeletonAsset), false);

            _database = (AnimationDatabaseAsset)EditorGUILayout.ObjectField(
                "Database Asset", _database, typeof(AnimationDatabaseAsset), false);

            _referenceRoot = (GameObject)EditorGUILayout.ObjectField(
                "Reference Character", _referenceRoot, typeof(GameObject), true);

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                _useSampling = EditorGUILayout.Toggle("Use Sampling", _useSampling);
                if (_useSampling)
                {
                    EditorGUILayout.LabelField("Sample Rate", GUILayout.Width(80));
                    _sampleRate = EditorGUILayout.Slider(_sampleRate, 15f, 120f);
                }
            }

            if (!_useSampling)
                EditorGUILayout.HelpBox("Keyframe mode: напрямую читает ключи из кривых. Быстрее, но не все клипы поддерживают.", MessageType.None);
            if (_useSampling && _referenceRoot == null)
                EditorGUILayout.HelpBox("Для Sampling нужен Reference Character в сцене!", MessageType.Warning);

            _addToDatabase = EditorGUILayout.Toggle("Auto-Add to Database", _addToDatabase);
            _nextClipId = EditorGUILayout.IntField("Next Clip ID", _nextClipId);

            using (new EditorGUILayout.HorizontalScope())
            {
                _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
                if (GUILayout.Button("…", GUILayout.Width(24)))
                {
                    string p = EditorUtility.OpenFolderPanel("Output folder", _outputFolder, "");
                    if (!string.IsNullOrEmpty(p))
                    {
                        if (p.StartsWith(Application.dataPath))
                            _outputFolder = "Assets" + p.Substring(Application.dataPath.Length);
                        else
                            _outputFolder = p;
                    }
                }
            }
        }

        private void DrawClipList()
        {
            EditorGUILayout.LabelField($"Clips to Convert ({_clips.Count})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ From Selection"))
                    AddFromSelection();
                if (GUILayout.Button("+ Drop Zone"))
                    HandleDropZone();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    _clips.Clear();
            }

            var dropRect = EditorGUILayout.GetControlRect(false, 40f);
            EditorGUI.DrawRect(dropRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            GUI.Label(dropRect, "← Drop AnimationClips Here →",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 });
            HandleDropEvent(dropRect);

            EditorGUILayout.Space(4);

            if (_clips.Count == 0)
            {
                EditorGUILayout.LabelField("No clips added.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("", GUILayout.Width(18));
                GUILayout.Label("Unity Clip", GUILayout.Width(160));
                GUILayout.Label("Export Name", GUILayout.Width(140));
                GUILayout.Label("ID", GUILayout.Width(45));
                GUILayout.Label("Loop", GUILayout.Width(40));
                GUILayout.Label("Status", GUILayout.ExpandWidth(true));
                GUILayout.Label("", GUILayout.Width(24));
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(200));
            _scroll = scroll.scrollPosition;

            for (int i = 0; i < _clips.Count; i++)
            {
                var entry = _clips[i];
                using (new EditorGUILayout.HorizontalScope(i % 2 == 0 ? GUI.skin.box : GUIStyle.none))
                {
                    bool selected = EditorGUILayout.Toggle(entry.Selected, GUILayout.Width(18));

                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.ObjectField(entry.UnityClip, typeof(AnimationClip), false, GUILayout.Width(160));

                    string clipName = EditorGUILayout.TextField(entry.ClipName, GUILayout.Width(140));
                    int clipId = EditorGUILayout.IntField(entry.ClipId, GUILayout.Width(45));
                    bool isLooping = EditorGUILayout.Toggle(entry.IsLooping, GUILayout.Width(40));

                    var color = entry.Status == "OK" ? Color.green
                        : entry.Status != null && entry.Status.StartsWith("ERR") ? Color.red
                        : Color.gray;
                    var oldColor = GUI.contentColor;
                    GUI.contentColor = color;
                    EditorGUILayout.LabelField(entry.Status ?? "—", GUILayout.ExpandWidth(true));
                    GUI.contentColor = oldColor;

                    if (GUILayout.Button("✕", GUILayout.Width(24)))
                    {
                        _clips.RemoveAt(i);
                        i--;
                        continue;
                    }

                    _clips[i] = new ClipEntry
                    {
                        UnityClip = entry.UnityClip,
                        ClipName = clipName,
                        ClipId = clipId,
                        IsLooping = isLooping,
                        Selected = selected,
                        Status = entry.Status
                    };
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All")) SetAllSelected(true);
                if (GUILayout.Button("Deselect All")) SetAllSelected(false);
            }
        }

        private void DrawExportSection()
        {
            if (_skeleton == null)
            {
                EditorGUILayout.HelpBox("Назначьте Skeleton Asset!", MessageType.Error);
            }

            int selectedCount = 0;
            foreach (var c in _clips) if (c.Selected) selectedCount++;

            using (new EditorGUI.DisabledScope(_skeleton == null || selectedCount == 0))
            {
                if (GUILayout.Button($"▶  Convert {selectedCount} Clip(s)", GUILayout.Height(36)))
                    ConvertSelected();
            }
        }

        private void AddFromSelection()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is AnimationClip clip)
                    AddClip(clip);
            }
        }

        private void AddClip(AnimationClip clip)
        {
            foreach (var e in _clips)
                if (e.UnityClip == clip) return;

            _clips.Add(new ClipEntry
            {
                UnityClip = clip,
                ClipName = clip != null ? clip.name : "Unknown",
                ClipId = _nextClipId++,
                IsLooping = clip != null && clip.isLooping,
                Selected = true,
                Status = "—"
            });
        }

        private void HandleDropZone() { }

        private void HandleDropEvent(Rect rect)
        {
            var ev = Event.current;
            if (ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform)
            {
                if (!rect.Contains(ev.mousePosition)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (ev.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                        if (obj is AnimationClip c) AddClip(c);
                    ev.Use();
                    Repaint();
                }
            }
        }

        private void ConvertSelected()
        {
            EnsureOutputFolder();
            bool anyError = false;

            for (int i = 0; i < _clips.Count; i++)
            {
                var entry = _clips[i];
                if (!entry.Selected) continue;

                try
                {
                    if (entry.UnityClip == null)
                    {
                        throw new Exception("Unity Clip is null");
                    }

                    AnimationClipAsset clipAsset;

                    if (_useSampling && _referenceRoot != null)
                    {
                        clipAsset = UnityClipConverter.ConvertBySampling(
                            entry.UnityClip, _referenceRoot, _skeleton, entry.ClipId, _sampleRate);
                    }
                    else
                    {
                        clipAsset = UnityClipConverter.Convert(
                            entry.UnityClip, _skeleton, entry.ClipId);
                    }

                    if (clipAsset == null)
                    {
                        throw new Exception("Failed to create clip asset");
                    }

                    if (clipAsset.tracks == null || clipAsset.tracks.Count == 0)
                    {
                        throw new Exception("Animation has no tracks");
                    }

                    clipAsset.clipName = entry.ClipName;
                    clipAsset.clipId = entry.ClipId;
                    clipAsset.isLooping = entry.IsLooping;

                    string path = $"{_outputFolder}/{SanitizeFileName(entry.ClipName)}.asset";
                    EnsureParentFolder(path);
                    
                    // Delete existing asset if it exists
                    var existing = AssetDatabase.LoadAssetAtPath<AnimationClipAsset>(path);
                    if (existing != null)
                        AssetDatabase.DeleteAsset(path);
                    
                    AssetDatabase.CreateAsset(clipAsset, path);

                    if (_addToDatabase && _database != null)
                    {
                        _database.AddClip(clipAsset);
                        EditorUtility.SetDirty(_database);
                    }

                    entry.Status = "OK";
                    Debug.Log($"[ClipExporter] Exported '{entry.ClipName}' → {path}");
                }
                catch (System.Exception ex)
                {
                    entry.Status = $"ERR: {ex.Message}";
                    Debug.LogError($"[ClipExporter] Failed '{entry.ClipName}': {ex.Message}");
                    anyError = true;
                }

                _clips[i] = entry;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int successCount = 0;
            foreach (var c in _clips) if (c.Status == "OK") successCount++;
            
            string msg = anyError
                ? $"Конвертация завершена с ошибками. Успешно: {successCount}"
                : $"Успешно конвертировано {successCount} клипов.";
            EditorUtility.DisplayDialog(anyError ? "Partial Success" : "Done", msg, "OK");
        }

        private string SanitizeFileName(string fileName)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');
            return fileName;
        }

        private void SetAllSelected(bool v)
        {
            for (int i = 0; i < _clips.Count; i++)
            {
                var e = _clips[i];
                e.Selected = v;
                _clips[i] = e;
            }
        }

        private void EnsureOutputFolder()
        {
            if (string.IsNullOrEmpty(_outputFolder)) return;
            
            var parts = _outputFolder.Split('/');
            if (parts.Length == 0) return;
            
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static void EnsureParentFolder(string assetPath)
        {
            string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir)) return;
            
            var parts = dir.Split('/');
            if (parts.Length == 0) return;
            
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}