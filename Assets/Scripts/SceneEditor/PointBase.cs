using Game.SceneLoader.Points;

namespace SceneEditor
{
    using UnityEngine;
    using System;

    [Serializable]
    public abstract class PointBase : MonoBehaviour
    {
        [Header("Base Settings")] private Guid _pointId = Guid.NewGuid();
        [SerializeField] private string _pointName = "New Point";
        [SerializeField] private Color _gizmoColor = Color.white;
        [SerializeField] private float _gizmoRadius = 1f;
        public Guid PointId => _pointId;
        public string PointName
        {
            get => _pointName;
            set => _pointName = value;
        }

        public Color GizmoColor
        {
            get => _gizmoColor;
            set => _gizmoColor = value;
        }

        public float GizmoRadius
        {
            get => _gizmoRadius;
            set => _gizmoRadius = value;
        }

        public virtual void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, _gizmoRadius);
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius * 1.2f);
        }

        public abstract void ToJson(Game.SceneLoader.ParseResult parseResult);
    }
}