using UnityEngine;

namespace Imported.AVIS.RuntimePerformanceMonitor
{
    public abstract class PerformanceDataProvider : MonoBehaviour, IPerformanceDataListener
    {
        [SerializeField] protected PerformanceDataCollector dataCollector;
    
        protected virtual void Awake()
        {
            if (dataCollector == null)
                dataCollector = FindObjectOfType<PerformanceDataCollector>();
        
            if (dataCollector != null)
            {
                dataCollector.RegisterListener(this);
            }
        }
 

        public abstract void OnPerformanceDataUpdated(ref PerformanceData data);
        public abstract void SetupDeviceData(string deviceInfo);

        protected virtual void OnDestroy()
        {
            if (dataCollector != null)
            {
                dataCollector.UnregisterListener(this);
            }
        }
    }
}