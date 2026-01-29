using UnityEngine;

namespace Scene
{
    [System.Serializable]
    public class NetworkInterpolator
    {
        private static class Constants
        {
            // Time constants
            public const float MIN_INTERPOLATION_TIME = 0.06f;
            public const float MAX_INTERPOLATION_TIME = 0.3f;
            public const float MAX_EXTRAPOLATION_TIME = 0.1f;
            public const float BASE_SMOOTH_DAMP_TIME = 0.08f;
            public const float TIME_EPSILON = 0.001f;
            public const float TICK_RATE_BASE = 60f;
            
            // Distance thresholds
            public const float NEAR_DISTANCE_THRESHOLD = 30f;
            public const float MEDIUM_DISTANCE_THRESHOLD = 60f;
            public const float FAR_DISTANCE_THRESHOLD = 100f;
            
            // Interpolation adjustments
            public const float MEDIUM_DISTANCE_FACTOR = 1.1f;
            public const float FAR_DISTANCE_FACTOR = 1.3f;
            
            // Time adjustment factors
            public const float HIGH_LATENCY_FACTOR = 1.2f;
            public const float LOW_LATENCY_FACTOR = 0.8f;
            
            // Snap distance
            public const float TELEPORT_DISTANCE_THRESHOLD = 15f;
            public const float FINAL_SMOOTHING_DISTANCE_THRESHOLD = 10f;
            
            // Smoothing factors
            public const float UPDATE_INTERVAL_SMOOTHING_MULTIPLIER = 0.5f;
        }
        private static class UpdateIntervalPresets
        {
            public static readonly int[] Intervals = { 1, 3, 5, 10 };
            public static readonly int[] EffectiveTickRates = { 60, 20, 12, 6 };
            
            public static readonly float[] InterpolationTimes = 
            { 
                0.08f,    // For interval 1 (60 FPS)
                0.12f,    // For interval 3 (20 FPS)
                0.18f,    // For interval 5 (12 FPS)
                0.25f     // For interval 10 (6 FPS)
            };
            
            public const int DEFAULT_INTERVAL_INDEX = 0;
            public const int HIGH_LATENCY_INTERVAL_INDEX = 3;
            public const int MEDIUM_LATENCY_INTERVAL_INDEX = 2;
            public const int LOW_LATENCY_INTERVAL_INDEX = 1;
        }
        private struct StateSnapshot
        {
            public Vector3 Position { get; private set; }
            public Quaternion Rotation { get; private set; }
            public double ServerTime  { get; private set; }
            public double LocalTime  { get; private set; }
            public Vector3 Velocity  { get; private set; }
            public int UpdateInterval { get; private set; }

            public StateSnapshot(Vector3 pos, Quaternion rot, double serverTime, double localTime, Vector3 vel = default, int interval = 1)
            {
                Position = pos;
                Rotation = rot;
                ServerTime = serverTime;
                LocalTime = localTime;
                Velocity = vel;
                UpdateInterval = interval;
            }

            public static StateSnapshot Interpolate(StateSnapshot a, StateSnapshot b, float t)
            {
                float smoothedT = a.UpdateInterval > UpdateIntervalPresets.Intervals[0] ? 
                    SmoothStep(t) : t;
                
                return new StateSnapshot
                {
                    Position = Vector3.Lerp(a.Position, b.Position, smoothedT),
                    Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, smoothedT),
                    ServerTime = a.ServerTime + (b.ServerTime - a.ServerTime) * t,
                    LocalTime = a.LocalTime + (b.LocalTime - a.LocalTime) * t,
                    Velocity = Vector3.Lerp(a.Velocity, b.Velocity, t),
                    UpdateInterval = Mathf.Max(a.UpdateInterval, b.UpdateInterval)
                };
            }

            private static float SmoothStep(float t)
            {
                // Standard smoothstep function: 3t² - 2t³
                return t * t * (3f - 2f * t);
            }
        }
        
        #region Fields
        
        private Transform _targetTransform;
        private readonly NetworkTime _networkTime;
        private StateSnapshot _current;
        private StateSnapshot _previous;
        private bool _hasValidStates;
        
        private float _currentDistanceToPlayer = 0f;
        private Vector3 _smoothedVelocity;
        
        private int _updatesReceived;
        
        private int _currentUpdateInterval = UpdateIntervalPresets.Intervals[0];
        
        #endregion
        
        public bool UseExtrapolation { get; set; } = true;

        public NetworkInterpolator(Transform transform, NetworkTime networkTime)
        {
            _targetTransform = transform;
            _networkTime = networkTime;
        }
        
 
        
        public void Update()
        {
            if (!_hasValidStates) return;

            double localNow = _networkTime.GetLocalTime();
            
            // If states are invalid or identical, apply current state directly
            if (_previous.ServerTime >= _current.ServerTime)
            {
                ApplyTransform(_current.Position, _current.Rotation);
                return;
            }
            
            double timeSincePrevious = localNow - _previous.LocalTime;
            double totalInterpolationTime = CalculateAdaptiveInterpolationTime();
            
            if (totalInterpolationTime <= 0) 
            {
                ApplyTransform(_current.Position, _current.Rotation);
                return;
            }
            
            float interpolationFactor = Mathf.Clamp01((float)(timeSincePrevious / totalInterpolationTime));
            
            interpolationFactor = ApplyAdaptiveSmoothing(interpolationFactor, _current.UpdateInterval);

            var interpolated = StateSnapshot.Interpolate(_previous, _current, interpolationFactor);
            
            Vector3 finalPosition = ApplyFinalSmoothing(interpolated.Position, _current.UpdateInterval);
            Quaternion finalRotation = interpolated.Rotation;

            ApplyTransform(finalPosition, finalRotation);
        }

        public void OnServerUpdate(Vector3 position, Quaternion rotation, double serverTime, int updateInterval = 1)
        {
            Vector3 velocity = CalculateVelocity(position, serverTime);

            var snapshot = new StateSnapshot(
                position, rotation, serverTime, 
                _networkTime.ServerToLocalTime(serverTime), 
                velocity,
                updateInterval
            );

            _updatesReceived++;

            if (!_hasValidStates)
            {
                InitializeFirstState(snapshot);
                return;
            }
            
            _currentUpdateInterval = updateInterval;
            UpdateStateBuffer(snapshot);
        }
        
        public void Reset(Vector3 position, Quaternion rotation)
        {
            double serverTime = _networkTime.GetLocalTime() + _networkTime.GetPing();
            _current = new StateSnapshot(position, rotation, serverTime, 
                _networkTime.ServerToLocalTime(serverTime), Vector3.zero, 
                UpdateIntervalPresets.Intervals[0]);
            _previous = _current;
            _hasValidStates = true;
            _smoothedVelocity = Vector3.zero;
            _currentUpdateInterval = UpdateIntervalPresets.Intervals[0];
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Reset(position, rotation);
            _targetTransform.position = position;
            _targetTransform.rotation = rotation;
        }

        public void ConfigureForDistance(float distance)
        {
            _currentDistanceToPlayer = distance;
            _currentUpdateInterval = CalculateOptimalUpdateInterval(distance);
        }
        
        public InterpolationStats GetStats()
        {
            double currentLatency = _hasValidStates ? 
                (_networkTime.GetLocalTime() - _current.ServerTime) : 0f;

            return new InterpolationStats
            {
                updatesReceived = _updatesReceived,
                bufferSize = _hasValidStates ? 2 : 0,
                currentLatency = (float)currentLatency,
                currentUpdateInterval = _currentUpdateInterval,
                effectiveTickRate = Mathf.RoundToInt(Constants.TICK_RATE_BASE / _currentUpdateInterval),
                isTimeSynced = _networkTime.IsInitialized
            };
        }
        
        public DebugInfo GetDebugInfo()
        {
            return new DebugInfo
            {
                serverPosition = _current.Position,
                clientPosition = _targetTransform.position,
                latency = _networkTime.GetLocalTime() - _current.ServerTime,
                bufferSize = _hasValidStates ? 2 : 0,
                isExtrapolating = IsCurrentlyExtrapolating(),
                updateInterval = _currentUpdateInterval,
                effectiveTickRate = Mathf.RoundToInt(Constants.TICK_RATE_BASE / _currentUpdateInterval)
            };
        }
  
        private void InitializeFirstState(StateSnapshot snapshot)
        {
            _current = snapshot;
            _previous = snapshot;
            ApplyTransform(snapshot.Position, snapshot.Rotation);
            _hasValidStates = true;
            _currentUpdateInterval = snapshot.UpdateInterval;
        }
        
        private void UpdateStateBuffer(StateSnapshot snapshot)
        {
            if (snapshot.ServerTime > _current.ServerTime)
            {
                _previous = _current;
                _current = snapshot;
            }
            else if (snapshot.ServerTime > _previous.ServerTime && snapshot.ServerTime < _current.ServerTime)
            {
                _previous = snapshot;
            }
        }
        
        private Vector3 CalculateVelocity(Vector3 newPosition, double newServerTime)
        {
            if (!_hasValidStates) return Vector3.zero;

            double timeDiff = newServerTime - _current.ServerTime;
            if (timeDiff > Constants.TIME_EPSILON)
            {
                Vector3 displacement = newPosition - _current.Position;
                return displacement / (float)timeDiff;
            }
            
            return Vector3.zero;
        }

        private double CalculateAdaptiveInterpolationTime()
        {
            float baseTime = GetInterpolationTimeForInterval(_currentUpdateInterval);
            baseTime = AdjustInterpolationTimeByDistance(baseTime, _currentDistanceToPlayer);
            baseTime = AdjustInterpolationTimeForLatency(baseTime);
            
            return Mathf.Clamp(baseTime, Constants.MIN_INTERPOLATION_TIME, Constants.MAX_INTERPOLATION_TIME);
        }
        
        private float AdjustInterpolationTimeForLatency(float baseTime)
        {
            double snapshotDelta = _current.ServerTime - _previous.ServerTime;
            double expectedDelta = _currentUpdateInterval / Constants.TICK_RATE_BASE;
            double latencyRatio = snapshotDelta / expectedDelta;
            
            const float HIGH_LATENCY_THRESHOLD = 1.5f;
            const float LOW_LATENCY_THRESHOLD = 0.5f;
            
            if (latencyRatio > HIGH_LATENCY_THRESHOLD)
            {
                return Mathf.Min(baseTime * Constants.HIGH_LATENCY_FACTOR, Constants.MAX_INTERPOLATION_TIME);
            }
            
            if (latencyRatio < LOW_LATENCY_THRESHOLD)
            {
                return Mathf.Max(baseTime * Constants.LOW_LATENCY_FACTOR, Constants.MIN_INTERPOLATION_TIME);
            }
            
            return baseTime;
        }

        private float GetInterpolationTimeForInterval(int interval)
        {
            int index = System.Array.IndexOf(UpdateIntervalPresets.Intervals, interval);
            
            if (index >= 0 && index < UpdateIntervalPresets.InterpolationTimes.Length)
            {
                return UpdateIntervalPresets.InterpolationTimes[index];
            }
            
            // Fallback calculation for unknown intervals
            float intervalBasedTime = interval / Constants.TICK_RATE_BASE * 1.5f;
            return Mathf.Clamp(intervalBasedTime, Constants.MIN_INTERPOLATION_TIME, Constants.MAX_INTERPOLATION_TIME);
        }

        private float AdjustInterpolationTimeByDistance(float baseTime, float distance)
        {
            if (distance > Constants.MEDIUM_DISTANCE_THRESHOLD)
            {
                return distance > Constants.FAR_DISTANCE_THRESHOLD 
                    ? baseTime * Constants.FAR_DISTANCE_FACTOR 
                    : baseTime * Constants.MEDIUM_DISTANCE_FACTOR;
            }
            
            return baseTime;
        }
        
        private int CalculateOptimalUpdateInterval(float distance)
        {
            if (distance >= Constants.FAR_DISTANCE_THRESHOLD)
                return UpdateIntervalPresets.Intervals[UpdateIntervalPresets.HIGH_LATENCY_INTERVAL_INDEX];
            
            if (distance >= Constants.MEDIUM_DISTANCE_THRESHOLD)
                return UpdateIntervalPresets.Intervals[UpdateIntervalPresets.MEDIUM_LATENCY_INTERVAL_INDEX];
            
            if (distance >= Constants.NEAR_DISTANCE_THRESHOLD)
                return UpdateIntervalPresets.Intervals[UpdateIntervalPresets.LOW_LATENCY_INTERVAL_INDEX];
            
            return UpdateIntervalPresets.Intervals[UpdateIntervalPresets.DEFAULT_INTERVAL_INDEX];
        }

        private float ApplyAdaptiveSmoothing(float t, int updateInterval)
        {
            const float EARLY_PHASE_SMOOTH_FACTOR = 0.7f;
            const float LATE_PHASE_SMOOTH_FACTOR = 0.3f;
            const float LATE_PHASE_OFFSET = 0.9f;
            
            const float MEDIUM_EARLY_SMOOTH_FACTOR = 0.5f;
            const float MEDIUM_LATE_SMOOTH_FACTOR = 0.5f;
            const float MEDIUM_LATE_OFFSET = 0.8f;
            
            switch (updateInterval)
            {
                case 1:
                    return t < 0.1f ? t * EARLY_PHASE_SMOOTH_FACTOR : 
                           t > LATE_PHASE_OFFSET ? LATE_PHASE_OFFSET + (t - LATE_PHASE_OFFSET) * LATE_PHASE_SMOOTH_FACTOR : t;
                    
                case 3:
                    return t < 0.2f ? t * MEDIUM_EARLY_SMOOTH_FACTOR : 
                           t > MEDIUM_LATE_OFFSET ? MEDIUM_LATE_OFFSET + (t - MEDIUM_LATE_OFFSET) * MEDIUM_LATE_SMOOTH_FACTOR : t;
                    
                case 5:
                    return Mathf.SmoothStep(0, 1, t);
                    
                case 10:
                    return Mathf.SmoothStep(0, 1, Mathf.SmoothStep(0, 1, t));
                    
                default:
                    return t;
            }
        }

        private Vector3 ApplyFinalSmoothing(Vector3 targetPosition, int updateInterval)
        {
            if (Vector3.Distance(targetPosition, _targetTransform.position) < Constants.FINAL_SMOOTHING_DISTANCE_THRESHOLD)
            {
                float smoothingTime = Constants.BASE_SMOOTH_DAMP_TIME * 
                    (updateInterval * Constants.UPDATE_INTERVAL_SMOOTHING_MULTIPLIER);
                
                return Vector3.SmoothDamp(
                    _targetTransform.position, 
                    targetPosition, 
                    ref _smoothedVelocity, 
                    smoothingTime
                );
            }
            
            return targetPosition;
        }

        private void ApplyTransform(Vector3 position, Quaternion rotation)
        {
            if (Vector3.Distance(_targetTransform.position, position) > Constants.TELEPORT_DISTANCE_THRESHOLD)
            {
                _targetTransform.position = position;
                _targetTransform.rotation = rotation;
                _smoothedVelocity = Vector3.zero;
            }
            else
            {
                _targetTransform.position = position;
                _targetTransform.rotation = rotation;
            }
        }

        private bool IsCurrentlyExtrapolating()
        {
            if (!_hasValidStates) return false;
            double localNow = _networkTime.GetLocalTime();
            return localNow > _current.LocalTime && UseExtrapolation;
        }

        [System.Serializable]
        public struct InterpolationStats
        {
            public int updatesReceived;
            public int bufferSize;
            public float currentLatency;
            public int currentUpdateInterval;
            public int effectiveTickRate;
            public bool isTimeSynced;
        }
        
        public struct DebugInfo
        {
            public Vector3 serverPosition;
            public Vector3 clientPosition;
            public double latency;
            public int bufferSize;
            public bool isExtrapolating;
            public int updateInterval;
            public int effectiveTickRate;
        }
    }
}