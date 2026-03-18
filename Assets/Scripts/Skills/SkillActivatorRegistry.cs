using System.Collections.Generic;

namespace Skills
{
    public class SkillActivatorRegistry
    {
        private readonly Dictionary<string, Activator> _activators = new();
        
        public void Register(string skillId, Activator activator)
        {
            _activators[skillId] = activator;
        }
        
        public Activator Get(string skillId)
        {
            return _activators.TryGetValue(skillId, out var activator) ? activator : null;
        }
    }
}