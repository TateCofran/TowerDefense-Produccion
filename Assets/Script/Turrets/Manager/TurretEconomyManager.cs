using UnityEngine;

public class TurretEconomyManager : MonoBehaviour
{
    [SerializeField] private int baseUpgradeCost = 15;
    [SerializeField] private float upgradeMultiplier = 1.3f;
    [SerializeField, Range(0f, 1f)] private float refundPercentage = 0.6f;

    public int GetInitialUpgradeCost()
    {
        return baseUpgradeCost;
    }
    public int CalculateUpgradeCostForLevel(int level)
    {
        int cost = baseUpgradeCost;
        for (int i = 1; i < level; i++)
        {
            cost = CalculateNextUpgradeCost(cost);
        }
        return cost;
    }

    public int CalculateNextUpgradeCost(int currentCost)
    {
        return Mathf.RoundToInt(currentCost * upgradeMultiplier);
    }

    public int CalculateSellValue(TurretStats stats, TurretDataHolder dataHolder)
    {
        int baseCost = TurretCostManager.Instance.GetBaseCost(dataHolder.turretData.id);

        int totalUpgradeCost = 0;
        for (int i = 1; i < stats.UpgradeLevel; i++)
        {
            totalUpgradeCost += TurretCostManager.Instance.GetUpgradeCost(dataHolder.turretData.id, i);
        }

        int totalInvested = baseCost + totalUpgradeCost;
        return Mathf.RoundToInt(totalInvested * refundPercentage);
    }



    public bool TrySpendGold(int amount)
    {
        if (GoldManager.Instance.HasEnoughGold(amount))
        {
            GoldManager.Instance.SpendGold(amount);
            return true;
        }
        return false;
    }

    public void RefundGold(int amount)
    {
        GoldManager.Instance.AddGold(amount);
    }
}
