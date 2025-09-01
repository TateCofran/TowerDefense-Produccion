using UnityEngine;
using System.Collections.Generic;

public class TurretUpgrade : MonoBehaviour, IUpgradeable
{
    [SerializeField] private int maxUpgradeLevel = 15;
    [SerializeField] private List<TurretUpgradeStep> upgradeSteps;

    private TurretStats stats;

    void Awake()
    {
        stats = GetComponent<TurretStats>();
    }

    public bool CanUpgrade()
    {
        return stats.UpgradeLevel < maxUpgradeLevel;
    }

    public void Upgrade()
    {
        if (!CanUpgrade()) return;

        int nextLevel = stats.UpgradeLevel + 1;
        TurretUpgradeStep step = upgradeSteps.Find(s => s.level == nextLevel);

        if (step != null)
        {
            stats.ApplyUpgradeStep(step);
        }
        else
        {
            Debug.LogWarning($"No upgrade step configured for level {nextLevel}");
        }
    }
    public TurretUpgradeStep GetUpgradeStepForLevel(int level)
    {
        return upgradeSteps.Find(step => step.level == level);
    }

    public int GetUpgradeLevel() => stats.UpgradeLevel;
    public int GetMaxUpgradeLevel() => maxUpgradeLevel;
}
