using UnityEngine;

public class TurretStats : MonoBehaviour, ITurretStats
{
    [Header("Stats")]
    [SerializeField] private float range;
    [SerializeField] private float fireRate;
    [SerializeField] private float damage;
    [SerializeField] private int upgradeLevel = 1;
    [SerializeField] private int maxUpgradeLevel = 15;

    private float baseDamage;
    private float baseRange;
    private float baseFireRate;

    [Header("Actual Stats")]
    [SerializeField] private float currentDamage;
    [SerializeField] private float currentRange;
    [SerializeField] private float currentFireRate;


    public float Damage => currentDamage;
    public float Range => currentRange;
    public float FireRate => currentFireRate;
    public int UpgradeLevel => upgradeLevel;
    public int MaxUpgradeLevel => maxUpgradeLevel;

    private IRangeDisplay rangeDisplay;
    private TurretDataHolder dataHolder;

    public event System.Action OnStatsChanged;

    void Awake()
    {
        rangeDisplay = GetComponent<IRangeDisplay>();
        dataHolder = GetComponent<TurretDataHolder>();

        CorruptionManager.Instance.OnCorruptionLevelChanged += ApplyCorruptionPenalty;
    }

    private void OnDestroy()
    {
        if (CorruptionManager.Instance != null)
            CorruptionManager.Instance.OnCorruptionLevelChanged -= ApplyCorruptionPenalty;
    }

    public void InitializeFromData(TurretData data)
    {
        float mod = Application.isPlaying ? GameModifiersManager.Instance.turretDamageMultiplier : 1f;

        baseDamage = Mathf.Round(data.damage * mod * 10f) / 10f;
        baseRange = Mathf.Round(data.range * 10f) / 10f;
        baseFireRate = Mathf.Round(data.fireRate * 10f) / 10f;

        RecalculateStats();
        UpdateRangeVisualizer();
    }
    private void ApplyCorruptionPenalty(CorruptionManager.CorruptionLevel level)
    {
        RecalculateStats(level);
        UpdateRangeVisualizer();
    }
    public void ApplyUpgradeStep(TurretUpgradeStep step)
    {
        baseDamage = Mathf.Round(baseDamage * step.damageMultiplier * 10f) / 10f;
        baseRange = Mathf.Round(baseRange * step.rangeMultiplier * 10f) / 10f;
        baseFireRate = Mathf.Round(baseFireRate * step.fireRateMultiplier * 10f) / 10f;

        upgradeLevel++;
        RecalculateStats();
        UpdateRangeVisualizer();
    }

    public void RecalculateStats(CorruptionManager.CorruptionLevel level = CorruptionManager.CorruptionLevel.None)
    {
        float rangeMod = 1f;
        float fireRateMod = 1f;
        float damageMod = 1f;

        if (dataHolder != null && dataHolder.turretData != null)
        {
            var turretWorld = dataHolder.turretData.GetAllowedWorld();
            var currentWorld = WorldManager.Instance.CurrentWorld;

            if (turretWorld == AllowedWorld.Normal)
            {
                if (currentWorld == WorldState.Normal)
                {
                    rangeMod *= 1.05f; // +5% rango
                }
                else
                {
                    fireRateMod *= 0.95f; // -5% fire rate
                }
            }
            else if (turretWorld == AllowedWorld.Other)
            {
                if (currentWorld == WorldState.OtherWorld)
                {
                    damageMod *= 1.05f; // +5% daño
                }
                else
                {
                    rangeMod *= 0.95f; // -5% rango
                }
            }
        }

        // Aplica los multiplicadores globales de modificadores (si existen)
        if (GameModifiersManager.Instance != null)
        {
            rangeMod *= GameModifiersManager.Instance.turretRangeMultiplier;
            fireRateMod *= GameModifiersManager.Instance.turretFireRateMultiplier;
            damageMod *= GameModifiersManager.Instance.turretDamageMultiplier;
        }

        // aplicar el penalizador SOLO al daño, usando el multiplicador centralizado ---
        float corruptionMultiplier = 1f;
        if (CorruptionManager.Instance != null)
            corruptionMultiplier = CorruptionManager.Instance.GetTurretDamageMultiplier();

        currentDamage = Mathf.Round(baseDamage * damageMod * corruptionMultiplier * 10f) / 10f;
        currentRange = Mathf.Round(baseRange * rangeMod * 10f) / 10f;
        currentFireRate = Mathf.Round(baseFireRate * fireRateMod * 10f) / 10f;

        OnStatsChanged?.Invoke();
    }

    private void UpdateRangeVisualizer()
    {
        if (rangeDisplay != null && rangeDisplay.IsVisible()) 
            rangeDisplay.Show(currentRange);
    }

}
