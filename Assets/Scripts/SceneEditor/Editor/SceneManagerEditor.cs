using System.Collections.Generic;
using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace SceneEditor.Editor
{
    public class SceneManagerEditor : EditorWindow
    {
        private static SceneManagerEditor window;
        private string savePath = "Assets/SceneData/";
        private string fileName = "scene_data.json";
        private Vector2 scrollPos;
        private List<PointBase> allPoints = new List<PointBase>();

        [MenuItem("Tools/Scene Manager")]
        public static void ShowWindow()
        {
            window = GetWindow<SceneManagerEditor>();
            window.titleContent = new GUIContent("Scene Manager");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            RefreshPoints();
        }

        private void RefreshPoints()
        {
            allPoints.Clear();
            allPoints.AddRange(FindObjectsOfType<PointBase>());
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Панель управления
            DrawControlPanel();

            EditorGUILayout.Space(10);

            // Список точек
            DrawPointsList();

            // Кнопки создания точек
            DrawCreationButtons();
        }

        private void DrawControlPanel()
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.LabelField("Scene Manager", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField("Save Path", savePath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Folder", savePath, "");
                if (!string.IsNullOrEmpty(path))
                    savePath = path.Replace(Application.dataPath, "Assets/");
            }

            EditorGUILayout.EndHorizontal();

            fileName = EditorGUILayout.TextField("File Name", fileName);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Points", GUILayout.Height(30)))
                RefreshPoints();

            if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
                SaveSceneToJson();

            if (GUILayout.Button("Load from JSON", GUILayout.Height(30)))
                LoadSceneFromJson();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPointsList()
        {
            EditorGUILayout.LabelField($"Points in Scene: {allPoints.Count}",
                EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));

            foreach (var point in allPoints)
            {
                if (point == null) continue;

                EditorGUILayout.BeginVertical("Box");

                EditorGUILayout.BeginHorizontal();
                bool isSelected = Selection.activeGameObject == point.gameObject;
                EditorGUILayout.LabelField(point.PointName,
                    isSelected ? EditorStyles.boldLabel : EditorStyles.label);

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = point.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    if (EditorUtility.DisplayDialog("Delete Point",
                            $"Delete {point.PointName}?", "Yes", "No"))
                    {
                        DestroyImmediate(point.gameObject);
                        RefreshPoints();
                    }
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Type: {point.GetType().Name}");
                EditorGUILayout.LabelField($"Position: {point.transform.position}");

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCreationButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Team Spawn", GUILayout.Height(30)))
                CreatePoint<TeamSpawnPoint>("TeamSpawnPoint");

            if (GUILayout.Button("Bot Spawn", GUILayout.Height(30)))
                CreatePoint<BotSpawnPoint>("BotSpawnPoint");

            if (GUILayout.Button("Resource Spawn", GUILayout.Height(30)))
                CreatePoint<ResourceSpawnPoint>("ResourceSpawnPoint");

            EditorGUILayout.EndHorizontal();
        }

        private void CreatePoint<T>(string defaultName) where T : PointBase
        {
            GameObject go = new GameObject(defaultName);
            go.transform.position = GetSpawnPosition();
            T point = go.AddComponent<T>();
            point.PointName = defaultName;
            Undo.RegisterCreatedObjectUndo(go, "Create " + defaultName);
            RefreshPoints();
            Selection.activeGameObject = go;
        }

        private Vector3 GetSpawnPosition()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                UnityEngine.Camera sceneCam = SceneView.lastActiveSceneView.camera;
                if (sceneCam != null)
                {
                    Vector3 pos = sceneCam.ViewportToWorldPoint(
                        new Vector3(0.5f, 0.5f, 10f));
                    return pos;
                }
            }

            return Vector3.zero;
        }

        private void SaveSceneToJson()
        {
            var sceneData = new
            {
                sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                saveDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                points = new List<object>()
            };

            foreach (var point in allPoints)
            {
                if (point != null)
                    sceneData.points.Add(point.ToJson());
            }

            string json = JsonConvert.SerializeObject(sceneData, Formatting.Indented);

            string fullPath = Path.Combine(savePath, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, json);

            Debug.Log($"Scene saved to: {fullPath}");
            AssetDatabase.Refresh();
        }

        private void LoadSceneFromJson()
        {
            string fullPath = Path.Combine(savePath, fileName);

            if (!File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("Error", "File not found!", "OK");
                return;
            }

            string json = File.ReadAllText(fullPath);
            // Здесь будет десериализация и создание объектов
            Debug.Log("Loading scene from JSON...");
        }
    }
}