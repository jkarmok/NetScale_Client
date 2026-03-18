using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor
{
    public sealed class SkeletonExporterWindow : EditorWindow
    {
        [MenuItem("AnimationSystem/Skeleton Exporter", priority = 1)]
        public static void Open() => GetWindow<SkeletonExporterWindow>("Skeleton Exporter");

        // ─── State ────────────────────────────────────────────────────────────

        private GameObject _targetRoot;
        private SkeletonAsset _existingAsset;
        private string _savePath = "Assets/AnimationSystem/Skeletons/NewSkeleton.asset";

        private List<BoneEntry> _detectedBones = new List<BoneEntry>();
        private Vector2 _boneListScroll;
        private bool _autoDetectFromAvatar = true;
        private Avatar _avatar;

        private struct BoneEntry
        {
            public int Index;
            public string Name;
            public int ParentIndex;
            public string Path;          // пустая строка ТОЛЬКО для корня
            public Transform Transform;
            public bool Include;
            public bool IsRoot;          // флаг: это сам _targetRoot.transform
        }

        // ─── GUI ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            GUILayout.Label("Skeleton Exporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Сканирует иерархию GameObject и создаёт SkeletonAsset.\n" +
                "Корень персонажа (сам GameObject) автоматически исключается из анимируемых костей — " +
                "в SkeletonAsset он нужен только для задания bind-pose. " +
                "Анимируются только дочерние Transform-ы.",
                MessageType.Info);

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                _targetRoot = (GameObject)EditorGUILayout.ObjectField(
                    "Character Root", _targetRoot, typeof(GameObject), true);
                if (_targetRoot == null && GUILayout.Button("From Selection", GUILayout.Width(110)))
                    _targetRoot = Selection.activeGameObject;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _autoDetectFromAvatar = EditorGUILayout.Toggle("Use Avatar", _autoDetectFromAvatar);
                if (_autoDetectFromAvatar)
                    _avatar = (Avatar)EditorGUILayout.ObjectField(_avatar, typeof(Avatar), false);
            }

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_targetRoot == null))
            {
                if (GUILayout.Button("▶  Scan Hierarchy", GUILayout.Height(28)))
                    ScanHierarchy();
            }

            if (_detectedBones.Count > 0)
                DrawBoneList();

            EditorGUILayout.Space(8);
            DrawExportSection();
        }

        private void DrawBoneList()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField($"Detected Bones ({_detectedBones.Count})", EditorStyles.boldLabel);

            int rootCount = _detectedBones.Count(b => b.IsRoot);
            int excludedCount = _detectedBones.Count(b => !b.Include);
            if (rootCount > 0)
                EditorGUILayout.HelpBox(
                    $"{rootCount} корневой Transform помечен IsRoot — он записывается в SkeletonAsset " +
                    "только как bind-pose и не анимируется напрямую.",
                    MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All"))       SetAllInclusion(true);
                if (GUILayout.Button("Deselect All"))     SetAllInclusion(false);
                if (GUILayout.Button("Select Humanoid"))  SelectHumanoidBones();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("✓",    GUILayout.Width(18));
                GUILayout.Label("Idx",  GUILayout.Width(30));
                GUILayout.Label("Par",  GUILayout.Width(30));
                GUILayout.Label("Name / Transform",  GUILayout.ExpandWidth(true));
                GUILayout.Label("Path", GUILayout.Width(200));
                GUILayout.Label("Root?",GUILayout.Width(42));
            }

            using var scroll = new EditorGUILayout.ScrollViewScope(_boneListScroll, GUILayout.Height(320));
            _boneListScroll = scroll.scrollPosition;

            for (int i = 0; i < _detectedBones.Count; i++)
            {
                var entry = _detectedBones[i];

                // Корневые кости подсвечиваем другим цветом
                var bgStyle = entry.IsRoot
                    ? new GUIStyle(EditorStyles.helpBox)
                    : (i % 2 == 0 ? GUI.skin.box : GUIStyle.none);

                using var row = new EditorGUILayout.HorizontalScope(bgStyle);

                entry.Include = EditorGUILayout.Toggle(entry.Include, GUILayout.Width(18));
                EditorGUILayout.LabelField(entry.Index.ToString(), GUILayout.Width(30));
                EditorGUILayout.LabelField(entry.ParentIndex >= 0 ? entry.ParentIndex.ToString() : "—", GUILayout.Width(30));

                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(entry.Transform, typeof(Transform), true, GUILayout.ExpandWidth(true));

                EditorGUILayout.LabelField(
                    string.IsNullOrEmpty(entry.Path) ? "(root)" : entry.Path,
                    EditorStyles.miniLabel, GUILayout.Width(200));

                EditorGUILayout.LabelField(entry.IsRoot ? "ROOT" : "", EditorStyles.miniLabel, GUILayout.Width(42));
                _detectedBones[i] = entry;
            }
        }

        private void DrawExportSection()
        {
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _savePath = EditorGUILayout.TextField("Save Path", _savePath);
                if (GUILayout.Button("…", GUILayout.Width(24)))
                {
                    string p = EditorUtility.SaveFilePanelInProject(
                        "Save Skeleton Asset", "NewSkeleton", "asset", "Choose path");
                    if (!string.IsNullOrEmpty(p)) _savePath = p;
                }
            }

            _existingAsset = (SkeletonAsset)EditorGUILayout.ObjectField(
                "Update Existing", _existingAsset, typeof(SkeletonAsset), false);

            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(_detectedBones.Count == 0))
            {
                if (GUILayout.Button("💾  Export Skeleton Asset", GUILayout.Height(34)))
                    ExportAsset();
            }
        }

        // ─── Scan ─────────────────────────────────────────────────────────────

        private void ScanHierarchy()
        {
            _detectedBones.Clear();
            if (_targetRoot == null) return;

            var indexMap = new Dictionary<Transform, int>();

            if (_autoDetectFromAvatar && _avatar != null && _avatar.isHuman)
                ScanFromAvatar(indexMap);
            else
                ScanDepthFirst(_targetRoot.transform, -1, indexMap);
        }

        private void ScanDepthFirst(Transform current, int parentIndex, Dictionary<Transform, int> indexMap)
        {
            int myIndex = _detectedBones.Count;
            indexMap[current] = myIndex;

            // ── ИСПРАВЛЕНИЕ: корень персонажа (_targetRoot.transform) получает
            //    пустой Path и помечается IsRoot. Он НЕ является костью в смысле анимации —
            //    его не двигают треки клипа, но его bind-pose нужна для корректных
            //    глобальных трансформов.
            bool isRoot = (current == _targetRoot.transform);
            string path = isRoot ? "" : GetRelativePath(current, _targetRoot.transform);

            _detectedBones.Add(new BoneEntry
            {
                Index       = myIndex,
                Name        = current.name,
                ParentIndex = parentIndex,
                Path        = path,
                Transform   = current,
                Include     = true,
                IsRoot      = isRoot
            });

            foreach (Transform child in current)
                ScanDepthFirst(child, myIndex, indexMap);
        }

        private void ScanFromAvatar(Dictionary<Transform, int> indexMap)
        {
            var humanBoneNames = new[]
            {
                "Hips","Spine","Chest","UpperChest","Neck","Head",
                "LeftShoulder","LeftUpperArm","LeftLowerArm","LeftHand",
                "RightShoulder","RightUpperArm","RightLowerArm","RightHand",
                "LeftUpperLeg","LeftLowerLeg","LeftFoot","LeftToes",
                "RightUpperLeg","RightLowerLeg","RightFoot","RightToes",
            };

            var allTransforms = _targetRoot.GetComponentsInChildren<Transform>(true);
            var transformByName = new Dictionary<string, Transform>();
            foreach (var t in allTransforms)
                transformByName[t.name] = t;

            // Добавляем корень первым
            int rootIdx = _detectedBones.Count;
            indexMap[_targetRoot.transform] = rootIdx;
            _detectedBones.Add(new BoneEntry
            {
                Index = rootIdx, Name = _targetRoot.name, ParentIndex = -1,
                Path = "", Transform = _targetRoot.transform, Include = true, IsRoot = true
            });

            foreach (var boneName in humanBoneNames)
            {
                if (!transformByName.TryGetValue(boneName, out var t)) continue;
                if (indexMap.ContainsKey(t)) continue;

                int myIndex = _detectedBones.Count;
                int parentIndex = -1;
                var pt = t.parent;
                while (pt != null)
                {
                    if (indexMap.TryGetValue(pt, out int pi)) { parentIndex = pi; break; }
                    pt = pt.parent;
                }

                indexMap[t] = myIndex;
                _detectedBones.Add(new BoneEntry
                {
                    Index = myIndex, Name = boneName, ParentIndex = parentIndex,
                    Path = GetRelativePath(t, _targetRoot.transform),
                    Transform = t, Include = true, IsRoot = false
                });
            }

            // Добавляем пропущенные через BFS
            var queue = new Queue<Transform>();
            queue.Enqueue(_targetRoot.transform);
            while (queue.Count > 0)
            {
                var t = queue.Dequeue();
                if (!indexMap.ContainsKey(t))
                {
                    int myIndex = _detectedBones.Count;
                    int parentIndex = -1;
                    var pt = t.parent;
                    while (pt != null)
                    {
                        if (indexMap.TryGetValue(pt, out int pi)) { parentIndex = pi; break; }
                        pt = pt.parent;
                    }
                    indexMap[t] = myIndex;
                    _detectedBones.Add(new BoneEntry
                    {
                        Index = myIndex, Name = t.name, ParentIndex = parentIndex,
                        Path = GetRelativePath(t, _targetRoot.transform),
                        Transform = t, Include = false, IsRoot = false
                    });
                }
                foreach (Transform child in t) queue.Enqueue(child);
            }
        }

        // ─── Export ───────────────────────────────────────────────────────────

        private void ExportAsset()
        {
            bool isNew = _existingAsset == null;
            SkeletonAsset asset = isNew
                ? ScriptableObject.CreateInstance<SkeletonAsset>()
                : _existingAsset;

            asset.bones.Clear();

            var included = _detectedBones.Where(b => b.Include).ToList();

            // Переиндексируем: только включённые кости, но сохраняем правильную родительскую иерархию
            var oldIdxToNew = new Dictionary<int, int>();
            for (int ni = 0; ni < included.Count; ni++)
                oldIdxToNew[included[ni].Index] = ni;

            foreach (var entry in included)
            {
                int newParent = entry.ParentIndex >= 0 && oldIdxToNew.TryGetValue(entry.ParentIndex, out int p)
                    ? p : -1;

                // ── ИСПРАВЛЕНИЕ: bind-pose берём непосредственно с Transform в текущий момент.
                //    Персонаж должен стоять в T/A-позе при нажатии Export!
                //    Если персонаж не в bind-pose — предупреждаем.
                asset.bones.Add(new SkeletonAsset.BoneEntry
                {
                    boneIndex          = oldIdxToNew[entry.Index],
                    boneName           = entry.Name,
                    parentIndex        = newParent,
                    transformPath      = entry.Path,  // "" для корня — это правильно
                    bindLocalPosition  = entry.Transform.localPosition,
                    bindLocalRotation  = entry.Transform.localRotation,
                    bindLocalScale     = entry.Transform.localScale
                });
            }

            if (isNew)
            {
                EnsureDirectory(_savePath);
                AssetDatabase.CreateAsset(asset, _savePath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int rootCount = included.Count(b => b.IsRoot);
            Debug.Log($"[SkeletonExporter] Экспортировано {asset.bones.Count} костей " +
                      $"({rootCount} корневых, {asset.bones.Count - rootCount} анимируемых) → {_savePath}");

            EditorUtility.DisplayDialog("Готово",
                $"SkeletonAsset сохранён: {asset.bones.Count} костей.\n\n" +
                "Убедитесь что персонаж был в T/A-позе при экспорте!\n" +
                _savePath, "OK");

            Selection.activeObject = asset;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static string GetRelativePath(Transform t, Transform root)
        {
            if (t == root) return "";
            var parts = new List<string>();
            var cur = t;
            while (cur != null && cur != root)
            {
                parts.Insert(0, cur.name);
                cur = cur.parent;
            }
            return string.Join("/", parts);
        }

        private void SetAllInclusion(bool val)
        {
            for (int i = 0; i < _detectedBones.Count; i++)
            {
                var e = _detectedBones[i]; e.Include = val; _detectedBones[i] = e;
            }
        }

        private void SelectHumanoidBones()
        {
            var humanoid = new HashSet<string> {
                "Hips","Spine","Chest","UpperChest","Neck","Head",
                "LeftShoulder","LeftUpperArm","LeftLowerArm","LeftHand",
                "RightShoulder","RightUpperArm","RightLowerArm","RightHand",
                "LeftUpperLeg","LeftLowerLeg","LeftFoot","LeftToes",
                "RightUpperLeg","RightLowerLeg","RightFoot","RightToes"
            };
            for (int i = 0; i < _detectedBones.Count; i++)
            {
                var e = _detectedBones[i];
                // Корень включаем всегда (он нужен как anchor)
                e.Include = e.IsRoot || humanoid.Contains(e.Name);
                _detectedBones[i] = e;
            }
        }

        private static void EnsureDirectory(string assetPath)
        {
            string dir = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir)) return;
            var parts = dir.Split('/');
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