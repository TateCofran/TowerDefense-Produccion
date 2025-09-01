using UnityEngine;

public class TurretShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    private IShootingBehavior shootingBehavior;
    private ITurretStats stats;
    private TurretDataHolder dataHolder;

    private float fireCountdown;
    private Transform currentTarget;

    void Awake()
    {
        stats = GetComponent<ITurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();
        shootingBehavior = GetComponent<IShootingBehavior>();

        if (shootingBehavior == null)
        {
            Debug.LogWarning($"[TurretShooter] No hay IShootingBehavior en {gameObject.name}. Este componente no disparará.");
            enabled = false;
        }
    }

    void Update()
    {
        if (currentTarget == null) return;

        fireCountdown -= Time.deltaTime;

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / stats.FireRate;
        }
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            currentTarget = null;
            return;
        }

        var enemy = target.GetComponent<Enemy>();
        if (enemy == null || enemy.WorldLogic == null)
        {
            currentTarget = null;
            return;
        }

        // puntar a cualquier enemigo, sin importar mundos
        currentTarget = target;
    }


    private void Shoot()
    {
        if (shootingBehavior == null || firePoint == null || stats == null)
            return;

        // PROBABILIDAD DE FALLO GLOBAL
        if (Random.value < TurretManager.Instance.GetGlobalTurretMissChance())
        {
            Debug.Log($"[TurretShooter] ¡Disparo fallido por miss chance global! ({TurretManager.Instance.GetGlobalTurretMissChance() * 100f:F1}% de probabilidad)");
            return;
        }

        shootingBehavior.Shoot(firePoint, currentTarget, stats);
    }

}
