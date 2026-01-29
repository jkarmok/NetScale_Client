namespace Imported.AVIS.RuntimePerformanceMonitor
{
    public struct PerformanceData
    {
        public float fps;
        public float frameTime;
        public float allocPerFrameKB;
        public float allocPerFrameMB;
        public long totalMemory;
        public long gcMemory;
        public double timestamp;
     
        public float[] frameTimeHistory;
        public float[] allocHistory;
        public int currentIndex;
        public int historyLength;
        
        // Extended statistics
        public PerformanceStats stats;
    }
    
    public struct PerformanceStats
    {
        // FPS Stats
        public float fpsMin;
        public float fpsMax;
        public float fpsAvg;
        public float fps1PercentLow;
        
        // Frame Time Stats
        public float frameTimeMin;
        public float frameTimeMax;
        public float frameTimeAvg;
        
        // Allocation Stats
        public float allocMin;
        public float allocMax;
        public float allocAvg;
        public float allocTotalSession;
        
        public int sampleCount;
    }

    public class HistoryData
    {
        public float[] frameTimeSamples;
        public float[] allocSamples;
        public int currentIndex;
        public int sampleCount;
    
        public HistoryData(int sampleCount)
        {
            this.sampleCount = sampleCount;
            frameTimeSamples = new float[sampleCount];
            allocSamples = new float[sampleCount];
            currentIndex = 0;
        }
    }
}