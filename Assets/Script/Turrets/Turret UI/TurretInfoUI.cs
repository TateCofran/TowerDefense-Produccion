using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(UIPanelController))]
public class TurretInfoUI : MonoBehaviour
{
    public static TurretInfoUI Instance;

    [Header("UI References")]
    public TMP_Text turretNameText;
    public TMP_Text damageText;
    public TMP_Text rangeText;
    public TMP_Text fireRateText;
    //public TMP_Text specialInfoText;

    public Button sellButton;
    public Button upgradeButton;
    public TMP_Text sellButtonText;
    public TMP_Text upgradeButtonText;

    public Button changeTargetModeButton;
    public TMP_Text changeTargetModeButtonText;

    private Turret currentTurret;

    private TurretDataHolder dataHolder;
    private TurretStats stats;
    private TurretTargeting targeting;

    private int currentUpgradeCost;

    private UIPanelController panelController;
    private TurretEconomyManager economyManager;
    private TurretUpgradeManager upgradeManager;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        panelController = GetComponent<UIPanelController>();
        economyManager = GetComponent<TurretEconomyManager>();
        upgradeManager = GetComponent<TurretUpgradeManager>();

        sellButton.onClick.AddListener(SellTurret);
        upgradeButton.onClick.AddListener(UpgradeTurret);
        changeTargetModeButton.onClick.AddListener(ChangeTargetingMode);

        panelController.Hide();
    }

    public void Initialize(Turret turret)
    {
        currentTurret = turret;

        dataHolder = turret.GetComponent<TurretDataHolder>();
        stats = turret.GetComponent<TurretStats>();
        stats.OnStatsChanged += UpdateInfo;

        if (dataHolder == null || stats == null)
        {
            Debug.LogError("Faltan componentes requeridos en la torreta.");
            return;
        }

        var type = dataHolder.turretData?.type;
        bool isSupport = type == "support";

        if (!isSupport)
        {
            targeting = turret.GetComponent<TurretTargeting>();
            if (targeting == null)
            {
                Debug.LogError("Falta TurretTargeting en torreta no-support.");
                return;
            }
        }

        currentUpgradeCost = economyManager.CalculateUpgradeCostForLevel(stats.UpgradeLevel);

        UpdateInfo();

        if (!isSupport)
            UpdateTargetModeText(targeting.mode);
    }


    public void UpdateInfo()
    {
        if (currentTurret == null) return;

        bool canUpgrade = upgradeManager.CanUpgradeTurret(stats);
        int maxAllowedLevel = upgradeManager.GetMaxAllowedLevel();

        var turretData = dataHolder.turretData;
        turretNameText.text = $"{turretData.name} (Nivel {stats.UpgradeLevel}/{maxAllowedLevel})";

        if (turretData.type == "support")
        {
            int totalGoldPerWave = turretData.goldPerWave * stats.UpgradeLevel;
            damageText.text = $"{totalGoldPerWave} Gold per wave";
            rangeText.text = "-";
            fireRateText.text = "-";
            changeTargetModeButton.gameObject.SetActive(false);
        }
        else
        {
            damageText.text = $"{stats.Damage}";
            fireRateText.text = $"{stats.FireRate}";
            rangeText.text = $"{stats.Range}";

            //specialInfoText.gameObject.SetActive(false);
            changeTargetModeButton.gameObject.SetActive(true);
        }


        sellButtonText.text = $"Sell ({economyManager.CalculateSellValue(stats, dataHolder)} Gold)";

        if (!canUpgrade)
        {
            upgradeButtonText.text = "Upgrade core to more lvl";
            upgradeButton.interactable = false;
        }
        else
        {
            upgradeButtonText.text = $"Mejorar ({currentUpgradeCost} oro)";
            upgradeButton.interactable = true;
        }
    }

    public void Hide() => panelController.Hide();
    public void Show() => panelController.Show();

    private void SellTurret()
    {
        int refund = economyManager.CalculateSellValue(stats, dataHolder);
        currentTurret.GetComponent<TurretEconomy>().Sell();
        Hide();
    }

    private void UpgradeTurret()
    {
        if (!upgradeManager.CanUpgradeTurret(stats))
        {
            Debug.Log("Nivel máximo alcanzado o limitado por núcleo.");
            return;
        }

        if (economyManager.TrySpendGold(currentUpgradeCost))
        {
            upgradeManager.UpgradeTurret(stats);
            currentUpgradeCost = economyManager.CalculateNextUpgradeCost(currentUpgradeCost);
            UpdateInfo();
            Debug.Log($"Torreta mejorada a nivel {stats.UpgradeLevel}. Próxima mejora: {currentUpgradeCost} oro.");
        }
        else
        {
            Debug.Log("No hay suficiente oro para mejorar.");
        }
    }

    public void UpdateTargetModeText(TurretTargeting.TargetingMode mode)
    {
        switch (mode)
        {
            case TurretTargeting.TargetingMode.Closest:
                changeTargetModeButtonText.text = "Close Enemy"; break;
            case TurretTargeting.TargetingMode.Farthest:
                changeTargetModeButtonText.text = "Far Enemy"; break;
            case TurretTargeting.TargetingMode.HighestHealth:
                changeTargetModeButtonText.text = "More health"; break;
            case TurretTargeting.TargetingMode.LowestHealth:
                changeTargetModeButtonText.text = "Less Health"; break;
        }
    }

    void ChangeTargetingMode()
    {
        targeting.NextMode(); // Asegurate de tener este método en TurretTargeting
        UpdateTargetModeText(targeting.mode);
    }
}
