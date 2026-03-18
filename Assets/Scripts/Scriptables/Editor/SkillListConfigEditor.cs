using UnityEditor;
using UnityEngine;
using System.IO;
using Scriptables.DTO;

namespace Scriptables.Editor
{
    [CustomEditor(typeof(SkillListConfig))]
    public class SkillListConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SkillListConfig skillList = (SkillListConfig)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

            if (GUILayout.Button("Export to JSON", GUILayout.Height(30)))
            {
                ExportToJson(skillList);
            }

            if (GUILayout.Button("Preview JSON", GUILayout.Height(30)))
            {
                PreviewJson(skillList);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Total Skills: {skillList.Count}", EditorStyles.boldLabel);
        }

        private void ExportToJson(SkillListConfig skillList)
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Skill List to JSON",
                Application.dataPath,
                $"{skillList.name}.json",
                "json");

            if (!string.IsNullOrEmpty(path))
            {
                var dto = SkillDTOMapper.ToDTO(skillList);
                string json = JsonUtility.ToJson(dto, true);
                File.WriteAllText(path, json);
                
                Debug.Log($"Skills exported to: {path}");
                EditorUtility.RevealInFinder(path);
            }
        }

        private void PreviewJson(SkillListConfig skillList)
        {
            var dto = SkillDTOMapper.ToDTO(skillList);
            string json = JsonUtility.ToJson(dto, true);
            
            // Показываем в отдельном окне
            EditorUtility.DisplayDialog(
                "JSON Preview", 
                json.Length > 1000 ? json.Substring(0, 1000) + "..." : json, 
                "OK"
            );
        }
    }
}