using UnityEngine;
using System.Linq;

[RequireComponent(typeof(TurretStats))]
public class TeslaTurret : MonoBehaviour, IShootingBehavior
{
    [SerializeField] private Transform firePoint;

    private ITurretStats stats;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        
        if (stats == null)
            Debug.LogError("[TeslaTurret] No se encontró ITurretStats en la torreta.");
    }

    public void Shoot(Transform firePoint, Transform targetIgnored, ITurretStats stats)
    {
        if (stats == null) return;

        //encuentra todos los enemigos activos dentro del rango
        var targets = EnemyTracker.GetActiveEnemies()
            .Where(e => Vector3.Distance(transform.position, e.transform.position) <= stats.Range)
            .ToArray();

        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) continue; //si se cumple alguna de las 3, sigue con el sig enemigo

            GameObject proj = ProjectilePool.Instance.GetProjectile();
            proj.transform.position = firePoint.position;
            proj.transform.rotation = Quaternion.identity;
            
            var projScript = proj.GetComponent<Projectile>();
            
            if (projScript != null)
                projScript.Initialize(enemy.transform, stats.Damage);
        }

        Debug.Log($"Shooting {targets.Length} enemies");
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (stats != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stats.Range);
        }
    }
#endif
}


