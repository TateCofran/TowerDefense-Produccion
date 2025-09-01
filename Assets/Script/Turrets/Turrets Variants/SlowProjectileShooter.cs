using UnityEngine;

public class SlowProjectileShooter : MonoBehaviour, IShootingBehavior
{

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {
        if (target == null || firePoint == null) return;

        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;

        var p = projectile.GetComponent<Projectile>();
        p.Initialize(target, stats.Damage);

        p.Initialize(target, stats.Damage); 

    }
}
