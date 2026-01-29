using System;
using Game.Scene;
using Game.Scene.Formatters;
using Infrastructure;
using LiteNetLibAPI;
using MemoryPack;
using NetScaleCommon;
using NetScaleCommon.Buffers;
using NetScaleCommon.Interfaces.Network;
using Network;
using VContainer;

namespace Installers
{
    public class NetworkInstaller : IDisposable
    {
        private ClientToProxy _clientToProxy;
        private GameLoop _gameLoop;
        private BufferPublisherForMemoryPack<ushort, UShortCodec> _serializer;

        public void Configure(IContainerBuilder builder, GameLoop gameLoop)
        {
            _gameLoop = gameLoop;
            const int maxMessageSize = 64;
            const int messageCount = 10_000;
            
            MemoryPackFormatterProvider.Register(new TransformDeltaDtoFormatter());
            MemoryPackFormatterProvider.Register(new TransformDeltaDtoArraySegmentFormatter());
            MemoryPackFormatterProvider.Register(new TransformStateDtoFormatter());
            MemoryPackFormatterProvider.Register(new TransformStateDtoArraySegmentFormatter());
            
            MpmcRingBufferNew<OutgoingPacketForMemoryPack<ushort, UShortCodec>> ring =
                new MpmcRingBufferNew<OutgoingPacketForMemoryPack<ushort, UShortCodec>>(
                    BufferCapacityCalculator.CalculateCapacity(messageCount, 0.8f), 1,
                    () => new OutgoingPacketForMemoryPack<ushort, UShortCodec>(maxMessageSize), 64);
            var ttlRing = new MpmcRingBufferWithTtl<OutgoingPacketForMemoryPack<ushort, UShortCodec>>(messageCount,
                () => new OutgoingPacketForMemoryPack<ushort, UShortCodec>(maxMessageSize));

            _serializer = new BufferPublisherForMemoryPack<ushort, UShortCodec>(ring, ttlRing);
            _clientToProxy = new ClientToProxy(new LiteNetLibApiInstance<ushort, UShortCodec>(),
                new NetScaleLoggerWrapper(), ring, ttlRing);

            _clientToProxy.Connected += OnConnected;
            _clientToProxy.Disconnected += OnDisconnected;
            _clientToProxy.InitClient();

            builder.RegisterInstance(_serializer);
            builder.RegisterInstance(_clientToProxy);
            builder.Register<MemoryPackBinarySerializer>(Lifetime.Singleton).As<IBinarySerializer>();
            _gameLoop.Updated += OnUpdated;
        }

        private void OnUpdated(float deltaTime)
        {
            _clientToProxy.Update(deltaTime);
        }

        private void OnDisconnected(ClientToProxy clientToProxy)
        {
      
        }

        private void OnConnected(ClientToProxy clientToProxy)
        {
 
        }
 
        public void Dispose()
        {
            _gameLoop.Updated -= OnUpdated;
            _clientToProxy.Dispose();
        }
    }
}