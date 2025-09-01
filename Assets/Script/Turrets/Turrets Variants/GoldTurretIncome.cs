using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class GoldTurretIncome : MonoBehaviour
{
    private bool registered = false;
    private TurretDataHolder dataHolder;
    private TurretStats stats;

    void Awake()
    {
        dataHolder = GetComponent<TurretDataHolder>();
        stats = GetComponent<TurretStats>(); 
    }

    void Start()
    {
        var rewardSystem = FindFirstObjectByType<GoldRewardSystem>();
        if (rewardSystem != null)
        {
            rewardSystem.Register(this);
            registered = true;
        }
        else
        {
            Debug.LogWarning("[GoldTurretIncome] No se encontró GoldRewardSystem en la escena.");
        }
    }
    public void GiveGold()
    {
        if (dataHolder != null && dataHolder.turretData != null && stats != null)
        {
            int baseGold = dataHolder.turretData.goldPerWave;
            int level = stats.UpgradeLevel;

            float multiplier = GameModifiersManager.Instance != null
                ? GameModifiersManager.Instance.goldPerWaveMultiplier
                : 1f;

            int totalGold = Mathf.RoundToInt(baseGold * level * multiplier); // escalar por nivel y modificadores

            GoldManager.Instance.AddGold(totalGold);

            Debug.Log($"[GoldTurret] Otorgó {totalGold} de oro. Nivel: {level}, Mult x{multiplier}");
        }
    }

}
