using UnityEngine;

namespace SceneEditor
{
    using UnityEngine;
    using System;

    [Serializable]
    public abstract class PointBase : MonoBehaviour
    {
        [Header("Base Settings")] private string _pointId = Guid.NewGuid().ToString();
        [SerializeField] private string _pointName = "New Point";
        [SerializeField] private Color _gizmoColor = Color.white;
        [SerializeField] private float _gizmoRadius = 1f;
        public string PointId => _pointId;
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

        public virtual object ToJson() => new
        {
            id = _pointId,
            name = _pointName,
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