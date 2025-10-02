using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class FireTurret : MonoBehaviour, IShootingBehavior
{
    private ITurretStats stats;

    [Header("Refs")]
    [SerializeField] private Transform firePoint;

    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask losBlockers = 0;
    [SerializeField] private float sphereCastRadius = 0.12f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw;

    [Header("Impact VFX (en la adquisición del objetivo)")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [SerializeField] private bool parentVfxToHit = true;
    [SerializeField] private float impactVfxLifetime = 1.5f;

    [Header("Daño de fuego (DoT)")]
    [SerializeField] private float tickRate = 1f;
    [SerializeField] private float damageFractionPerSecond = 1f / 2f;
    [Tooltip("Opcional: instanciar VFX en cada tick (en el centro del enemigo).")]
    [SerializeField] private bool vfxOnEachTick = false;

    private float tickCountdown;
    private Enemy burningTarget;
    private float totalDamageApplied = 0f;

    [Header("Impact Sound")]
    [SerializeField] private AudioClip impactClip; // suena cuando se adquiere target (y opcional en cada tick si vfxOnEachTick)
    [Range(0f, 1f)]
    [SerializeField] private float impactVolume = 1f;
    [SerializeField] private bool ensureAudioSource = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[FireTurret] Falta asignar firePoint.");
        tickCountdown = 0f;

        if (ensureAudioSource)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
                _audioSource.rolloffMode = AudioRolloffMode.Linear;
                _audioSource.minDistance = 1f;
                _audioSource.maxDistance = 20f;
                _audioSource.dopplerLevel = 0f;
            }
        }
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

    public void Shoot(Transform firePointIgnored, Transform target, ITurretStats statsParam)
    {
        if (!firePoint || target == null || stats == null) return;

        Vector3 o = firePoint.position;
        Vector3 hitPos = target.position + targetOffset;
        Vector3 dir = (hitPos - o);
        float dist = dir.magnitude;
        if (dist <= 0.001f) return;

        Vector3 ndir = dir / dist;

        // LOS opcional
        if (losBlockers != 0 &&
            Physics.Raycast(o, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
        {
            if (debugDraw) Debug.DrawLine(o, hitPos, Color.gray, 0.1f);
            return;
        }

        bool acquired = false;

        // Raycast directo
        if (Physics.Raycast(o, ndir, out RaycastHit hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            if (TryGetEnemy(hit.collider.transform, out var enemy))
            {
                SpawnImpactVfx(hit, ndir);
                StartOrRefreshBurn(enemy);
                if (debugDraw) Debug.DrawLine(o, hit.point, Color.red, 0.1f);
                acquired = true;
            }
        }

        // SphereCast fallback
        if (!acquired && sphereCastRadius > 0f &&
            Physics.SphereCast(o, sphereCastRadius, ndir, out RaycastHit shit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
        {
            if (TryGetEnemy(shit.collider.transform, out var enemy))
            {
                SpawnImpactVfx(shit, ndir);
                StartOrRefreshBurn(enemy);
                if (debugDraw) Debug.DrawLine(o, shit.point, Color.magenta, 0.1f);
                acquired = true;
            }
        }
    }

    private void StartOrRefreshBurn(Enemy enemy)
    {
        if (enemy == null || enemy.Health == null || enemy.Health.IsDead()) return;
        if (burningTarget == enemy) return;

        burningTarget = enemy;
        totalDamageApplied = 0f;
        tickCountdown = 0f;
    }

    private void ApplyBurnDamage()
    {
        if (burningTarget == null || burningTarget.Health == null) return;
        if (burningTarget.Health.IsDead())
        {
            burningTarget = null;
            totalDamageApplied = 0f;
            return;
        }

        if (totalDamageApplied < stats.Damage)
        {
            float remaining = stats.Damage - totalDamageApplied;
            float dps = Mathf.Max(0f, stats.Damage * damageFractionPerSecond);
            float damageThisTick = Mathf.Min(dps, remaining);

            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(damageThisTick));
            float before = burningTarget.Health.GetCurrentHealth();
            burningTarget.Health.TakeDamage(dmgInt);
            float after = burningTarget.Health.GetCurrentHealth();

            float realApplied = Mathf.Max(0f, before - after);
            totalDamageApplied += realApplied;

            if (vfxOnEachTick && impactVfxPrefab)
            {
                // Instanciamos en el centro del enemigo (sin normal de impacto).
                Vector3 pos = burningTarget.transform.position + Vector3.up * 0.35f;
                GameObject fx = Instantiate(impactVfxPrefab, pos, Quaternion.identity, parentVfxToHit ? burningTarget.transform : null);
                if (impactVfxLifetime > 0f) Destroy(fx, impactVfxLifetime);

            }
        }
        else
        {
            burningTarget = null;
            totalDamageApplied = 0f;
        }
    }

    private void SpawnImpactVfx(RaycastHit hit, Vector3 fallbackDir)
    {
        if (!impactVfxPrefab) return;

        Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal : -fallbackDir;
        Vector3 pos = hit.point + normal * impactVfxOffsetAlongNormal;
        Quaternion rot = Quaternion.LookRotation(normal);
        Transform parent = parentVfxToHit ? hit.collider.transform : null;

        GameObject fx = Instantiate(impactVfxPrefab, pos, rot, parent);
        if (impactVfxLifetime > 0f) Destroy(fx, impactVfxLifetime);
        _audioSource.PlayOneShot(impactClip, impactVolume);

    }

    private static bool TryGetEnemy(Transform t, out Enemy enemy)
    {
        enemy = null;
        if (!t) return false;

        enemy = t.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.Health != null) return true;

        var health = t.GetComponentInParent<EnemyHealth>();
        if (health != null)
        {
            enemy = health.GetComponentInParent<Enemy>();
            return enemy != null && enemy.Health != null;
        }
        return false;
    }
}
