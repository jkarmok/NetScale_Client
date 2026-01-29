
using System.Numerics;
using Game;
using Game.Enums2;
using Game.Scene;
using GameCommonExternal.Extensions;
using NetScaleCommon;
using Network;
using Vector2 = UnityEngine.Vector2;

namespace Gateway
{
    public class InputGateway
    {
        private readonly ClientToProxy _clientToProxy;
        
        public InputGateway(ClientToProxy connection)
        {
            _clientToProxy = connection;
           
        }
        
        private InputDto _inputDto;
        
        public void Publish(Vector2 movmentDirection)
        {
            _inputDto = new InputDto();
            new Vector3(movmentDirection.x,0, -movmentDirection.y).EncodeVector3ToHalfVector3(out _inputDto.MovementDirection);
            _inputDto.Skill01.Used = true;
            _inputDto.Skill01.SkillDirection = new HalfVector3(55, 33, 556);
            _clientToProxy.Publish(RpcTypes.InputUpdate, DeliveryMethod.UnreliableSequenced, _inputDto);
        }
    }
}