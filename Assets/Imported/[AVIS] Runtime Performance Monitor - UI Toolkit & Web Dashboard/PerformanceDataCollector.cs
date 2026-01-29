using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Imported.AVIS.RuntimePerformanceMonitor
{
    public class PerformanceDataCollector : MonoBehaviour
    {
        [SerializeField] private int historySampleCount = 100;
        [SerializeField] private int statsWindowSize = 100;
    
        private PerformanceData _currentData;
        private HistoryData _historyData;
    
        private ProfilerRecorder _totalMemoryRecorder;
        private ProfilerRecorder _gcMemoryRecorder;
    
        private float _deltaTime;
        private long _lastAllocMemory;
    
        private readonly List<IPerformanceDataListener> _listeners = new List<IPerformanceDataListener>();
    
        private string _deviceInfo;
        private bool _deviceInfoInitialized = false;
        
        // Statistics tracking
        private PerformanceStats _stats;
        private Queue<float> _fpsWindow;
        private float _fpsSum;
        private float _frameTimeSum;
        private float _allocSum;

        public PerformanceData CurrentData => _currentData;
        public HistoryData History => _historyData;
        public string DeviceInfo => _deviceInfo;

        private void Awake()
        {
            InitializeData();
            InitializeProfilers();
            InitializeDeviceInfo();
            InitializeStatistics();
        }

        private void Start()
        {
            NotifyAboutDeviceListeners();
        }

        private void InitializeData()
        {
            _historyData = new HistoryData(historySampleCount);
        
            _currentData = new PerformanceData
            {
                frameTimeHistory = _historyData.frameTimeSamples,
                allocHistory = _historyData.allocSamples,
                historyLength = historySampleCount
            };
        }
        
        private void InitializeStatistics()
        {
            _stats = new PerformanceStats
            {
                fpsMin = float.MaxValue,
                frameTimeMin = float.MaxValue,
                allocMin = float.MaxValue,
                sampleCount = 0
            };
            
            _fpsWindow = new Queue<float>(statsWindowSize);
        }

        private void InitializeProfilers()
        {
            _totalMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            _gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        }

        private void InitializeDeviceInfo()
        {
            if (_deviceInfoInitialized) return;
        
            _deviceInfo = $"Device: {SystemInfo.deviceModel}\n" +
                          $"OS: {SystemInfo.operatingSystem}\n" +
                          $"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)\n" +
                          $"GPU: {SystemInfo.graphicsDeviceName}\n" +
                          $"VRAM: {SystemInfo.graphicsMemorySize} MB\n" +
                          $"RAM: {SystemInfo.systemMemorySize} MB";
        
            _deviceInfoInitialized = true;
        }

        private void Update()
        {
            _deltaTime = Time.unscaledDeltaTime;
        
            CollectFrameData();
            UpdateStatistics();
            UpdateHistory();
        
            NotifyListeners();
        }

        private void CollectFrameData()
        {
            _currentData.fps = 1f / _deltaTime;
            _currentData.frameTime = _deltaTime * 1000f;
            _currentData.timestamp = Time.timeAsDouble;
        
            _currentData.totalMemory = _totalMemoryRecorder.LastValue;
            _currentData.gcMemory = _gcMemoryRecorder.LastValue;
        
            long currentAllocMemory = _gcMemoryRecorder.LastValue;
            long allocDelta = currentAllocMemory - _lastAllocMemory;
            _currentData.allocPerFrameKB = Mathf.Max(0, allocDelta / 1024f);
            _currentData.allocPerFrameMB = _currentData.allocPerFrameKB / 1024f;
            _lastAllocMemory = currentAllocMemory;
        }
        
        private void UpdateStatistics()
        {
            float fps = _currentData.fps;
            float frameTime = _currentData.frameTime;
            float alloc = _currentData.allocPerFrameKB;
            
            // Update min/max
            _stats.fpsMin = Mathf.Min(_stats.fpsMin, fps);
            _stats.fpsMax = Mathf.Max(_stats.fpsMax, fps);
            _stats.frameTimeMin = Mathf.Min(_stats.frameTimeMin, frameTime);
            _stats.frameTimeMax = Mathf.Max(_stats.frameTimeMax, frameTime);
            _stats.allocMin = Mathf.Min(_stats.allocMin, alloc);
            _stats.allocMax = Mathf.Max(_stats.allocMax, alloc);
            
            // Rolling window for averages
            _fpsWindow.Enqueue(fps);
            _fpsSum += fps;
            _frameTimeSum += frameTime;
            _allocSum += alloc;
            _stats.sampleCount++;
            
            if (_fpsWindow.Count > statsWindowSize)
            {
                _fpsSum -= _fpsWindow.Dequeue();
            }
            
            // Calculate averages
            int windowCount = _fpsWindow.Count;
            _stats.fpsAvg = _fpsSum / windowCount;
            _stats.frameTimeAvg = _frameTimeSum / _stats.sampleCount;
            _stats.allocAvg = _allocSum / _stats.sampleCount;
            
            // Calculate 1% Low FPS (worst 1% frame times)
            _stats.fps1PercentLow = Calculate1PercentLow();
            
            // Total session allocations
            _stats.allocTotalSession += _currentData.allocPerFrameMB;
            
            // Copy stats to current data
            _currentData.stats = _stats;
        }
        
        private float Calculate1PercentLow()
        {
            if (_historyData.frameTimeSamples == null || _historyData.sampleCount < 10)
                return 0f;
            
            // Find the worst 1% of frame times (highest values)
            int sampleSize = Mathf.Min(_stats.sampleCount, _historyData.sampleCount);
            int percentileIndex = Mathf.Max(1, sampleSize / 100);
            
            float maxFrameTime = 0f;
            for (int i = 0; i < percentileIndex; i++)
            {
                int idx = (_historyData.currentIndex - i + _historyData.sampleCount) % _historyData.sampleCount;
                maxFrameTime = Mathf.Max(maxFrameTime, _historyData.frameTimeSamples[idx]);
            }
            
            return maxFrameTime > 0 ? 1000f / maxFrameTime : 0f;
        }

        private void UpdateHistory()
        {
            _historyData.currentIndex = (_historyData.currentIndex + 1) % _historyData.sampleCount;
        
            _historyData.frameTimeSamples[_historyData.currentIndex] = _currentData.frameTime;
            _historyData.allocSamples[_historyData.currentIndex] = _currentData.allocPerFrameMB;
        
            _currentData.currentIndex = _historyData.currentIndex;
        }

        private void NotifyListeners()
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnPerformanceDataUpdated(ref _currentData);
            }
        }
        
        private void NotifyAboutDeviceListeners()
        {
            for (int i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].SetupDeviceData(_deviceInfo);
            }
        }

        public void RegisterListener(IPerformanceDataListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void UnregisterListener(IPerformanceDataListener listener)
        {
            _listeners.Remove(listener);
        }

        private void OnDestroy()
        {
            _totalMemoryRecorder.Dispose();
            _gcMemoryRecorder.Dispose();
            _listeners.Clear();
        }
    }

    public interface IPerformanceDataListener
    {
        void OnPerformanceDataUpdated(ref PerformanceData data);
        void SetupDeviceData(string deviceInfo);
    }
}
