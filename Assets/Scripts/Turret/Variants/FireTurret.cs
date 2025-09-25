using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class FireTurret : MonoBehaviour, IShootingBehavior
{
    private ITurretStats stats;

    [Header("Projectile settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Daño de fuego")]
    [SerializeField] private float tickRate = 1f;               // cuántos ticks por segundo
    [SerializeField] private float damageFractionPerSecond = 1f/2f;

    private float tickCountdown;
    private Enemy burningTarget;           // enemigo marcado para DoT
    private float totalDamageApplied = 0f; // no superar stats.Damage
    private bool hasFiredProjectile = false;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (stats == null)
            Debug.LogError("[FireTurret] No se encontró ITurretStats en la torreta.");

        tickCountdown = 0f;
    }

    private void Update()
    {
        if (burningTarget == null) return;

        tickCountdown -= Time.deltaTime;

        if (tickCountdown <= 0f)
        {
            ApplyBurnDamage();
            tickCountdown = 1f / tickRate;
        }
    }

    public void MarkTarget(Transform target)
    {
        if (target == null) return;

        //si ya hay un target vivo no lo cambia
        if (burningTarget != null && !burningTarget.Health.IsDead())
            return;

        burningTarget = target.GetComponent<Enemy>();
        totalDamageApplied = 0f;
        hasFiredProjectile = false; //para disparar la primer bala
    }

    public void Shoot(Transform firePoint, Transform target, ITurretStats stats)
    {

        if (burningTarget == null || burningTarget.Health == null || burningTarget.Health.IsDead())
            return;

        if (!hasFiredProjectile && target != null && projectilePrefab != null)
        {
            var projectile = ProjectilePool.Instance.GetProjectile();
            projectile.transform.position = firePoint.position;
            projectile.transform.rotation = firePoint.rotation;
            projectile.SetActive(true);

            var projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
                projScript.Initialize(target, 0); //daño lo maneja FireTurret

            hasFiredProjectile = true;
        }
        
        //para seguir haciendole daño al target
        MarkTarget(target);
    }

     private void ApplyBurnDamage()
     {
         if (burningTarget == null || burningTarget.Health == null) return;

         if (!burningTarget.Health.IsDead() && totalDamageApplied < stats.Damage)
         {
            float remainingDamage = stats.Damage - totalDamageApplied;
            float dps = stats.Damage * damageFractionPerSecond;
            float damageThisTick = Mathf.Min(dps, remainingDamage);

            float prevHealth = burningTarget.Health.GetCurrentHealth();
            burningTarget.Health.TakeDamage(damageThisTick);
            float newHealth = burningTarget.Health.GetCurrentHealth();

            float realApplied = prevHealth - newHealth;
            totalDamageApplied += realApplied;

            Debug.Log($"[{gameObject.name}]. Tick de fuego: {damageThisTick}, (real aplicado: {realApplied}), total: {totalDamageApplied}");
            Debug.Log("Enemy max health: " + burningTarget.Health.GetMaxHealth() + ". Enemy current health: " + burningTarget.Health.GetCurrentHealth());
        }
         else
         {
            Debug.Log($"[{gameObject.name}] DoT finalizado. Total aplicado: {totalDamageApplied}");
            burningTarget = null;
            totalDamageApplied = 0f;
            hasFiredProjectile = false;
         }
        
    }
}
