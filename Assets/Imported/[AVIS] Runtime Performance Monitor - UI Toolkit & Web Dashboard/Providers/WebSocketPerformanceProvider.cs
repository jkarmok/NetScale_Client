using UnityEngine;

namespace Imported.AVIS.RuntimePerformanceMonitor.Providers
{
    public class WebSocketPerformanceProvider : PerformanceDataProvider
    {
        [SerializeField] private int port = 8080;
        [SerializeField] private float broadcastInterval = 0.1f;

        private WebSocketSharp.Server.WebSocketServer _server;
        private float broadcastTimer;

        // Ссылки на broadcaster'ы
        private PerformanceBroadcaster _broadcaster;
        private DeviceDataBroadcaster _deviceDataBroadcaster;

        protected override void Awake()
        {
            base.Awake();
            StartWebSocketServer();
        }

        private void StartWebSocketServer()
        {
            _server = new WebSocketSharp.Server.WebSocketServer(port);

            _server.AddWebSocketService<PerformanceBroadcaster>("/performance", () =>
            {
                _broadcaster = new PerformanceBroadcaster();
                return _broadcaster;
            });

            _server.AddWebSocketService<DeviceDataBroadcaster>("/device", () =>
            {
                _deviceDataBroadcaster = new DeviceDataBroadcaster(GetDeviceInfo);
                return _deviceDataBroadcaster;
            });

            _server.Start();

            Debug.Log($"Performance WebSocket server started on port {port}");
        }

        public override void OnPerformanceDataUpdated(ref PerformanceData data)
        {
            // Отправляем данные с интервалом чтобы уменьшить нагрузку
            broadcastTimer += Time.deltaTime;
            if (broadcastTimer >= broadcastInterval)
            {
                broadcastTimer = 0f;
                if (_broadcaster != null)
                {
                    _broadcaster.BroadcastData(ref data);
                }
            }
        }

        private string GetDeviceInfo()
        {
            return _deviceName;
        }

        private string _deviceName;

        public override void SetupDeviceData(string deviceInfo)
        {
            _deviceName = deviceInfo;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _server?.Stop();
        }
    }
}