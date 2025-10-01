using UnityEngine;
using System.Linq;

[RequireComponent(typeof(TurretStats))]
public class TeslaTurret : MonoBehaviour, IShootingBehavior
{
    [Header("Refs")]
    [SerializeField] private Transform firePoint;

    [Header("Raycast")]
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private LayerMask losBlockers = 0;
    [SerializeField] private float sphereCastRadius = 0.18f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.35f, 0f);
    [SerializeField] private bool debugDraw = false;

    [Header("Impact VFX")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float impactVfxOffsetAlongNormal = 0.03f;
    [SerializeField] private bool parentVfxToHit = true;
    [SerializeField] private float impactVfxLifetime = 1.5f;

    private ITurretStats stats;

    private void Awake()
    {
        stats = GetComponent<ITurretStats>();
        if (!firePoint) Debug.LogWarning("[TeslaTurret] Falta asignar firePoint.");
    }

    public void Shoot(Transform firePointIgnored, Transform targetIgnored, ITurretStats statsParam)
    {
        if (stats == null || !firePoint) return;

        var enemiesInRange = EnemyTracker.GetActiveEnemies()
            .Where(e => e != null && e.Health != null && !e.Health.IsDead())
            .Where(e => Vector3.Distance(transform.position, e.transform.position) <= stats.Range)
            .ToArray();

        int hits = 0;

        foreach (var enemy in enemiesInRange)
        {
            Vector3 hitPos = enemy.transform.position + targetOffset;
            Vector3 dir = (hitPos - firePoint.position);
            float dist = dir.magnitude;
            if (dist <= 0.001f) continue;
            Vector3 ndir = dir / dist;

            // LOS opcional
            if (losBlockers != 0 &&
                Physics.Raycast(firePoint.position, ndir, dist, losBlockers, QueryTriggerInteraction.Ignore))
            {
                if (debugDraw) Debug.DrawLine(firePoint.position, hitPos, Color.gray, 0.1f);
                continue;
            }

            // Raycast fino
            if (Physics.Raycast(firePoint.position, ndir, out RaycastHit hit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
            {
                SpawnImpactVfx(hit, ndir);
                ApplyDamage(hit.collider.transform, Mathf.Max(1, (int)stats.Damage));
                if (debugDraw) Debug.DrawLine(firePoint.position, hit.point, Color.cyan, 0.1f);
                hits++;
                continue;
            }

            // SphereCast fallback
            if (sphereCastRadius > 0f &&
                Physics.SphereCast(firePoint.position, sphereCastRadius, ndir, out RaycastHit shit, dist + 0.25f, hittableLayers, QueryTriggerInteraction.Ignore))
            {
                SpawnImpactVfx(shit, ndir);
                ApplyDamage(shit.collider.transform, Mathf.Max(1, (int)stats.Damage));
                if (debugDraw) Debug.DrawLine(firePoint.position, shit.point, Color.yellow, 0.1f);
                hits++;
                continue;
            }

            if (debugDraw) Debug.DrawLine(firePoint.position, firePoint.position + ndir * Mathf.Min(dist, 3f), Color.red, 0.1f);
        }

        if (debugDraw) Debug.Log($"[TeslaTurret] Raycast hits: {hits}");
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
    }

    private static void ApplyDamage(Transform hitTf, int damage)
    {
        if (!hitTf) return;

        var h = hitTf.GetComponentInParent<EnemyHealth>();
        if (h != null) { h.TakeDamage(damage); return; }

        var dmg = hitTf.GetComponentInParent<IDamageable>();
        if (dmg != null) { dmg.TakeDamage(damage); return; }

        var e = hitTf.GetComponentInParent<Enemy>();
        if (e != null && e.Health != null) { e.Health.TakeDamage(damage); }
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
