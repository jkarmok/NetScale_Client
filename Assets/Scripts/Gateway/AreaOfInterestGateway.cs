using System;
using Game;
using Game.Enums2;
using Game.Scene;
using GameCommonExternal.DeltaCompression;
using NetScaleCommon;
using NetScaleCommon.StructDeltaCompression;
using Network;
using UnityEngine;

namespace Gateway
{
    public delegate void EntityLifecycleBatchDelegate(in EntityLifecycleBatchDto entityLifecycleBatchDto);
    public delegate void TransformBatchDelegate(in TransformBatchStateDto transformBatchDto);
    public class AreaOfInterestGateway
    {
        public event EntityLifecycleBatchDelegate EntityLifecycleUpdate;
        public event TransformBatchDelegate TransformUpdate;
        private readonly ClientToProxy _clientToProxy;
 
        private TransformBatchDeltaDto transformBatchDto;
        private readonly IClientDeltaProvider<TransformBatchStateDto, TransformBatchDeltaDto> _batchProvider;
        private readonly ClientSnapshotBuffer<TransformBatchStateDto> _snapshotBuffer;
        private readonly TransformStateDeltaClientProvider _transformProvider;

        public AreaOfInterestGateway(ClientToProxy connection)
        {
            _clientToProxy = connection;
            _transformProvider = new TransformStateDeltaClientProvider();
            _batchProvider = new TransformBatchDeltaClientProvider(4096, _transformProvider);
            _snapshotBuffer = new ClientSnapshotBuffer<TransformBatchStateDto>(120);
            _clientToProxy.Listen<EntityLifecycleBatchDto>((ushort)RpcTypes.EntityLifecycleUpdate, HandleEntityLifecycleUpdate);
            _clientToProxy.Listen<TransformBatchDeltaDto>((ushort)RpcTypes.EntityTransformDelta, HandleEntityTransformDelta);
        }
 
        private void HandleEntityLifecycleUpdate(in EntityLifecycleBatchDto entityLifecycleBatchDto)
        {
            /*
            foreach (var entityDto in entityLifecycleBatchDto.Entered)
            {
                Debug.Log($"Entered {entityDto.Id}");
            }

            foreach (var exitedId in entityLifecycleBatchDto.ExitedIds)
            {
                Debug.Log($"Exited {exitedId}");
            }*/
            EntityLifecycleUpdate.Invoke(in entityLifecycleBatchDto);
        }

        private void HandleEntityTransformDelta(in TransformBatchDeltaDto transformBatchDelta)
        {
            try
            {
                if (transformBatchDelta.IsFullSnapshot)
                {
                    var state = _batchProvider.ApplySnapshot(in transformBatchDelta);
                    _snapshotBuffer.SaveSnapshot(state.Tick, state);
                    _clientToProxy.Publish(RpcTypes.ConfirmTransformTick, DeliveryMethod.Unreliable, state.Tick);

                    TransformUpdate.Invoke(in state);
                }
                else
                {
                    if (_snapshotBuffer.TryGetSnapshot(transformBatchDelta.BaseTick, out var baseState))
                    {
                        var state = _batchProvider.ApplyDelta(in baseState, in transformBatchDelta);
                        _snapshotBuffer.SaveSnapshot(state.Tick, state);
                        _clientToProxy.Publish(RpcTypes.ConfirmTransformTick, DeliveryMethod.Unreliable, state.Tick);
                        TransformUpdate.Invoke(in state);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Missing base snapshot for tick {transformBatchDelta.BaseTick}. " +
                            $"Current buffer range: unknown. " +
                            $"Requesting full snapshot.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to process transform delta: {ex.Message}");
                Debug.LogException(ex);
            }
        }
    }
}