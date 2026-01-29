using UnityEngine;

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
    
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
    }
}