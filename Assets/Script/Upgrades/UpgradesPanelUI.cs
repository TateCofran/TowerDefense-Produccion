using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class UpgradesPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform generalUpgradesContainer;
    public Transform normalWorldUpgradesContainer;
    public Transform otherWorldUpgradesContainer;
    public UpgradeUIItem upgradeUIPrefab;
    public TMP_Text normalEssenceText;
    public TMP_Text otherWorldEssenceText;

    private void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        int normalEssence = PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal);
        int otherWorldEssence = PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld);

        normalEssenceText.text = $"{normalEssence}";
        otherWorldEssenceText.text = $"{otherWorldEssence}";

        // Limpiá contenedores
        foreach (Transform child in generalUpgradesContainer) Destroy(child.gameObject);
        foreach (Transform child in normalWorldUpgradesContainer) Destroy(child.gameObject);
        foreach (Transform child in otherWorldUpgradesContainer) Destroy(child.gameObject);

        List<UpgradeData> upgrades = new List<UpgradeData>(UpgradeManager.Instance.GetUpgrades());
        foreach (var upgrade in upgrades)
        {
            bool unlocked = UpgradeManager.Instance.IsUnlocked(upgrade.upgradeId);
            UpgradeUIItem uiItem = Instantiate(upgradeUIPrefab, GetContainer(upgrade.category));

            // Decidí cuál essence mostrarle según la categoría
            int playerEssence = upgrade.category switch
            {
                UpgradeCategory.General => Mathf.Min(normalEssence, otherWorldEssence), // O ambas, o lo que prefieras
                UpgradeCategory.NormalWorld => normalEssence,
                UpgradeCategory.OtherWorld => otherWorldEssence,
                _ => 0,
            };
            uiItem.Setup(upgrade, unlocked, OnUnlockUpgrade);
        }
    }


    private Transform GetContainer(UpgradeCategory category)
    {
        return category switch
        {
            UpgradeCategory.General => generalUpgradesContainer,
            UpgradeCategory.NormalWorld => normalWorldUpgradesContainer,
            UpgradeCategory.OtherWorld => otherWorldUpgradesContainer,
            _ => generalUpgradesContainer,
        };
    }

    private void OnUnlockUpgrade(string upgradeId)
    {
        bool unlocked = UpgradeManager.Instance.TryUnlockUpgrade(upgradeId);
        if (unlocked)
            RefreshUI();
    }


}
