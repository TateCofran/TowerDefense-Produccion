using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("All available upgrades")]
    [SerializeField] private List<UpgradeData> allUpgrades;

    public event Action<UpgradeData> OnUpgradeUnlocked;

    private HashSet<string> unlockedUpgrades = new HashSet<string>();
    private const string PlayerPrefsKey = "UnlockedUpgrades";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUnlockedUpgrades();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IReadOnlyList<UpgradeData> GetUpgrades() => allUpgrades;

    public bool IsUnlocked(string upgradeId) => unlockedUpgrades.Contains(upgradeId);

    public bool TryUnlockUpgrade(string upgradeId)
    {
        var upgrade = allUpgrades.Find(u => u.upgradeId == upgradeId);
        if (upgrade == null || IsUnlocked(upgradeId)) return false;

        bool canUnlock = false;

        switch (upgrade.category)
        {
            case UpgradeCategory.NormalWorld:
                canUnlock = PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal) >= upgrade.xpCost;
                break;
            case UpgradeCategory.OtherWorld:
                canUnlock = PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld) >= upgrade.xpCost;
                break;
            case UpgradeCategory.General:
                int half = Mathf.CeilToInt(upgrade.xpCost / 2f);
                canUnlock =
                    PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal) >= half &&
                    PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld) >= half;
                break;
        }

        if (!canUnlock)
            return false;

        // Descontar esencia solo si se puede desbloquear
        switch (upgrade.category)
        {
            case UpgradeCategory.NormalWorld:
                PlayerExperienceManager.Instance.RemoveEssence(WorldState.Normal, upgrade.xpCost);
                break;
            case UpgradeCategory.OtherWorld:
                PlayerExperienceManager.Instance.RemoveEssence(WorldState.OtherWorld, upgrade.xpCost);
                break;
            case UpgradeCategory.General:
                PlayerExperienceManager.Instance.RemoveEssenceBoth(upgrade.xpCost);
                break;
        }

        unlockedUpgrades.Add(upgradeId);
        SaveUnlockedUpgrades();

        OnUpgradeUnlocked?.Invoke(upgrade);
        return true;
    }

    /// Guarda upgrades desbloqueadas en PlayerPrefs como string separado por comas.
    private void SaveUnlockedUpgrades()
    {
        string unlocked = string.Join(",", unlockedUpgrades);
        PlayerPrefs.SetString(PlayerPrefsKey, unlocked);
        PlayerPrefs.Save();
    }
    // Carga upgrades desbloqueadas desde PlayerPrefs.

    private void LoadUnlockedUpgrades()
    {
        unlockedUpgrades.Clear();
        string unlocked = PlayerPrefs.GetString(PlayerPrefsKey, "");
        if (!string.IsNullOrEmpty(unlocked))
        {
            string[] upgrades = unlocked.Split(',');
            foreach (string upg in upgrades)
                if (!string.IsNullOrWhiteSpace(upg))
                    unlockedUpgrades.Add(upg);
        }
    }


    // Devuelve true si alguna vez se desbloqueó al menos una mejora.
    public bool HasAnyUpgradeUnlocked() => unlockedUpgrades.Count > 0;

#if UNITY_EDITOR
    [ContextMenu("Reset Upgrades")]
    public void ResetUpgrades()
    {
        PlayerPrefs.DeleteKey("UnlockedUpgrades");
        PlayerPrefs.Save();

        Debug.Log("[UpgradeManager] Todas las mejoras han sido reseteadas.");
        // Si querés también actualizar la UI automáticamente:
        UpgradesPanelUI panel = FindFirstObjectByType<UpgradesPanelUI>();
        if (panel != null)
            panel.RefreshUI();
    }
#endif

}
