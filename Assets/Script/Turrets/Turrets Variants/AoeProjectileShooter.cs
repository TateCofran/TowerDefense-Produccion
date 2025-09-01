using UnityEngine;

public class AoeProjectileShooter : MonoBehaviour, IShootingBehavior
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float explosionRadius = 2.5f;

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (target == null || firePoint == null) return;

        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;

        var p = projectile.GetComponent<Projectile>();
        p.Initialize(target, stats.Damage);

        // Configuración para proyectil de área
        p.isAOE = true;
        p.explosionRadius = explosionRadius;
    }
}
