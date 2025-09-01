using UnityEngine;

public class Turret : MonoBehaviour
{
    [SerializeField] private TurretStats stats;
    public TurretStats Stats => stats;

    private TurretDataHolder dataHolder;
    private IShootingBehavior shooter;
    private ITargetingBehavior targeting;
    private IRangeDisplay rangeDisplay;

    void Awake()
    {
        stats = GetComponent<TurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();
        rangeDisplay = GetComponent<IRangeDisplay>();

        if (dataHolder != null && dataHolder.turretData != null)
        {
            string type = dataHolder.turretData.type;

            if (type == "attack" || type == "aoe" || type == "slow")
            {
                shooter = GetComponent<IShootingBehavior>();
                targeting = GetComponent<ITargetingBehavior>();

                if (shooter == null)
                    Debug.LogWarning($"[Turret] {gameObject.name} no tiene IShootingBehavior. No podrá disparar.");

                if (targeting == null)
                    Debug.LogWarning($"[Turret] {gameObject.name} no tiene ITargetingBehavior. No podrá detectar enemigos.");
            }
        }
    }
    private void Start()
    {
        HideRange();
    }
    public void ShowRange()
    {
        rangeDisplay?.Show(stats.Range);
    }


    public void HideRange()
    {
        rangeDisplay?.Hide();
    }

    private void OnMouseDown()
    {
        TurretSelector.Instance.SelectTurret(this);
    }
}
