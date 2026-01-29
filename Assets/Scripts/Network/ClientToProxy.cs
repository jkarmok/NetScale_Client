using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLibAPI;
using Microsoft.Extensions.Logging;
using NetScaleCommon;
using NetScaleCommon.Buffers;
using NetScaleCommon.Collections;
using NetScaleCommon.Interfaces.Network;
using NetScaleCommon.Peer;
using UnityEngine;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Network
{
    public class ClientToProxy : IDisposable
    {
        public event Action<ClientToProxy> Connected;
        public event Action<ClientToProxy> Disconnected;
        public event Action OnConnectionTimeout;
    
        private FixedLockFreeDictionary<ushort, List<Handler<ushort>>> _handlers = new(512);
        private readonly IApiInstance<ushort> _apiInstance;
        private readonly ILogger _logger;
        private readonly MpmcRingBufferNew<OutgoingPacketForMemoryPack<ushort, UShortCodec>> _ring;
        private readonly MpmcRingBufferWithTtl<OutgoingPacketForMemoryPack<ushort, UShortCodec>> _ttlRing;
        private Guid _connectionId;
        private MpmcRingBufferConsumer<ushort, UShortCodec> _consumer;
        private BufferPublisherForMemoryPack<ushort, UShortCodec> _serializer;
        private IConnection<ushort> _connection;

        public ClientToProxy(IApiInstance<ushort> apiInstance, ILogger logger, MpmcRingBufferNew<OutgoingPacketForMemoryPack<ushort, UShortCodec>> ring, MpmcRingBufferWithTtl<OutgoingPacketForMemoryPack<ushort, UShortCodec>> ttlRing)
        {
            _apiInstance = apiInstance;
            _logger = logger;
            _ring = ring;
            _ttlRing = ttlRing;
        }
 
        public void InitClient()
        {
            _connectionId = Guid.NewGuid();
            _apiInstance.InitClient("ProxyServer", false, true);
 
            _apiInstance.Connected += OnConnected;
            _apiInstance.Disconnected += OnDisconnected;
        
            _apiInstance.OnConnectionTimeout += () =>
            {
                _logger.LogInformation($"Timeout");
            };

            _apiInstance.OnInfo += (message) => { _logger.LogInformation($"message:{message}"); };
            _apiInstance.OnError += (message) =>
            {
                _logger.LogError($"message:{message}");
                Debug.LogError($"OnError:{message}");
            };
 
            _serializer = new BufferPublisherForMemoryPack<ushort, UShortCodec>(_ring, _ttlRing);
            _consumer = new MpmcRingBufferConsumer<ushort, UShortCodec>(_connectionId, _ring, _ttlRing);
 
        }

        public void Update(float deltaTime)
        {
            try
            {
                _apiInstance.ReadMessage();
                _consumer.ConsumeAndSend();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        public bool TryGetStatistics(out long packetsSent, out long packetsReceived, out long bytesSent,
            out long bytesReceived)
        {
            if (_connection == null)
            {
                packetsReceived = 0;
                packetsSent = 0;
                bytesReceived = 0;
                bytesSent = 0;
                return false;
            }

            var statistics = _connection.Statistics;
            packetsSent = statistics.PacketsSent;
            packetsReceived = statistics.PacketsReceived;
            bytesSent = statistics.BytesSent;
            bytesReceived = statistics.BytesReceived;
            return true;
        }

        public void Connect(string host, int port)
        {
            _apiInstance.Connect(host, port);
        }
        private void OnDisconnected(IConnection<ushort> connection)
        {
            _logger.LogInformation($"Закрыто соединение: {connection}");
            connection.UnRegisterCallback(OnProcessMessage);
            _consumer.OnDisconnected(_connectionId);
            Disconnected.Invoke(this);
            _connection = null;
        }
 
        private void OnConnected(IConnection<ushort> connection)
        {
            _connection = connection;
            _logger.LogInformation($"Исходящее соединение: {connection}");
            connection.RegisterCallback(OnProcessMessage);
            _consumer.OnConnecteed(_connectionId, connection);
            Connected.Invoke(this);
        }
    
        private void OnProcessMessage(in ushort key, in IReader reader, in object inc)
        {
            try
            {
                if (!_handlers.TryGetValue(key, out var handlers))
                {
                    handlers = new List<Handler<ushort>>();
                    _logger.LogWarning($"Don`t know any handlers for index: {key.ToString()}");
                    return;
                }

                foreach (var handler in handlers!)
                {
                    handler.Delegat.Invoke(in key, in reader, in inc);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }
    
    
        public void Listen(ushort routeIndexator, Handler<ushort>.Callback eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.Bind(eventHandler);
            handlers.Add(handler);
        }
 
        public void Listen<T>(ushort routeIndexator, Handler<ushort>.Callback<T> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.Bind<T>(eventHandler);
            handlers.Add(handler);
        }
        
        public void ListenRaw(ushort routeIndexator, Handler<ushort>.Callback<Memory<byte>> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.BindRaw(eventHandler);
            handlers.Add(handler);
        }
        public void Listen<T1, T2>(ushort routeIndexator, Handler<ushort>.Callback<T1, T2> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.Bind<T1, T2>(eventHandler);
            handlers.Add(handler);
        }

        public void Listen<T1, T2, T3>(ushort routeIndexator, Handler<ushort>.Callback<T1, T2, T3> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.Bind<T1, T2, T3>(eventHandler);
            handlers.Add(handler);
        }       
        
        public void StopListening<T>(ushort routeIndexator)
        {
            if (!_handlers.TryRemove(routeIndexator))
            {
                throw new Exception($"Don `t have a handler with route index: {routeIndexator}");
            }
        }   
 
        public void StopListening<T>(ushort routeIndexator, Handler<ushort>.Callback<T> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                throw new Exception($"Don `t have a handler with route index: {routeIndexator}");
            }
            
            handlers.RemoveAll(h => h.GetHashCode() == eventHandler.GetHashCode());
            if (handlers.Count == 0)
            {
                _handlers.TryRemove(routeIndexator);
            }
        }

        public void Publish(in ushort key, in DeliveryMethod deliveryMethod)
        {
            _serializer.Publish(in key, in deliveryMethod);
        }

        public void Publish<T>(in ushort key, in DeliveryMethod deliveryMethod, in T t1)
        {
            _serializer.Publish(in key, in deliveryMethod, in t1);
        }

        public void Publish<T1, T2>(in ushort key, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2)
        {
            _serializer.Publish(in key, in deliveryMethod, in t1, in t2);
        }

        public void Publish<T1, T2, T3>(in ushort key, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2, in T3 t3)
        {
            _serializer.Publish(in key, in deliveryMethod, in t1, in t2, in t3);
        }

        public void PublishAndForgetExpiring(in TimeSpan exp, in ushort key, in DeliveryMethod deliveryMethod)
        {
            _serializer.PublishAndForgetExpiring(in exp, in key, in deliveryMethod);
        }

        public void PublishAndForgetExpiring<T1>(in TimeSpan exp, in ushort key, in DeliveryMethod deliveryMethod, in T1 t1)
        {
            _serializer.PublishAndForgetExpiring(in exp, in key, in deliveryMethod, in t1);
        }

        public void PublishAndForgetExpiring<T1, T2>(in TimeSpan exp, in ushort key, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2)
        {
            _serializer.PublishAndForgetExpiring(in exp, in key, in deliveryMethod, in t1, in t2);
        }

        public void PublishAndForgetExpiring<T1, T2, T3>(in TimeSpan exp, in ushort key, in DeliveryMethod deliveryMethod, in T1 t1, in T2 t2, in T3 t3)
        {
            _serializer.PublishAndForgetExpiring(in exp, in key, in deliveryMethod, in t1, in t2, in t3);
        }
        private void ListenForAsk<T>(ushort routeIndexator, Handler<ushort>.Callback<T> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                handlers = new List<Handler<ushort>>();
                _handlers.TryAdd(routeIndexator, handlers);
            }

            var handler = new Handler<ushort>();
            handler.Bind<T>(eventHandler);
            handlers.Add(handler);
        }
        
        
        public async Task<TResponse> AskAsync<TRequest, TResponse>(ushort requestIndex,
            DeliveryMethod netDeliveryMethod, TRequest request, TimeSpan timeout)
        {
            TResponse responses = default;

            var responseReceived = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(timeout);

            var responseIndex = requestIndex;

            void ResponseHandler(in TResponse response)
            {
                responses = response;
                responseReceived.TrySetResult(true);
            }
            ListenForAsk<TResponse>(responseIndex, ResponseHandler);
            try
            {
                var envelope = new QueryEnvelope<TRequest, ushort>(request, responseIndex);

                _serializer.Publish(requestIndex, netDeliveryMethod, envelope);

                using (cts.Token.Register(() => responseReceived.TrySetCanceled(), useSynchronizationContext: false))
                {
                    await responseReceived.Task;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Request timed out");
            }
            finally
            {
                await StopListeningForAsk<TResponse>(responseIndex, ResponseHandler);
            }

            _logger.LogInformation("Ask completed");
            return responses;
        }
        
        private async Task StopListeningForAsk<T>(ushort routeIndexator, Handler<ushort>.Callback<T> eventHandler)
        {
            if (!_handlers.TryGetValue(routeIndexator, out var handlers))
            {
                throw new Exception($"Don `t have a handler with route index: {routeIndexator}");
            }

            handlers.RemoveAll(h => h.GetHashCode() == eventHandler.GetHashCode());
            if (handlers.Count == 0)
            {
                _handlers.TryRemove(routeIndexator);
            }
        }
        public void Dispose()
        {
            _handlers = new(512);
            _apiInstance?.Dispose();
        }
    }
}

