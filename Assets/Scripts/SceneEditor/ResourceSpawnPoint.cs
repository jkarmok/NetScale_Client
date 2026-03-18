using Game.SceneLoader;
using Game.SceneLoader.Points;
using UnityEngine;

namespace SceneEditor
{
    public class ResourceSpawnPoint : PointBase
    {
        [Header("Resource Settings")] [SerializeField]
        private string _resourceType = "wood";

        [SerializeField] private int _resourceAmount = 100;
        [SerializeField] private float _respawnTime = 60f;
        [SerializeField] private Vector3 _spawnArea = new Vector3(3, 0, 3);
        [SerializeField] private int _teamId = 0;

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, _spawnArea);
        }

        public override void ToJson(ParseResult parseResult)
        {
            parseResult.ResourceSpawnPoints.Add(new ResourceSpawnPointDTO
            {
                TeamId = _teamId,
                ResourceType = _resourceType,
                ResourceAmount = _resourceAmount,
                RespawnTime = _respawnTime,
                Id = PointId,
                Name = PointName,
                Position =
                    new System.Numerics.Vector3(transform.position.x, transform.position.y, transform.position.z),
                Rotation = new System.Numerics.Vector3(transform.rotation.eulerAngles.x,
                    transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z),
                Type = this.GetType().Name
            });
        }
    }
}