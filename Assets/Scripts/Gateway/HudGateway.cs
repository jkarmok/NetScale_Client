using System;
using Game;
using Game.Enums2;
using Network;

namespace Gateway
{
    public class HudGateway
    {
        public event Action<HudDto> HudUpdated;
        private readonly ClientToProxy _clientToProxy;
        
        public HudGateway(ClientToProxy connection)
        {
            _clientToProxy = connection;
            _clientToProxy.Listen<HudDto>((ushort)RpcTypes.HudUpdate, OnHudUpdate);
        }

        private void OnHudUpdate(in HudDto hudDto)
        {
            HudUpdated.Invoke(hudDto);
        }
    }
}