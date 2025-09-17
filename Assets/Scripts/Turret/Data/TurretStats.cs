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
    }

    // === Inicialización desde JSON antiguo ===
    public void InitializeFromData(TurretDataSO data)
    {
        float mod = Application.isPlaying ? GameModifiersManager.Instance.turretDamageMultiplier : 1f;

        baseDamage = Mathf.Round(data.damage * mod * 10f) / 10f;
        baseRange = Mathf.Round(data.range * 10f) / 10f;
        baseFireRate = Mathf.Round(data.fireRate * 10f) / 10f;

        RecalculateStats();
        UpdateRangeVisualizer();
    }

    // === Inicialización desde ScriptableObject ===
    public void InitializeFromSO(TurretDataSO so)
    {
        if (so == null) return;

        float mod = Application.isPlaying ? GameModifiersManager.Instance.turretDamageMultiplier : 1f;

        baseDamage = Mathf.Round(so.damage * mod * 10f) / 10f;
        baseRange = Mathf.Round(so.range * 10f) / 10f;
        baseFireRate = Mathf.Round(so.fireRate * 10f) / 10f;
        //maxUpgradeLevel = so != null ? so.price : maxUpgradeLevel; // ejemplo, o podés mapear otro campo

        RecalculateStats();
        UpdateRangeVisualizer();
    }
    /*
    public void ApplyUpgradeStep(TurretUpgradeStep step)
    {
        baseDamage = Mathf.Round(baseDamage * step.damageMultiplier * 10f) / 10f;
        baseRange = Mathf.Round(baseRange * step.rangeMultiplier * 10f) / 10f;
        baseFireRate = Mathf.Round(baseFireRate * step.fireRateMultiplier * 10f) / 10f;

        upgradeLevel++;
        RecalculateStats();
        UpdateRangeVisualizer();
    }
    */
    public void RecalculateStats()
    {
        float rangeMod = 1f;
        float fireRateMod = 1f;
        float damageMod = 1f;


        if (GameModifiersManager.Instance != null)
        {
            rangeMod *= GameModifiersManager.Instance.turretRangeMultiplier;
            fireRateMod *= GameModifiersManager.Instance.turretFireRateMultiplier;
            damageMod *= GameModifiersManager.Instance.turretDamageMultiplier;
        }

        currentDamage = Mathf.Round(baseDamage * damageMod * 10f) / 10f;
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
