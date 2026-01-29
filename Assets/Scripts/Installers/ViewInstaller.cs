using System;
using Game.Scene;
using Scene;
using UnityEngine;
using VContainer.Unity;
using View;

namespace Installers
{
    public class ViewInstaller : IStartable, IDisposable
    {
        private BatchSceneApplier _batchSceneApplier;

        public ViewInstaller(BatchSceneApplier batchSceneApplier)
        {
            _batchSceneApplier = batchSceneApplier;
        }
 
        private void OnEntityAdded(NetworkEntity entity, EntityDto entityDto, bool owned)
        {
            var view = entity.GetComponent<IView>();
            view?.SpawnSetup(entityDto.ViewId, entityDto.SpartalOwnerId, entityDto.ProxyId, entityDto.TransformState,
                new Vector3( entityDto.Position.X, entityDto.Position.Y, entityDto.Position.Z),
                new Quaternion(entityDto.Rotation.X, entityDto.Rotation.Y, entityDto.Rotation.Z, entityDto.Rotation.W));
        }
        private void OnEntityUpdated(NetworkEntity entity, EntityDto entityDto, bool owned)
        {
            var view = entity.GetComponent<IView>();
            view?.SpawnSetup(entityDto.ViewId, entityDto.SpartalOwnerId, entityDto.ProxyId, entityDto.TransformState,
                new Vector3( entityDto.Position.X, entityDto.Position.Y, entityDto.Position.Z),
                new Quaternion(entityDto.Rotation.X, entityDto.Rotation.Y, entityDto.Rotation.Z, entityDto.Rotation.W));
            
        }
        
        public void Dispose()
        {
            _batchSceneApplier.OnEntityAdded -= OnEntityAdded;
            _batchSceneApplier.OnEntityUpdated -= OnEntityUpdated;
        }
 

        public void Start()
        {
            _batchSceneApplier.OnEntityAdded += OnEntityAdded;
            _batchSceneApplier.OnEntityUpdated += OnEntityUpdated;
        }
    }
}