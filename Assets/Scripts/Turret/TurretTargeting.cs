using System.Linq;
using UnityEngine;

public class TurretTargeting : MonoBehaviour, ITargetingBehavior
{
    public enum TargetingMode { Closest, Farthest, HighestHealth, LowestHealth }
    public TargetingMode mode = TargetingMode.Closest;

    private ITurretStats stats;
    private TurretShooter shooter;

    void Awake()
    {
        stats = GetComponent<ITurretStats>();
        shooter = GetComponent<TurretShooter>();
    }

    void Update()
    {
        float currentRange = stats.Range;
        Transform target = GetTarget(transform.position, currentRange);
        shooter.SetTarget(target);
    }

    public void NextMode()
    {
        mode = (TargetingMode)(((int)mode + 1) % System.Enum.GetValues(typeof(TargetingMode)).Length);
    }

    public Transform GetTarget(Vector3 turretPosition, float range)
    {

        var enemiesInRange = EnemyTracker.Enemies
            .Where(enemy => enemy != null &&
                            enemy.gameObject.activeInHierarchy &&
                            enemy.Health != null &&
                            !enemy.Health.IsDead()
                            &&
                            Vector3.Distance(turretPosition, enemy.transform.position) <= range)
            .ToList();

        if (enemiesInRange.Count == 0)
            return null;

        return mode switch
        {
            TargetingMode.Closest => enemiesInRange
                .OrderBy(x => Vector3.Distance(turretPosition, x.transform.position)).First().transform,

            TargetingMode.Farthest => enemiesInRange
                .OrderByDescending(x => Vector3.Distance(turretPosition, x.transform.position)).First().transform,

            TargetingMode.HighestHealth => enemiesInRange
                .OrderByDescending(x => x.Health.GetCurrentHealth()).First().transform,

            TargetingMode.LowestHealth => enemiesInRange
                .OrderBy(x => x.Health.GetCurrentHealth()).First().transform,

            _ => null
        };
    }

}
