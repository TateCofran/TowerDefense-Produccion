using UnityEngine;
using TMPro;

[RequireComponent(typeof(UIPanelController))]
public class TurretTooltipUI : MonoBehaviour
{
    public static TurretTooltipUI Instance;

    [Header("UI References")]
    public TMP_Text tooltipText;

    private string currentTurretId;
    private UIPanelController panelController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        panelController = GetComponent<UIPanelController>();
        panelController.Hide();
    }

    public void Show(string turretId)
    {
        currentTurretId = turretId;

        var data = TurretDatabase.Instance.GetTurretData(turretId);
        if (data == null) return;

        int cost = TurretCostManager.Instance.GetCurrentCost(turretId);

        string text = $"{data.name}\nCosto: {cost} oro";

        if (data.type == "attack" || data.type == "aoe" || data.type == "slow")
        {
            float mod = GameModifiersManager.Instance.turretDamageMultiplier;
            float finalDamage = Mathf.Round(data.damage * mod * 100f) / 100f;

            string final = finalDamage.ToString("F1");
            text += $"\nDaño: {data.damage} (+{(mod - 1f) * 100f}% to {final})";
        }

        if (data.type == "support")
            text += $"\nOro por oleada: {data.goldPerWave}";

        tooltipText.text = text;
        panelController.Show();
    }

    public void RefreshIfVisible(string turretId)
    {
        if (currentTurretId != turretId || !tooltipText.gameObject.activeInHierarchy)
            return;

        Show(turretId);
    }

    public void Hide()
    {
        panelController.Hide();
    }
}
