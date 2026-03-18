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

        public void Publish(
            Vector2 movementDirection,
            bool hasSkill01, Vector2 skill01Direction, 
            bool hasSkill02, Vector2 skill02Direction, 
            bool hasSkill03, Vector2 skill03Direction,
            bool hasSkill04, Vector2 skill04Direction)
        {
            _inputDto = new InputDto();
            _inputDto.MovementDirection = EncodeVector2ToHalfVector2(movementDirection);
            
            _inputDto.Skill01.Used = hasSkill01;
            _inputDto.Skill01.SkillDirection = EncodeVector2ToHalfVector2(skill01Direction);
            
            _inputDto.Skill02.Used = hasSkill02;
            _inputDto.Skill02.SkillDirection = EncodeVector2ToHalfVector2(skill02Direction);
            
            _inputDto.Skill03.Used = hasSkill03;
            _inputDto.Skill03.SkillDirection = EncodeVector2ToHalfVector2(skill03Direction);
            
            _inputDto.Skill04.Used = hasSkill04;
            _inputDto.Skill04.SkillDirection = EncodeVector2ToHalfVector2(skill04Direction);
            
            
            _clientToProxy.Publish(RpcTypes.InputUpdate, DeliveryMethod.UnreliableSequenced, _inputDto);
        }

        private HalfVector2 EncodeVector2ToHalfVector2(Vector2 input)
        {
            return new HalfVector2(
                ShorteningEncoder.EncodeFloatToHalfPrecision(input.x), 
                ShorteningEncoder.EncodeFloatToHalfPrecision(input.y));
        }
    }
}