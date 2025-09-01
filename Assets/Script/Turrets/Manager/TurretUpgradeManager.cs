using UnityEngine;

public class TurretUpgradeManager : MonoBehaviour
{
    [SerializeField] private float statMultiplier = 1.25f;
    public static TurretUpgradeManager Instance { get; private set; }

    private Core core;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    void Start()
    {
        core = Core.Instance;
    }

    public bool CanUpgradeTurret(TurretStats stats)
    {
        return stats.UpgradeLevel < stats.MaxUpgradeLevel
            && stats.UpgradeLevel < Core.Instance.GetMaxTurretLevel();

    }
    public int GetMaxAllowedLevel() => Core.Instance != null ? Core.Instance.GetMaxTurretLevel() : 1;


    public void UpgradeTurret(TurretStats stats)
    {
        if (!CanUpgradeTurret(stats)) return;

        int nextLevel = stats.UpgradeLevel + 1;
        var upgradeComponent = stats.GetComponent<TurretUpgrade>();
        if (upgradeComponent == null)
        {
            Debug.LogWarning($"[TurretUpgradeManager] Falta componente TurretUpgrade en {stats.gameObject.name}");
            return;
        }

        var step = upgradeComponent.GetUpgradeStepForLevel(nextLevel);
        if (step != null)
        {
            stats.ApplyUpgradeStep(step);
        }
        else
        {
            Debug.LogWarning($"[TurretUpgradeManager] No se encontró mejora para nivel {nextLevel} en {stats.gameObject.name}");
        }
    }

    public void ForceUpgradeTurret(TurretStats stats, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (CanUpgradeTurret(stats))
                UpgradeTurret(stats);
            else
                break;
        }
    }

}
