using System.Collections.Generic;
using UnityEngine;

public class GoldRewardSystem : MonoBehaviour
{
    private List<GoldTurretIncome> goldTurrets = new();
    [SerializeField] private int goldPerWave = 15;

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveEnded += OnWaveCompleted;
        }
    }

    public void Register(GoldTurretIncome turret)
    {
        if (!goldTurrets.Contains(turret))
        {
            goldTurrets.Add(turret);
        }
    }

    private void OnWaveCompleted()
    {
        // Obtener el multiplicador global de oro de los modificadores
        float multiplier = GameModifiersManager.Instance != null
            ? GameModifiersManager.Instance.goldPerWaveMultiplier
            : 1f;

        int finalGold = Mathf.RoundToInt(goldPerWave * multiplier);

        GoldManager.Instance.AddGold(finalGold);
        Debug.Log($"[GoldRewardSystem] Otorgado {finalGold} de oro al completar la oleada. (Base: {goldPerWave}, Mult x{multiplier})");

        // También otorgar oro por torretas
        foreach (var turret in goldTurrets)
        {
            turret.GiveGold();
        }
    }

}
