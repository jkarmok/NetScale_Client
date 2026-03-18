using System;
using System.Collections.Generic;
using Game.Scene;
using UnityEngine;
using VContainer;

namespace Scene
{
    public class BatchSceneApplier : IDisposable
    {
        public event Action<NetworkEntity, EntityDto, bool> OnEntityAdded;
        public event Action<NetworkEntity, EntityDto, bool> OnEntityUpdated;
        public event Action<NetworkEntity, bool> OnEntityRemoved;
 
        private readonly EntityPool _entityPool;
        private readonly Dictionary<ushort, NetworkEntity> _networkEntities = new();

        private int _totalSpawns;
        private int _totalDespawns;

        public BatchSceneApplier(EntityPrefabConfig config, IObjectResolver resolver)
        {
            var poolParent = new GameObject("EntityPool").transform;
            _entityPool = new EntityPool(config, poolParent, resolver);
        }
        
        public void ApplyLifecycle(in EntityLifecycleBatchDto lifecycleBatch)
        {
            if (lifecycleBatch.ExitedIds.Count > 0)
            {
                foreach (var id in lifecycleBatch.ExitedIds)
                {
                    DespawnEntity(id);
                }
            }

            if (lifecycleBatch.Entered.Count > 0)
            {
                for (ushort i = 0; i < lifecycleBatch.Entered.Count; i++)
                {
                    ref var entityDto  = ref lifecycleBatch.Entered.Array[i];
                    SpawnEntity(ref entityDto, entityDto.Id == lifecycleBatch.SelfId);
                }
            }
            if (lifecycleBatch.Updated.Count > 0)
            {
                for (ushort i = 0; i < lifecycleBatch.Updated.Count; i++)
                {
                    ref var entityDto  = ref lifecycleBatch.Updated.Array[i];
                    UpdateEntity(ref entityDto, entityDto.Id == lifecycleBatch.SelfId);
                }
            }
        }
        
        public void ApplyTransforms(in TransformBatchStateDto transformBatch)
        {
            if (transformBatch.Transfroms.Count > 0)
            {
                for (ushort i = 0; i < transformBatch.Transfroms.Count; i++)
                {  
                    ref var transformDto  = ref transformBatch.Transfroms.Array[i];
                    UpdateEntityTransform(ref transformDto, in transformBatch.ServerTime);
                }
            }
        }
  
        private void SpawnEntity(ref EntityDto entityDto, bool owned)
        {
            var typeId = GetTypeIdFromEntity(ref entityDto);

            var networkEntity = _entityPool.GetEntity(typeId, entityDto.Id);
            TryGetParent(entityDto, out var parent);

            networkEntity
                .SpawnSetup(entityDto.Id, typeId, entityDto.SpartalOwnerId, entityDto.ProxyId, entityDto.TransformState,
                    new Vector3(entityDto.Position.X, entityDto.Position.Y, entityDto.Position.Z),
                    new Quaternion( entityDto.Rotation.X,  entityDto.Rotation.Y, entityDto.Rotation.Z, entityDto.Rotation.W), owned, parent);
         
            _networkEntities[entityDto.Id] = networkEntity;
            _totalSpawns++;
            OnEntityAdded?.Invoke(networkEntity, entityDto, owned);
        }

        private bool TryGetParent(EntityDto entityDto, out Transform parent)
        {
            if (entityDto.TransformState == TransformState.AttachedToParent)
            {
                if (_networkEntities.TryGetValue(entityDto.AttachedTo, out var networkEntity))
                {
                    parent = networkEntity.transform;
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Parent {entityDto.AttachedTo} not found");
                }
            }

            parent = null;
            return false;
        }

        private void UpdateEntity(ref EntityDto entityDto, bool owned)
        {
            if (_networkEntities.TryGetValue(entityDto.Id, out var networkEntity))
            {  
                OnEntityUpdated?.Invoke(networkEntity, entityDto, owned);
            }
        }

        private void DespawnEntity(ushort instanceHash)
        {
            if (_networkEntities.Remove(instanceHash, out var networkEntity))
            {
                _totalDespawns++;
                OnEntityRemoved?.Invoke(networkEntity, networkEntity.IsOwned);
                _entityPool.ReturnEntity(instanceHash);
            }
        }

        private void UpdateEntityTransform(ref TransformStateDto transformDto, in double serverTime)
        {
            if (_networkEntities.TryGetValue(transformDto.Id, out var networkEntity))
            {
                var rotation = new Quaternion(transformDto.Rotation.X, transformDto.Rotation.Y, transformDto.Rotation.Z,  transformDto.Rotation.W);
                networkEntity
                    .SetTargetTransform(
                        new Vector3(transformDto.Position.X, transformDto.Position.Y, transformDto.Position.Z),
                        rotation, 
                        serverTime);
            }
        }
 
        private byte GetTypeIdFromEntity(ref EntityDto entityDto)
        {
            return entityDto.ViewId;
        }

        public void GetStats(out int activeEntities, out int totalSpawns, out int totalDespawns)
        {
            activeEntities = _networkEntities.Count;
            totalSpawns = _totalSpawns;
            totalDespawns = _totalDespawns;
        }
 
        public void Dispose()
        {

        }
    }
}