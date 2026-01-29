using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Imported.AVIS.RuntimePerformanceMonitor.Providers
{
    [CustomEditor(typeof(WebSocketPerformanceProvider))]
    public class WebSocketPerformanceProviderEditor : Editor
    {
        private SerializedProperty portProperty;
        private SerializedProperty broadcastIntervalProperty;

        private void OnEnable()
        {
            portProperty = serializedObject.FindProperty("port");
            broadcastIntervalProperty = serializedObject.FindProperty("broadcastInterval");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Стандартные свойства
            EditorGUILayout.PropertyField(portProperty);
            EditorGUILayout.PropertyField(broadcastIntervalProperty);

            serializedObject.ApplyModifiedProperties();

            // Кнопка для открытия HTML файла
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Web Interface", EditorStyles.boldLabel);
              
            if (GUILayout.Button("Open Web Dashboard", GUILayout.Height(30)))
            {
                OpenWebDashboard();
            }

            // Информация о сервере
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"WebSocket server running on port {portProperty.intValue}", MessageType.Info);
        }

        private string FindHtmlFile()
        {
            // Ищем все HTML файлы в проекте
            string[] guids = AssetDatabase.FindAssets("t:TextAsset");

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.EndsWith("index-performance.html", System.StringComparison.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
                    return fullPath;
                }
            }

            return string.Empty;
        }

        private void OpenWebDashboard()
        {
            var htmlFile = FindHtmlFile();
            
            if (string.IsNullOrEmpty(htmlFile))
            {
                EditorUtility.DisplayDialog("HTML Files Not Found", 
                    "No HTML files found in the project. Please add your dashboard HTML file.", 
                    "OK");
                return;
            }
 
            OpenFileInBrowser(htmlFile);
        }

        private string GetRelativePath(string fullPath)
        {
            string projectRoot = Directory.GetCurrentDirectory();
            if (fullPath.StartsWith(projectRoot))
            {
                return fullPath.Substring(projectRoot.Length + 1);
            }
            return fullPath;
        }

        private void OpenFileInBrowser(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
                
                UnityEngine.Debug.Log($"Opened web dashboard: {filePath}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", 
                    $"Failed to open HTML file: {e.Message}", 
                    "OK");
            }
        }
    }
}