using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeData", menuName = "Upgrades/Upgrade Data", order = 1)]
public class UpgradeData : ScriptableObject
{
    public string upgradeId;
    public string upgradeName;
    [TextArea]
    public string description;
    public UpgradeCategory category;
    public int xpCost;

    public Sprite icon; // Ícono de la mejora

    [Range(1, 3)]
    public int tier = 1; // Tier o nivel de la mejora (1 a 3)
}
