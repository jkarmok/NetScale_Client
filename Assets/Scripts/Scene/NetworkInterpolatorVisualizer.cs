using UnityEngine;

namespace Scene
{
    public class NetworkInterpolatorVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkEntity _networkEntity;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool _showWhenSelected = true;
        [SerializeField] private bool _showAlways = false;
        
        [Header("Colors")]
        [SerializeField] private Color _serverColor = Color.red;
        [SerializeField] private Color _interpolatedColor = Color.green;
        [SerializeField] private Color _extrapolatedColor = Color.yellow;
        [SerializeField] private Color _connectionColor = Color.white;

        [Header("Sizes")]
        [SerializeField] private float _serverSphereSize = 1f;
        [SerializeField] private float _clientSphereSize = 1f;

        private Vector3 _lastServerPosition;
        private Quaternion _lastServerRotation;
        private double _lastServerUpdateTime;
        private bool _hasServerData;

        private void OnValidate()
        {
            if (_networkEntity == null)
                _networkEntity = GetComponent<NetworkEntity>();
        }

        public void UpdateServerData(Vector3 position, Quaternion rotation, double serverTime)
        {
            _lastServerPosition = position;
            _lastServerRotation = rotation;
            _lastServerUpdateTime = serverTime;
            _hasServerData = true;
        }

        private void OnDrawGizmos()
        {
            if (_showAlways)
            {
                DrawVisualization();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_showWhenSelected)
            {
                DrawVisualization();
            }
        }

        private void DrawVisualization()
        {
            if (_networkEntity == null || !_hasServerData) return;

            var stats = _networkEntity.GetInterpolationStats();
            if (!stats.isTimeSynced) return;

            DrawServerPosition();
            DrawClientPosition();
            DrawConnectionLine();
            DrawInfoText(stats);
        }

        private void DrawServerPosition()
        {
            Gizmos.color = _serverColor;
            Gizmos.DrawWireSphere(_lastServerPosition, _serverSphereSize);
            Gizmos.DrawRay(_lastServerPosition, _lastServerRotation * Vector3.forward * 1f);
            
            DrawLabel(_lastServerPosition + Vector3.up * 1.5f, 
                     $"SERVER\nTime: {_lastServerUpdateTime:F2}", 
                     _serverColor);
        }

        private void DrawClientPosition()
        {
            Gizmos.color = GetClientStateColor();
            Gizmos.DrawWireSphere(transform.position, _clientSphereSize);
            Gizmos.DrawRay(transform.position, transform.rotation * Vector3.forward * 0.7f);
        }

        private void DrawConnectionLine()
        {
            Gizmos.color = _connectionColor;
            Gizmos.DrawLine(_lastServerPosition, transform.position);
            
            Vector3 direction = (transform.position - _lastServerPosition).normalized;
            float distance = Vector3.Distance(_lastServerPosition, transform.position);
            
            if (distance > 0.1f)
            {
                DrawArrow(_lastServerPosition, direction, Mathf.Min(distance * 0.3f, 0.5f));
            }
        }

        private void DrawInfoText(NetworkInterpolator.InterpolationStats stats)
        {
            Vector3 rootPos = transform.position;
            float verticalOffset = 2f;

            double timeDiff = stats.currentLatency;
            string timeStatus = timeDiff < 0 ? "PREDICTING" : "LAGGING";
            float distance = Vector3.Distance(_lastServerPosition, transform.position);

            string infoText = $"NETWORK ENTITY\n" +
                             $"Status: {timeStatus}\n" +
                             $"Offset: {Mathf.Abs((float)timeDiff):F3}s\n" +
                             $"Distance: {distance:F2}m\n" +
                             $"Updates: {stats.updatesReceived}\n";

            DrawLabel(rootPos + Vector3.up * verticalOffset, infoText, GetClientStateColor(stats));
        }

        private Color GetClientStateColor(NetworkInterpolator.InterpolationStats stats)
        {
            double timeDiff = stats.currentLatency;

            if (timeDiff < 0)
                return Color.cyan;
            else if (timeDiff < 0.05)
                return _interpolatedColor;
            else if (timeDiff < 0.2)
                return _extrapolatedColor;
            else
                return Color.magenta;
        }

        private Color GetClientStateColor()
        {
            if (_networkEntity == null) return Color.gray;
            return GetClientStateColor(_networkEntity.GetInterpolationStats());
        }

        private void DrawLabel(Vector3 position, string text, Color color)
        {
            #if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = 10;
            style.fontStyle = FontStyle.Bold;
            UnityEditor.Handles.Label(position, text, style);
            #endif
        }

        private void DrawArrow(Vector3 position, Vector3 direction, float size)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = _connectionColor;
            UnityEditor.Handles.ArrowHandleCap(0, position, 
                Quaternion.LookRotation(direction), size, EventType.Repaint);
            #endif
        }
    }
}