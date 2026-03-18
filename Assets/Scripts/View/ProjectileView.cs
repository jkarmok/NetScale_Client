using System;
using Game.Scene;
using Physics3D;
using UnityEngine;
using View;

public class ProjectileView : MonoBehaviour, IView
{
    [SerializeField] private Transform projectileViewTransform;
    [SerializeField] private float _projectileSpeed = 100f;
    [SerializeField] private float _maxDistance = 300f;
    [SerializeField] private float _gravity = 9.8f;
    [SerializeField] private float _projectileRadius = 0.1f;
    [SerializeField] private LayerMask _hitMask = -1;
    
    private Projectile _activeProjectile;
    
    public void SpawnSetup(byte viewId, int spatialOwnerId, int entityProxyId,
        TransformState entityTransformState, Vector3 position, Quaternion rotation, bool owned)
    {
        projectileViewTransform.localPosition = Vector3.zero;
        projectileViewTransform.localRotation = Quaternion.identity;
        _activeProjectile = new Projectile(
            parent: gameObject,
            origin: transform.position,
            direction: transform.forward,
            speed: _projectileSpeed,
            maxDistance: _maxDistance,
            gravity: _gravity,
            radius: _projectileRadius,
            hitMask: _hitMask
        );
        
        _activeProjectile.OnCompleted += OnProjectileCompleted;
        _activeProjectile.OnUpdated += OnProjectileUpdated;
    }

    private void OnDestroy()
    {
        if (_activeProjectile != null)
        {
            _activeProjectile.OnCompleted -= OnProjectileCompleted;
            _activeProjectile.OnUpdated -= OnProjectileUpdated;
        }
    }

    void Update()
    {
        _activeProjectile?.Update(Time.deltaTime);
    }
    
    private void OnProjectileCompleted(Projectile projectile, GameObject hitObject)
    {
        if (hitObject != null)
        {
            Debug.Log($"Попадание в: {hitObject.name} в точке {projectile.ContactPoint}");
            
            // Здесь можно создать эффект попадания через Object.Instantiate
            // или вызвать методы на компонентах
            hitObject.SendMessage("OnHit", projectile, SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.Log("Снаряд достиг максимальной дистанции");
        }
        
        projectile.OnCompleted -= OnProjectileCompleted;
        projectile.OnUpdated -= OnProjectileUpdated;
        
        if (_activeProjectile == projectile)
        {
            _activeProjectile = null;
        }
    }

    private void OnProjectileUpdated(Projectile projectile)
    {
        // Можно использовать для отладки или визуализации
        Debug.DrawLine(projectile.Position, projectile.Position + projectile.Direction * 0.5f, Color.yellow);
        projectileViewTransform.position = projectile.Position;
       
    }
}