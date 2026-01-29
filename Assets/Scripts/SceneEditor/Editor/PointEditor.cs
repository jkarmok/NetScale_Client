using UnityEditor;
using UnityEngine;

namespace SceneEditor.Editor
{
    [CustomEditor(typeof(PointBase), true)]
    public class PointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            PointBase point = (PointBase)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Point Configuration",
                EditorStyles.boldLabel);

            // Общие настройки
            EditorGUILayout.BeginVertical("Box");

            point.PointName = EditorGUILayout.TextField("Point Name", point.PointName);

            EditorGUILayout.LabelField("Point ID: " + point.PointId);

            EditorGUILayout.BeginHorizontal();
            point.GizmoColor = EditorGUILayout.ColorField("Gizmo Color", point.GizmoColor);
            point.GizmoRadius = EditorGUILayout.FloatField("Radius", point.GizmoRadius);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Оригинальный инспектор
            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Кнопки действий
            if (GUILayout.Button("Update Gizmo", GUILayout.Height(30)))
            {
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Copy Position", GUILayout.Height(25)))
            {
                GUIUtility.systemCopyBuffer = JsonUtility.ToJson(point.transform.position);
                Debug.Log("Position copied to clipboard");
            }
        }
    }
}