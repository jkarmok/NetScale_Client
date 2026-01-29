using System;
using UnityEngine;

namespace Scene
{
    public class NetworkTime
    {
        private double _serverTimeOffset;
        private bool _isInitialized;
        private double _lastSyncTime;
        private float _ping;

        public void SyncWithServer(double serverTime)
        {
            _serverTimeOffset = serverTime - Time.realtimeSinceStartupAsDouble;
            _isInitialized = true;
            _lastSyncTime = GetLocalTime();
        
            Debug.Log($"Time synced. Server: {serverTime}, Local offset: {_serverTimeOffset}");
        }

        public double ServerToLocalTime(double serverTime)
        {
            if (!_isInitialized)
                return 0;
            
            return serverTime - _serverTimeOffset;
        }
        
        public double GetLocalTime()
        {
            if (!_isInitialized)
                return 0;
            
            return Time.realtimeSinceStartupAsDouble + _serverTimeOffset;
        }
 
        public float GetPing() => _ping;
        public bool IsInitialized => _isInitialized;
    }
}