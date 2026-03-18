using UnityEngine;

namespace Physics3D
{
    public class Projectile
    {
        public bool IsCompleted { get; private set; }
        public event System.Action<Projectile, GameObject> OnCompleted;
        public event System.Action<Projectile> OnUpdated;

        private GameObject _parent;
        private Vector3 _direction;
        private Vector3 _position;
        private Vector3 _contactPoint;
        private Vector3 _gravityVelocity;
        private Vector3 _normal;
        private float _speed;
        private float _maxDistance;
        private readonly float _gravity;
        private readonly LayerMask _hitMask;
        private readonly float _radius;
        private bool _isHit;
 
        public float TraveledDistance { get; private set; }
        public Vector3 Position => _position;
        public Vector3 Direction => _direction;
        public Vector3 ContactPoint => _contactPoint;
        public Vector3 Normal => _normal;
        public float Speed => _speed;
        public float MaxDistance => _maxDistance;
        public GameObject HitObject { get; private set; }

        public Projectile(
            GameObject parent, 
            Vector3 origin, 
            Vector3 direction, 
            float speed, 
            float maxDistance, 
            float gravity,
            float radius,
            LayerMask hitMask)
        {
            _parent = parent;
            _position = origin;
            _direction = direction.normalized;
            _speed = speed;
            _maxDistance = maxDistance;
            _gravity = gravity;
            _radius = radius;
            _hitMask = hitMask; // По умолчанию все слои
            _gravityVelocity = Vector3.zero;
            TraveledDistance = 0;
        }

        public void Update(float deltaTime)
        {
            if (IsCompleted) return;
 
            var origin = _position;
            float deltaDistance = _speed * deltaTime;
 
            _gravityVelocity += Vector3.down * _gravity * deltaTime;
             
            Vector3 newPosition = _position + _direction * deltaDistance + _gravityVelocity * deltaTime;
            Vector3 sweepDirection = newPosition - origin;
            float sweepLength = sweepDirection.magnitude;
 
            RaycastHit hitInfo;
            bool hit = Physics.SphereCast(
                origin,
                _radius,
                sweepDirection.normalized,
                out hitInfo,
                sweepLength,
                _hitMask);

            OnUpdated?.Invoke(this);

            if (hit && CanHit(hitInfo.collider.gameObject))
            {
                _isHit = true;
                _contactPoint = hitInfo.point;
                _normal = hitInfo.normal;
                _position = hitInfo.point;
                HitObject = hitInfo.collider.gameObject;
                
                Completed(HitObject);
            }
            else
            {
                _position = newPosition;
                TraveledDistance += deltaDistance;

                if (TraveledDistance >= _maxDistance)
                {
                    Completed(null);
                }
            }
        }

        private bool CanHit(GameObject hitObject)
        {
            if (hitObject == _parent)
                return false;
                
            // Можно добавить дополнительные проверки
            // Например, по тегам или компонентам
            
            return true;
        }

        private void Completed(GameObject hitObject)
        {
            IsCompleted = true;
            OnCompleted?.Invoke(this, hitObject);
 
            // Очищаем подписки для предотвращения утечек памяти
            OnCompleted = null;
            OnUpdated = null;
        }
    }
}