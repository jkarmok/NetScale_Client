using System.Collections.Generic;
using Game.Scene;
using UnityEngine;

namespace View
{
    public class CharacterView : MonoBehaviour, IView
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

        [SerializeField] private List<SkinnedMeshRenderer> _meshRenderer;
        
        public void SpawnSetup(byte viewId, int spatialOwnerId, int entityProxyId,
            TransformState entityTransformState, Vector3 position, Quaternion rotation, bool owned)
        {
            foreach (var skinnedMeshRenderer in _meshRenderer)
            {
                skinnedMeshRenderer.material.SetColor(BASE_COLOR, _colors[Mathf.Abs(spatialOwnerId) % _colors.Length]);
            }
        }
    }
}