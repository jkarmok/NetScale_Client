using System.Collections.Generic;
using UnityEngine;

namespace Scene
{
    [CreateAssetMenu(fileName = "EntityPrefabConfig", menuName = "Game/Entity Prefab Config")]
    public class EntityPrefabConfig : ScriptableObject
    {
        [System.Serializable]
        public class PrefabMapping
        {
            public byte TypeId;
            public NetworkEntity Prefab;
        }

        public PrefabMapping[] Prefabs;
        
        private Dictionary<byte, NetworkEntity> _prefabLookup;
        
        public void Initialize()
        {
            _prefabLookup = new Dictionary<byte, NetworkEntity>(Prefabs.Length);
            foreach (var mapping in Prefabs)
            {
                if (mapping.Prefab != null)
                    _prefabLookup[mapping.TypeId] = mapping.Prefab;
            }
        }
        
        public bool TryGetPrefab(byte typeId, out NetworkEntity prefab)
        {
            return _prefabLookup.TryGetValue(typeId, out prefab);
        }
    }
}