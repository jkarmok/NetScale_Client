using Game.SceneLoader;
using Game.SceneLoader.Points;
using UnityEngine;

namespace SceneEditor
{
    public class TeamBasePoint : PointBase
    {
        [Header("Team Settings")]
        [SerializeField] private int _teamId = 0;
        [SerializeField] private string _teamName = "Blue Team";
        [SerializeField] private float _maxHealth = 5;
        [SerializeField] private float _respawnDelay = 3f;
    
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = GetTeamColor();
            Gizmos.DrawCube(transform.position + Vector3.up * 2, 
                new Vector3(1.5f, 0.5f, 1.5f));
        }

        public override void ToJson(ParseResult parseResult)
        {
           parseResult.TeamBasePoints.Add(new TeamBasePointDTO(){
               TeamId = _teamId,
               TeamName = _teamName,
               MaxHealth = _maxHealth,
               RespawnDelay = _respawnDelay,
               Id = PointId,
               Name = PointName,
               Position = new System.Numerics.Vector3(transform.position.x, transform.position.y, transform.position.z),
               Rotation = new System.Numerics.Vector3(transform.rotation.eulerAngles.x,  transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z)
           });
        }

        private Color GetTeamColor()
        {
            return _teamId switch
            {
                0 => Color.blue,
                1 => Color.red,
                2 => Color.green,
                3 => Color.yellow,
                _ => Color.gray
            };
        }
  
    }
}