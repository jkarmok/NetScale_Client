using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Scene
{
    public class EntityPool
    {
        private readonly IObjectResolver _resolver;
        private readonly Dictionary<int, Stack<NetworkEntity>> _pools = new();
        private readonly Dictionary<int, NetworkEntity> _activeEntities = new();
        private readonly EntityPrefabConfig _prefabConfig;
        private readonly Transform _poolParent;

        public EntityPool(EntityPrefabConfig prefabConfig, Transform poolParent, IObjectResolver resolver)
        {
            _prefabConfig = prefabConfig;
            _poolParent = poolParent;
            _resolver = resolver;
            _prefabConfig.Initialize();
        }

        public NetworkEntity GetEntity(byte typeId, int instanceHash)
        {
            if (_activeEntities.TryGetValue(instanceHash, out var existingEntity))
                return existingEntity;

            if (!_pools.TryGetValue(typeId, out var pool))
            {
                pool = new Stack<NetworkEntity>();
                _pools[typeId] = pool;
            }

            NetworkEntity entity;
            if (pool.Count > 0)
            {
                entity = pool.Pop();
                entity.gameObject.SetActive(true);
            }
            else
            {
                if (!_prefabConfig.TryGetPrefab(typeId, out var prefab))
                {
                    Debug.LogError($"Prefab not found for type: {typeId}");
                    return null;
                }
                var instance = _resolver.Instantiate(prefab.gameObject, _poolParent);
                entity = instance.GetComponent<NetworkEntity>();
            }

            _activeEntities[instanceHash] = entity;
            return entity;
        }

        public void ReturnEntity(int instanceHash)
        {
            if (_activeEntities.TryGetValue(instanceHash, out var entityComponent))
            {
                if (entityComponent == null)
                    return;

                entityComponent.gameObject.SetActive(false);
                entityComponent.transform.SetParent(_poolParent);
 
                if (entityComponent != null)
                {
                    var typeId = entityComponent.TypeId;
                    if (!_pools.TryGetValue(typeId, out var pool))
                    {
                        pool = new Stack<NetworkEntity>();
                        _pools[typeId] = pool;
                    }

                    pool.Push(entityComponent);
                }

                _activeEntities.Remove(instanceHash);
            }
        }

        public bool TryGetActiveEntity(int instanceHash, out NetworkEntity entity)
        {
            return _activeEntities.TryGetValue(instanceHash, out entity);
        }
    }
}