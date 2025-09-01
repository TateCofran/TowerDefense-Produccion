using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Receta de oleadas (SO)")]
    [SerializeField] private WaveRecipeList recipeList;
    public WaveRecipeList RecipeList => recipeList;


    [Header("Multiplicador global de cantidad de enemigos")]
    [Range(0.5f, 3f)]
    [SerializeField] private float enemyCountMultiplier = 1f;
    public float EnemyCountMultiplier => enemyCountMultiplier;

    [Header("Configuración")]
    [SerializeField] private int maxWaves = 45;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesThisWave = 0;
    private int consecutiveOtherWorldWaves = 0;
    private WorldState previousWorldState = WorldState.Normal;

    public bool WaveInProgress => enemiesAlive > 0;
    public bool IsFirstWave => isFirstWave;
    public bool IsLastWave() => currentWave >= maxWaves;

    private bool isFirstWave = true;
    private bool waveStarted = false;
    private WorldState waveStartWorld;

    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveEnded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
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
        // Si necesitás registrar lógica especial, hacelo acá
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
    public void TryStartFirstWave()
    {
        if (!waveStarted && currentWave == 0)
        {
            StartNextWave();
        }
    }

    public void StartNextWave()
    {
        if (waveStarted)
            return;

        waveStarted = true;
        if (CellInteraction.hoveredCell != null)
            CellInteraction.hoveredCell.RefreshPreview();

        currentWave++;

        waveStartWorld = WorldManager.Instance.CurrentWorld;

        CalculateWorldStreak();

        int totalEnemies = CalculateEnemiesThisWave();

        enemiesThisWave = totalEnemies;
        enemiesAlive = totalEnemies;

        Debug.Log($"[WaveManager] Oleada {currentWave} iniciada con {totalEnemies} enemigos.");
        OnWaveStarted?.Invoke(currentWave, totalEnemies);

        if (CorruptionManager.Instance.CoreLosesLifePerWave())
        {
            Core.Instance.TakeDamage(1);
        }

    }

    public void NotifyEnemyKilled()
    {
        enemiesAlive--;

        // Actualizá la UI de enemigos restantes
        WaveUIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        if (enemiesAlive <= 0)
        {
            Debug.Log($"[WaveManager] Oleada {currentWave} finalizada.");
            waveStarted = false;

            // Penalidad de corrupción nivel 3
            if (CorruptionManager.Instance != null && CorruptionManager.Instance.CurrentLevel == CorruptionManager.CorruptionLevel.Level3)
            {
                Debug.Log("[WaveManager] Penalidad de corrupción nivel 3 activa: daño automático al núcleo.");
                Core.Instance?.TakeDamage(1);
            }

            // Upgrades y recompensas por fin de oleada
            var upgradeMgr = UpgradeManager.Instance;

            // Oro pasivo (siempre al terminar una oleada)
            if (upgradeMgr != null && upgradeMgr.IsUnlocked("gold_pasive"))
                GoldManager.Instance?.AddGold(5);

            // Corrupción menos en NormalWorld
            if (upgradeMgr != null &&
                upgradeMgr.IsUnlocked("corruption_normalWorld") &&
                WorldManager.Instance.CurrentWorld == WorldState.Normal)
            {
                CorruptionManager.Instance?.ReduceCorruptionPercent(0.02f); // 2%
            }

            // Oro extra si la oleada terminó en OtherWorld
            if (upgradeMgr != null &&
                upgradeMgr.IsUnlocked("gold_otherWorld") &&
                WorldManager.Instance.CurrentWorld == WorldState.OtherWorld)
            {
                GoldManager.Instance?.AddGold(15);
            }

            GoldManager.Instance?.UpdateGoldUI();

            // Evento de fin de oleada
            OnWaveEnded?.Invoke();
            if (CellInteraction.hoveredCell != null)
                CellInteraction.hoveredCell.RefreshPreview();
        }
    }
    public int GetCurrentWave() => currentWave;
    public int GetEnemiesAlive() => enemiesAlive;
    public int GetEnemiesThisWave() => enemiesThisWave;
    private void CalculateWorldStreak()
    {
        var currentWorld = WorldManager.Instance.CurrentWorld;
        if (currentWorld == WorldState.OtherWorld)
        {
            if (previousWorldState == WorldState.OtherWorld)
                consecutiveOtherWorldWaves++;
            else
                consecutiveOtherWorldWaves = 1;
        }
        else
        {
            consecutiveOtherWorldWaves = 0;
        }

        previousWorldState = currentWorld;
    }
    private int CalculateEnemiesThisWave()
    {
        if (recipeList == null)
        {
            Debug.LogWarning("[WaveManager] No se asignó el WaveRecipeList.");
            return 0;
        }

        var recipe = recipeList.waveRecipes.Find(r => r.waveNumber == currentWave);
        if (recipe == null)
        {
            Debug.LogWarning($"No hay receta configurada para la ronda {currentWave}");
            return 0;
        }

        int total = 0;
        foreach (var step in recipe.steps)
            total += Mathf.CeilToInt(step.count * enemyCountMultiplier);

        Debug.Log($"[WaveManager] Total enemigos esta oleada: {total} (receta ScriptableObject, multiplicador: {enemyCountMultiplier})");
        return total;
    }
    public void SetEnemyCountMultiplier(float value)
    {
        enemyCountMultiplier = Mathf.Clamp(value, 0.5f, 3f);
    }

}
