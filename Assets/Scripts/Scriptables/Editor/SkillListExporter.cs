using UnityEngine;
using UnityEditor;
using System.IO;
using Game.Skills;
using Scriptables.DTO;

namespace Scriptables.Editor
{
    public static class SkillListExporter
    {
        [MenuItem("Assets/Export Skill List to JSON", true)]
        private static bool ExportSkillListValidation()
        {
            return Selection.activeObject != null && 
                   Selection.activeObject is SkillListConfig;
        }

        [MenuItem("Assets/Export Skill List to JSON")]
        private static void ExportSkillList()
        {
            SkillListConfig selectedList = Selection.activeObject as SkillListConfig;
            
            if (selectedList == null)
                return;

            // Конвертируем в DTO через маппер
            SkillListDTO dto = SkillDTOMapper.ToDTO(selectedList);

            string path = EditorUtility.SaveFilePanel(
                "Export Skill List to JSON",
                Application.dataPath,
                $"{selectedList.name}.json",
                "json");

            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(dto, true);
                File.WriteAllText(path, json);
                Debug.Log($"Skills exported to: {path}");
                EditorUtility.RevealInFinder(path);
            }
        }
    }
}