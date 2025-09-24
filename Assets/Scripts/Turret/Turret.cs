using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class Turret : MonoBehaviour
{
    [SerializeField] private TurretStats stats;
    public TurretStats Stats => stats;

    private TurretDataHolder dataHolder;
    private IShootingBehavior shooter;
    private ITargetingBehavior targeting;
    private IRangeDisplay rangeDisplay;

    [Header("Range Display")]
    [SerializeField] private bool showRangeOnStart = true;
    void Awake()
    {
        stats = GetComponent<TurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();
        rangeDisplay = GetComponent<IRangeDisplay>();

        // Siempre intentamos resolver comportamientos
        shooter = GetComponent<IShootingBehavior>();
        targeting = GetComponent<ITargetingBehavior>();

        // Tipo tomado desde SO (ya no usamos turretData)
        string type = (dataHolder != null && dataHolder.turretDataSO != null)
            ? dataHolder.turretDataSO.type
            : null;

        if (RequiresCombatBehaviors(type))
        {
            if (shooter == null)
                Debug.LogWarning($"[Turret] {name} no tiene IShootingBehavior. No podrá disparar.");

            if (targeting == null)
                Debug.LogWarning($"[Turret] {name} no tiene ITargetingBehavior. No podrá detectar enemigos.");
        }
    }

    private void Start()
    {
        if (showRangeOnStart) ShowRange();
        else HideRange();
    }

    public void ShowRange() => rangeDisplay?.Show(stats.Range);
    public void HideRange() => rangeDisplay?.Hide();

    // Si no hay tipo, asumimos que requiere combate (conservador)
    private static bool RequiresCombatBehaviors(string type)
    {
        if (string.IsNullOrEmpty(type)) return true;
        type = type.ToLowerInvariant();
        return type == "attack" || type == "aoe" || type == "slow";
    }
}
