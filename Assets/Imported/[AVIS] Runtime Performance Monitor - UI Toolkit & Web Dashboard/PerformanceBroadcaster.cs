using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Imported.AVIS.RuntimePerformanceMonitor
{
    public class PerformanceBroadcaster : WebSocketBehavior
    {
        private static readonly List<PerformanceBroadcaster> _clients = new List<PerformanceBroadcaster>();
    
        // Пул JSON builder для избежания аллокаций
        private readonly System.Text.StringBuilder _jsonBuilder = new System.Text.StringBuilder(512);
    
        protected override void OnOpen()
        {
            _clients.Add(this);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _clients.Remove(this);
        }

        public void BroadcastData(ref PerformanceData data)
        {
            if (_clients.Count == 0) return;
        
            var json = BuildJson(ref data);
        
            for (int i = _clients.Count - 1; i >= 0; i--)
            {
                var client = _clients[i];
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        client.Send(json);
                    }
                    catch (System.Exception)
                    {
                        // Удаляем отключенных клиентов
                        _clients.RemoveAt(i);
                    }
                }
                else
                {
                    _clients.RemoveAt(i);
                }
            }
        }

        private string BuildJson(ref PerformanceData data)
        {
            _jsonBuilder.Clear();
        
            _jsonBuilder.Append('{');
            _jsonBuilder.Append("\"fps\":").Append(data.fps.ToString("F1")).Append(',');
            _jsonBuilder.Append("\"frameTime\":").Append(data.frameTime.ToString("F2")).Append(',');
            _jsonBuilder.Append("\"allocPerFrameKB\":").Append(data.allocPerFrameKB.ToString("F2")).Append(',');
            _jsonBuilder.Append("\"allocPerFrameMB\":").Append(data.allocPerFrameMB.ToString("F4")).Append(',');
            _jsonBuilder.Append("\"totalMemory\":").Append(data.totalMemory).Append(',');
            _jsonBuilder.Append("\"gcMemory\":").Append(data.gcMemory).Append(',');
            _jsonBuilder.Append("\"timestamp\":").Append(data.timestamp.ToString("F3"));
            _jsonBuilder.Append('}');
        
            return _jsonBuilder.ToString();
        }
    }
}