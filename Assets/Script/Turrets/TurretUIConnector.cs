using UnityEngine;

public class TurretUIConnector : MonoBehaviour
{
    private Turret turret;
    private TurretUpgrade upgrade;
    private TurretEconomy economy;
    private TurretDataHolder dataHolder;


    void Awake()
    {
        turret = GetComponent<Turret>();
        upgrade = GetComponent<TurretUpgrade>();
        economy = GetComponent<TurretEconomy>();

        dataHolder = GetComponent<TurretDataHolder>();
        if (dataHolder == null)
            Debug.LogError("Falta el componente TurretDataHolder en " + gameObject.name);
    }

public void ShowUI()
    {
        TurretInfoUI.Instance.Initialize(turret);
        TurretInfoUI.Instance.Show();
    }

    public void HideUI()
    {
        TurretInfoUI.Instance.Hide();
    }

    public void Upgrade()
    {
        if (upgrade.CanUpgrade())
        {
            int cost = TurretCostManager.Instance.GetUpgradeCost(dataHolder.turretData.id, upgrade.GetUpgradeLevel());
            if (GoldManager.Instance.HasEnoughGold(cost))
            {
                GoldManager.Instance.SpendGold(cost);
                upgrade.Upgrade();
                TurretInfoUI.Instance.UpdateInfo();
            }
            else
            {
                Debug.Log("No hay suficiente oro.");
            }
        }
    }

    public void Sell()
    {
        economy.Sell();
        HideUI();
    }
}
