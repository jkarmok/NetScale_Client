using UnityEngine;

namespace SceneEditor
{
    public class TeamSpawnPoint : PointBase
    {
        [Header("Team Settings")]
        [SerializeField] private int _teamId = 0;
        [SerializeField] private string _teamName = "Blue Team";
        [SerializeField] private int _maxPlayers = 5;
        [SerializeField] private float _respawnDelay = 3f;
    
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = GetTeamColor();
            Gizmos.DrawCube(transform.position + Vector3.up * 2, 
                new Vector3(1.5f, 0.5f, 1.5f));
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
    
        public override object ToJson()
        {
            return new {
                _teamId,
                _teamName,
                _maxPlayers,
                _respawnDelay,
                id = PointId,
                name = PointName,
                position = new
                {
                    x = transform.position.x,
                    y = transform.position.y,
                    z = transform.position.z
                },
                rotation = new
                {
                    x = transform.rotation.eulerAngles.x,
                    y = transform.rotation.eulerAngles.y,
                    z = transform.rotation.eulerAngles.z
                },
                type = this.GetType().Name
            };
        }
    }
}