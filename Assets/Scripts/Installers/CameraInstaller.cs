using System;
using Camera;
using Game.Scene;
using Scene;
using UnityEngine;
using VContainer.Unity;

namespace Installers
{
    public class CameraInstaller : IStartable, IDisposable
    {
        private readonly CameraView _cameraView;
        private readonly BatchSceneApplier _batchSceneApplier;

        public CameraInstaller(CameraView cameraView, BatchSceneApplier batchSceneApplier)
        {
            _cameraView = cameraView;
            _batchSceneApplier = batchSceneApplier;
        }
        
        private void EntityRemoved(NetworkEntity view)
        {
            var cameraTarget = view.GetComponent<ICameraTarget>();
            
            if (cameraTarget == null)
                return;
            
            if (false == cameraTarget.Owned)
                return;

            if (_cameraView.IsTarget(cameraTarget))
            {
                _cameraView.UnSetTarget();
            }
        }

        private void OnEntityAdded(NetworkEntity view, EntityDto entityDto, bool owned)
        {
            var cameraTarget = view.GetComponent<ICameraTarget>();
            
            if (cameraTarget == null)
                return;
            
            if (false == owned)
                return;
            
            cameraTarget.ChangeOwned();
            _cameraView.SetTarget(cameraTarget);

        }

        public void Dispose()
        {
            _batchSceneApplier.OnEntityAdded -= OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved -= EntityRemoved;
        }

        public void Start()
        {
            Application.targetFrameRate = 700;
            QualitySettings.vSyncCount = 0;   // Выключаем VSync
            Application.runInBackground = true;
            
            _batchSceneApplier.OnEntityAdded += OnEntityAdded;
            _batchSceneApplier.OnEntityRemoved += EntityRemoved;
        }
    }
}