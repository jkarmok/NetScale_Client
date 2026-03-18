using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AnimationSystem.Serialization;

namespace AnimationSystem.Unity.Editor.Export
{
    /// <summary>
    /// Менеджер экспорта скелета и анимаций.
    /// Использует System.IO для работы с файлами - работает за пределами Unity.
    /// </summary>
    public sealed class AnimationSystemExporter : EditorWindow
    {
        [MenuItem("AnimationSystem/Export/Export to Loader Format", priority = 100)]
        public static void Open() => GetWindow<AnimationSystemExporter>("AS Exporter");

        private SkeletonAsset _skeletonAsset;
        private AnimationDatabaseAsset _databaseAsset;
        private string _exportPath = "Assets/AnimationSystem/Export/Character";
        private string _characterName = "Character";
        private bool _exportSkeleton = true;
        private bool _exportAnimations = true;
        private bool _createMetaFile = true;
        
        private Vector2 _scrollPos;

        private void OnGUI()
        {
            GUILayout.Label("Animation System Exporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Экспортирует скелет и анимации в формат, совместимый с AnimationSystem.Loaders.\n" +
                "Все файлы сохраняются в одну папку для удобного переноса на другую платформу.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            
            // Основные настройки
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
                
                _skeletonAsset = (SkeletonAsset)EditorGUILayout.ObjectField(
                    "Skeleton Asset", _skeletonAsset, typeof(SkeletonAsset), false);
                
                _databaseAsset = (AnimationDatabaseAsset)EditorGUILayout.ObjectField(
                    "Animation Database", _databaseAsset, typeof(AnimationDatabaseAsset), false);
                
                _characterName = EditorGUILayout.TextField("Character Name", _characterName);
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    _exportPath = EditorGUILayout.TextField("Export Path", _exportPath);
                    if (GUILayout.Button("…", GUILayout.Width(24)))
                    {
                        string path = EditorUtility.OpenFolderPanel("Select Export Folder", 
                            Application.dataPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            // Сохраняем как абсолютный путь
                            _exportPath = path;
                        }
                    }
                }
                
                EditorGUILayout.HelpBox(
                    "Путь может быть абсолютным (C:/Exports) или относительным от папки проекта (Assets/Exports)", 
                    MessageType.None);
            }

            EditorGUILayout.Space(6);

            // Опции экспорта
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Export Options", EditorStyles.boldLabel);
                
                _exportSkeleton = EditorGUILayout.Toggle("Export Skeleton", _exportSkeleton);
                _exportAnimations = EditorGUILayout.Toggle("Export Animations", _exportAnimations);
                _createMetaFile = EditorGUILayout.Toggle("Create Meta File", _createMetaFile);
            }

            EditorGUILayout.Space(6);

            // Preview
            if (_skeletonAsset != null || _databaseAsset != null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Export Preview", EditorStyles.boldLabel);
                    
                    using var scroll = new EditorGUILayout.ScrollViewScope(_scrollPos, GUILayout.Height(120));
                    _scrollPos = scroll.scrollPosition;
                    
                    if (_exportSkeleton && _skeletonAsset != null)
                    {
                        EditorGUILayout.LabelField($"• Skeleton: {_skeletonAsset.name} ({_skeletonAsset.bones.Count} bones)");
                    }
                    
                    if (_exportAnimations && _databaseAsset != null)
                    {
                        EditorGUILayout.LabelField($"• Database: {_databaseAsset.name} ({_databaseAsset.Clips.Count} clips)");
                        foreach (var clip in _databaseAsset.Clips.Take(5))
                        {
                            if (clip != null)
                                EditorGUILayout.LabelField($"  - {clip.clipName} (id:{clip.clipId})", EditorStyles.miniLabel);
                        }
                        if (_databaseAsset.Clips.Count > 5)
                            EditorGUILayout.LabelField($"  ... and {_databaseAsset.Clips.Count - 5} more", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.Space(8);

            // Кнопка экспорта
            bool canExport = (_exportSkeleton && _skeletonAsset != null) || 
                           (_exportAnimations && _databaseAsset != null);
            
            using (new EditorGUI.DisabledScope(!canExport))
            {
                if (GUILayout.Button($"▶  Export to {GetDisplayPath()}", GUILayout.Height(36)))
                {
                    Export();
                }
            }
        }

        private string GetDisplayPath()
        {
            if (string.IsNullOrEmpty(_exportPath))
                return "Select path";
            return _exportPath.Length > 50 ? "..." + _exportPath.Substring(_exportPath.Length - 50) : _exportPath;
        }

        private void Export()
        {
            try
            {
                // Получаем абсолютный путь
                string fullPath = ResolveExportPath(_exportPath);
                
                // Создаем директорию экспорта
                Directory.CreateDirectory(fullPath);
                
                var exportedFiles = new List<string>();
                var meta = new ExportMetaData
                {
                    characterName = _characterName,
                    exportDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    skeletonFile = "",
                    animationFiles = new List<string>()
                };

                // Экспорт скелета
                if (_exportSkeleton && _skeletonAsset != null)
                {
                    string skeletonPath = ExportSkeleton(_skeletonAsset, fullPath);
                    exportedFiles.Add(skeletonPath);
                    meta.skeletonFile = Path.GetFileName(skeletonPath);
                    Debug.Log($"[Exporter] Skeleton exported: {skeletonPath}");
                }

                // Экспорт анимаций
                if (_exportAnimations && _databaseAsset != null)
                {
                    var animationPaths = ExportAnimations(_databaseAsset, fullPath, meta);
                    exportedFiles.AddRange(animationPaths);
                }

                // Создание мета-файла
                if (_createMetaFile)
                {
                    string metaPath = Path.Combine(fullPath, $"{_characterName}_meta.json");
                    File.WriteAllText(metaPath, meta.ToJson());
                    exportedFiles.Add(metaPath);
                }

                // Показываем результат
                string message = $"Exported {exportedFiles.Count} files to:\n{fullPath}\n\n";
                message += "Files:\n";
                foreach (var file in exportedFiles.Take(5))
                    message += $"• {Path.GetFileName(file)}\n";
                if (exportedFiles.Count > 5)
                    message += $"• ... and {exportedFiles.Count - 5} more\n";

                EditorUtility.DisplayDialog("Export Complete", message, "OK");
                
                // Открываем папку в проводнике
                System.Diagnostics.Process.Start(fullPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Exporter] Export failed: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Export Failed", e.Message, "OK");
            }
        }

        private string ResolveExportPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                path = "Assets/AnimationSystem/Export";
            
            // Если путь начинается с Assets/ - преобразуем в абсолютный
            if (path.StartsWith("Assets/") || path.StartsWith("Assets\\"))
            {
                path = Path.Combine(Application.dataPath, path.Substring(7));
            }
            
            // Заменяем обратные слеши на прямые
            path = path.Replace('\\', '/');
            
            return path;
        }

        private string ExportSkeleton(SkeletonAsset asset, string exportPath)
        {
            var skeletonDef = asset.BuildDefinition();
            string json = SkeletonLoader.SaveToJson(skeletonDef);
            
            string fileName = $"{_characterName}_skeleton.json";
            string filePath = Path.Combine(exportPath, fileName);
            
            File.WriteAllText(filePath, json);
            return filePath;
        }

        private List<string> ExportAnimations(AnimationDatabaseAsset asset, string exportPath, ExportMetaData meta)
        {
            string animsPath = Path.Combine(exportPath, "Animations");
            Directory.CreateDirectory(animsPath);

            var skeletonDef = _skeletonAsset?.BuildDefinition();
            int boneCount = skeletonDef?.BoneCount ?? 
                           (_skeletonAsset?.bones?.Count ?? 30); // fallback

            var exportedFiles = new List<string>();

            for (int i = 0; i < asset.Clips.Count; i++)
            {
                var clipAsset = asset.Clips[i];
                if (clipAsset == null) continue;

                // Показываем прогресс
                if (i % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar("Exporting Animations", 
                        $"Exporting {clipAsset.clipName}...", 
                        (float)i / asset.Clips.Count);
                }

                try
                {
                    var clip = clipAsset.BuildClip(boneCount);
                    byte[] binary = AnimationClipLoader.SaveToBinary(clip, boneCount);
                    
                    string fileName = $"{clipAsset.clipId:D4}_{SanitizeFileName(clipAsset.clipName)}.anim";
                    string filePath = Path.Combine(animsPath, fileName);
                    
                    File.WriteAllBytes(filePath, binary);
                    meta.animationFiles.Add(fileName);
                    exportedFiles.Add(filePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Exporter] Failed to export clip {clipAsset.clipName}: {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            return exportedFiles;
        }

        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        [System.Serializable]
        private class ExportMetaData
        {
            public string characterName;
            public string exportDate;
            public string skeletonFile;
            public List<string> animationFiles;

            public string ToJson()
            {
                var obj = new
                {
                    characterName,
                    exportDate,
                    skeletonFile,
                    animationCount = animationFiles?.Count ?? 0,
                    animations = animationFiles
                };
                return UnityEngine.JsonUtility.ToJson(obj, true);
            }
        }
    }
}