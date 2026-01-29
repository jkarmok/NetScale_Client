using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Imported.AVIS.RuntimePerformanceMonitor
{
    public class DeviceDataBroadcaster : WebSocketBehavior
    {
        private readonly Func<string> _deviceDataCallback;

        private static readonly List<DeviceDataBroadcaster> _clients = new List<DeviceDataBroadcaster>();

        public DeviceDataBroadcaster(Func<string> deviceDataCallback)
        {
            _deviceDataCallback = deviceDataCallback;
        }
        
        protected override void OnOpen()
        {
            Debug.Log($"Connection oppened for device data broadcaster");
            _clients.Add(this);
            BroadcastDeviceData(_deviceDataCallback.Invoke());
        }

        protected override void OnClose(CloseEventArgs e)
        {
            _clients.Remove(this);
        }
 
        protected void BroadcastDeviceData(in string deviceData)
        {
            if (_clients.Count == 0) return;
 
            for (int i = _clients.Count - 1; i >= 0; i--)
            {
                var client = _clients[i];
                if (client.State == WebSocketState.Open)
                {
                    try
                    {
                        client.Send(deviceData);
                    }
                    catch (System.Exception)
                    {
                        // Удаляем отключенных клиентов
                        _clients.RemoveAt(i);
                        Debug.Log($"Connection closed for device data broadcaster: {deviceData}");
                    }
                }
                else
                {
                    _clients.RemoveAt(i);
                }
            }
        }
    }
}