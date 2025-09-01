using UnityEngine;

[RequireComponent(typeof(TurretStats))]
[RequireComponent(typeof(TurretDataHolder))]
public class TurretEconomy : MonoBehaviour, ISellable
{
    [SerializeField] private float refundPercentage = 0.6f;

    private TurretStats stats;
    private TurretDataHolder dataHolder;

    void Awake()
    {
        stats = GetComponent<TurretStats>();
        dataHolder = GetComponent<TurretDataHolder>();
    }

    public int GetSellValue()
    {
        if (dataHolder == null || dataHolder.turretData == null)
        {
            Debug.LogError("[TurretEconomy] No se encontró el TurretDataHolder o turretData.");
            return 0;
        }

        return Mathf.RoundToInt(dataHolder.turretData.cost * refundPercentage);
    }

    public void Sell()
    {
        if (dataHolder == null || dataHolder.turretData == null)
        {
            Debug.LogError("[TurretEconomy] No se puede vender: falta turretData.");
            return;
        }

        int refund = GetSellValue();
        GoldManager.Instance.AddGold(refund);

        var cellLink = GetComponent<TurretCellLink>();
        if (cellLink != null)
        {
            Debug.Log("[TurretEconomy] Llamando a ReleaseCell()");
            cellLink.ReleaseCell(dataHolder.turretData.id);
        }

        TurretCostManager.Instance.OnTurretRemoved(dataHolder.turretData.id);
        Destroy(gameObject);
    }
}
