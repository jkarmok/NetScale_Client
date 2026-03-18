using UnityEditor;
using UnityEngine;

namespace AnimationSystem.Unity.Editor.Export
{
    /// <summary>
    /// Помощник для быстрой настройки загрузчика анимаций.
    /// </summary>
    public static class AnimationLoaderSetup
    {
        [MenuItem("GameObject/AnimationSystem/Add Loader from Export", false, 10)]
        private static void AddLoaderFromExport(MenuCommand menuCommand)
        {
            // Создаем GameObject
            GameObject go = new GameObject("AnimationLoader");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            
            // Добавляем компоненты
            var loader = go.AddComponent<Loaders.AnimationSystemLoader>();
            var animator = go.AddComponent<AnimatorComponent>();
            
            // Связываем
            var serializedLoader = new SerializedObject(loader);
            serializedLoader.FindProperty("_animator").objectReferenceValue = animator;
            serializedLoader.ApplyModifiedProperties();

            // Регистрируем для undo
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        [MenuItem("Assets/AnimationSystem/Setup Loader from Export", false, 200)]
        private static void SetupLoaderFromSelectedFolder()
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a folder containing exported files", "OK");
                return;
            }

            // Создаем загрузчик
            GameObject go = new GameObject("AnimationLoader");
            var loader = go.AddComponent<Loaders.AnimationSystemLoader>();
            var animator = go.AddComponent<AnimatorComponent>();

            // Ищем файлы
            string[] jsonFiles = System.IO.Directory.GetFiles(folderPath, "*_skeleton.json");
            if (jsonFiles.Length > 0)
            {
                string jsonPath = jsonFiles[0];
                var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    jsonPath.Replace(Application.dataPath, "Assets"));
                
                if (jsonAsset != null)
                {
                    var serializedLoader = new SerializedObject(loader);
                    serializedLoader.FindProperty("_skeletonJson").objectReferenceValue = jsonAsset;
                    
                    // Ищем анимации
                    string animsFolder = System.IO.Path.Combine(folderPath, "Animations");
                    string assetsPath = animsFolder.Replace(Application.dataPath, "Assets");
                    
                    if (System.IO.Directory.Exists(animsFolder))
                    {
                        var animFiles = System.IO.Directory.GetFiles(animsFolder, "*.anim");
                        var clipAssets = new System.Collections.Generic.List<TextAsset>();
                        
                        foreach (var animFile in animFiles)
                        {
                            string assetPath = animFile.Replace(Application.dataPath, "Assets");
                            var clip = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                            if (clip != null)
                                clipAssets.Add(clip);
                        }
                        
                        serializedLoader.FindProperty("_animationClips").arraySize = clipAssets.Count;
                        for (int i = 0; i < clipAssets.Count; i++)
                        {
                            serializedLoader.FindProperty("_animationClips")
                                .GetArrayElementAtIndex(i).objectReferenceValue = clipAssets[i];
                        }
                    }
                    
                    serializedLoader.ApplyModifiedProperties();
                }
            }

            Undo.RegisterCreatedObjectUndo(go, "Setup Animation Loader");
            Selection.activeObject = go;
        }
    }
}