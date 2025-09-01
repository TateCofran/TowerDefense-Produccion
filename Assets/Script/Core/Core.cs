using System;
using UnityEngine;

public class Core : MonoBehaviour
{
    [SerializeField] private int baseHealth = 10; // NUEVO, visible en inspector si querés
    public int maxHealth = 10; // solo para mostrar en inspector
    private int currentHealth;

    public int coreLevel = 1;
    public int maxCoreLevel = 3;

    public static Core Instance;

    public static event Action OnCoreDamaged;

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RecalculateHealth();
        CoreUI.Instance.UpdateUI();
    }

    private void OnEnable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeUnlocked += HandleUpgradeUnlocked;

        ApplyUnlockedUpgradesAtStart();
    }

    private void OnDisable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeUnlocked -= HandleUpgradeUnlocked;
    }

    private void HandleUpgradeUnlocked(UpgradeData upgrade)
    {
        if (upgrade.upgradeId == "core_health")
        {
            RecalculateHealth();
            CoreUI.Instance.UpdateUI();
        }
    }

    private void ApplyUnlockedUpgradesAtStart()
    {
        if (UpgradeManager.Instance == null) return;
        foreach (var upgrade in UpgradeManager.Instance.GetUpgrades())
        {
            if (UpgradeManager.Instance.IsUnlocked(upgrade.upgradeId))
                HandleUpgradeUnlocked(upgrade);
        }
    }
    private void RecalculateHealth()
    {
        int bonus = 0;
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsUnlocked("core_health"))
            bonus = 1;

        maxHealth = baseHealth + bonus;
        currentHealth = maxHealth;
        Debug.Log($"[Core] Nueva vida máxima: {maxHealth} (base {baseHealth} + bonus {bonus})");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Disparar animación delay de barra (antes de chequear game over)
        CoreUI.Instance.OnCoreDamaged();

        if (currentHealth <= 0)
        {
            GameManager.Instance.GameOver();
            return;
        }
    }

    public void UpgradeCore()
    {
        if (coreLevel >= maxCoreLevel) return;

        int cost = GetUpgradeCost();
        if (!GoldManager.Instance.HasEnoughGold(cost))
        {
            Debug.Log("No hay suficiente oro para mejorar el núcleo.");
            return;
        }

        GoldManager.Instance.SpendGold(cost);
        coreLevel++;

        Debug.Log($"Núcleo mejorado a nivel {coreLevel}");

        CoreUI.Instance.UpdateUI();
    }

    public int GetUpgradeCost()
    {
        return coreLevel switch
        {
            1 => 150,
            2 => 300,
            _ => 9999
        };
    }

    public int GetMaxTurretLevel()
    {
        return coreLevel switch
        {
            1 => 5,
            2 => 10,
            3 => 15,
            _ => 5
        };
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public int GetCoreLevel() => coreLevel;
    public bool IsMaxLevel() => coreLevel >= maxCoreLevel;


}
