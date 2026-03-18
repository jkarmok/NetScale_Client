using System;
using Game.SceneLoader;
using Game.SceneLoader.Points;
using UnityEngine;
using Vector3 = System.Numerics.Vector3;

namespace SceneEditor
{
    public class BotSpawnPoint : PointBase
    {
        [Header("Bot Settings")]
        [SerializeField] private string _botType = "guard";
        [SerializeField] private int _botLevel = 1;
        [SerializeField] private float _spawnRadius = 2f;
        [SerializeField] private int _maxBots = 3;
        [SerializeField] private float _respawnInterval = 30f;
        [SerializeField] private int _teamId = 0;
        [SerializeField] private string _goapAgentSettings = "";
          
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }

        public override void ToJson(ParseResult parseResult)
        {
            if (string.IsNullOrWhiteSpace(_botType))
            {
                throw new Exception($"Cannot create bot spawn point for bot type {_botType}");
            }

            if (string.IsNullOrWhiteSpace(_goapAgentSettings))
            {
                throw new Exception($"You need to specify bot settings for bot type {_botType}");
            }
            parseResult.BotSpawnPoints.Add(new BotSpawnPointDTO()
                {
                    TeamId = _teamId,
                    BotType = _botType,
                    Level = _botLevel,
                    SpawnRadius = _spawnRadius,
                    MaxBots = _maxBots,
                    SpawnInterval = _respawnInterval,
                    Id = PointId,
                    Name = PointName,
                    Position = new Vector3(transform.position.x,  transform.position.y, transform.position.z),
                    Rotation = new Vector3(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z ),
                    Type = this.GetType().Name,
                    GoapAgentSettings = _goapAgentSettings
                }
            );
        }

 
    }
}