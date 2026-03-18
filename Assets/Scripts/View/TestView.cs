using Game.Scene;
using UnityEngine;

namespace View
{
    public class TestView : MonoBehaviour, IView
    {
        private static readonly int BASE_COLOR = Shader.PropertyToID("_BaseColor");

        private readonly Color[] _colors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.gray,
            Color.white,
            Color.black,
            Color.clear
        };

        [SerializeField] private MeshRenderer _meshRenderer;
        
        public void SpawnSetup(byte viewId, int spatialOwnerId, int entityProxyId,
            TransformState entityTransformState, Vector3 position, Quaternion rotation, bool owned)
        {
            _meshRenderer.material.SetColor(BASE_COLOR, _colors[Mathf.Abs(spatialOwnerId) % _colors.Length]);
        }
    }
}