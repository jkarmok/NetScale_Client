using UnityEditor;
using UnityEngine;

namespace SceneEditor
{
    public static class SceneHotkeys
    {
        [MenuItem("Tools/Create/Team Spawn Point %&t")]
        private static void CreateTeamSpawnHotkey()
        {
            CreatePointWithType<TeamSpawnPoint>();
        }
    
        [MenuItem("Tools/Create/Bot Spawn Point %&b")]
        private static void CreateBotSpawnHotkey()
        {
            CreatePointWithType<BotSpawnPoint>();
        }
    
        [MenuItem("Tools/Create/Resource Point %&r")]
        private static void CreateResourcePointHotkey()
        {
            CreatePointWithType<ResourceSpawnPoint>();
        }
    
        private static void CreatePointWithType<T>() where T : PointBase
        {
            GameObject go = new GameObject(typeof(T).Name);
            go.transform.position = GetSceneViewCenter();
            go.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(go, "Create " + typeof(T).Name);
            Selection.activeGameObject = go;
        }
    
        private static Vector3 GetSceneViewCenter()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                UnityEngine.Camera cam = sceneView.camera;
                if (cam != null)
                {
                    return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                }
            }
            return Vector3.zero;
        }
    }
}