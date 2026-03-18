using System.Collections.Generic;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "NewSkillList", menuName = "Skills/SkillList")]
    public class SkillListConfig : ScriptableObject
    {
        public string ListId;
        public string ListName;
        
        [SerializeField]
        private List<SkillConfig> skills = new List<SkillConfig>();

        public SkillConfig GetSkillById(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;
                
            return skills.Find(skill => skill != null && skill.SkillId == skillId);
        }

        public SkillConfig GetSkillByIndex(int index)
        {
            if (index >= 0 && index < skills.Count)
                return skills[index];
                
            return null;
        }
        public IReadOnlyList<SkillConfig> GetSkills()=> skills;
        public int Count => skills.Count;
    }
}