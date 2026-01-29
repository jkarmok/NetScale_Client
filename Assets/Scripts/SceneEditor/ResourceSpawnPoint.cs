using UnityEngine;

namespace SceneEditor
{
    public class ResourceSpawnPoint : PointBase
    {
        [Header("Resource Settings")]
        [SerializeField] private string _resourceType = "wood";
        [SerializeField] private int _resourceAmount = 100;
        [SerializeField] private float _respawnTime = 60f;
        [SerializeField] private Vector3 _spawnArea = new Vector3(3, 0, 3);
    
        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, _spawnArea);
        }
    }
}