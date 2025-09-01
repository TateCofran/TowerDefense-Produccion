using System.Collections.Generic;
using UnityEngine;

public class TurretCostManager : MonoBehaviour
{
    public static TurretCostManager Instance;

    [SerializeField] private float costMultiplier = 1.25f;

    private Dictionary<string, int> currentCosts = new Dictionary<string, int>();
    private Dictionary<string, int> baseCosts = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCostsFromTurretDatabase(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeCostsFromTurretDatabase()
    {
        if (TurretDatabase.Instance == null)
        {
            Debug.LogError("TurretDatabase no está disponible. Asegurate que se inicialice antes que TurretCostManager.");
            return;
        }

        foreach (var data in TurretDatabase.Instance.allTurrets)
        {
            baseCosts[data.id] = data.cost;
            currentCosts[data.id] = data.cost;
            // Debug.Log($"[TurretCostManager] Costo base para {data.id}: {data.cost}");
        }
    }

    public int GetUpgradeCost(string turretId, int currentUpgradeLevel)
    {
        int baseCost = 15;
        float multiplier = 1.3f;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(multiplier, currentUpgradeLevel - 1));
    }
    public int GetCurrentCost(string turretId)
    {
        if (currentCosts.TryGetValue(turretId, out int cost))
        {
            float globalMultiplier = GameModifiersManager.Instance != null
                ? GameModifiersManager.Instance.turretCostMultiplier
                : 1f;

            return Mathf.RoundToInt(cost * globalMultiplier);
        }

        Debug.LogWarning($"[TurretCostManager] No se encontró el costo actual para: {turretId}");
        return 9999;
    }

    public void OnTurretPlaced(string turretId)
    {
        if (!currentCosts.ContainsKey(turretId)) return;

        int oldCost = currentCosts[turretId];
        int newCost = Mathf.CeilToInt(oldCost * costMultiplier);
        currentCosts[turretId] = newCost;

        //Debug.Log($"[TurretCostManager] Costo actualizado de {turretId}: {oldCost} to {newCost}");
    }
    public void OnTurretRemoved(string turretId)
    {
        if (!currentCosts.ContainsKey(turretId) || !baseCosts.ContainsKey(turretId)) return;

        int current = currentCosts[turretId];
        int lowered = Mathf.FloorToInt(current / costMultiplier);

        // Nunca bajar más que el costo base
        currentCosts[turretId] = Mathf.Max(lowered, baseCosts[turretId]);

        //Debug.Log($"[TurretCostManager] Costo reducido de {turretId}: {current} to {currentCosts[turretId]}");
    }

    public int GetBaseCost(string turretId)
    {
        int baseCost = baseCosts.TryGetValue(turretId, out int c) ? c : 0;

        float globalMultiplier = GameModifiersManager.Instance != null
            ? GameModifiersManager.Instance.turretCostMultiplier
            : 1f;

        return Mathf.RoundToInt(baseCost * globalMultiplier);
    }

    public int CurrentCostsCount()
    {
        return currentCosts.Count;
    }

}
