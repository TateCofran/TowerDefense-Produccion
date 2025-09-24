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

    [Header("Autostart")]
    [Tooltip("Si está activo, al colocar un tile/torreta con éxito y no hay oleada en curso, arranca la siguiente.")]
    [SerializeField] private bool autoStartOnPlacement = true;

    [Tooltip("Anti-rebote para múltiples eventos de colocación casi simultáneos.")]
    [SerializeField] private float placementCooldown = 0.15f;

    private float _nextAllowedPlacementStartTime = 0f;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesThisWave = 0;
    private int consecutiveOtherWorldWaves = 0;
    //private WorldState previousWorldState = WorldState.Normal;

    public bool WaveInProgress => enemiesAlive > 0;
    public bool IsFirstWave => isFirstWave;
    public bool IsLastWave() => currentWave >= maxWaves;

    private bool isFirstWave = true;
    private bool waveStarted = false;
    //private WorldState waveStartWorld;

    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {


        // >>> NUEVO: suscripción a eventos de colocación
        PlacementEvents.OnTurretPlaced += HandleTurretPlaced;
        PlacementEvents.OnTileApplied += HandleTileApplied;
    }

    private void OnDisable()
    {

        // >>> NUEVO: desuscripción
        PlacementEvents.OnTurretPlaced -= HandleTurretPlaced;
        PlacementEvents.OnTileApplied -= HandleTileApplied;
    }

    // ====== AUTOSTART DESDE COLOCACIÓN ======
    private void HandleTurretPlaced(PlacementEvents.TurretPlacedInfo info)
    {
        TryAutoStartAfterPlacement("TurretPlaced");
    }

    private void HandleTileApplied(PlacementEvents.TileAppliedInfo info)
    {
        TryAutoStartAfterPlacement("TileApplied");
    }

    private void TryAutoStartAfterPlacement(string source)
    {
        if (!autoStartOnPlacement) return;

        // Anti-rebote para dobles eventos (ej: tile + conexión de path + etc.)
        if (Time.unscaledTime < _nextAllowedPlacementStartTime) return;
        _nextAllowedPlacementStartTime = Time.unscaledTime + placementCooldown;

        // Reglas:
        // - No arranco si ya hay una oleada corriendo
        // - Si es la primera, la arranco
        // - Si terminó la anterior (enemiesAlive <= 0), arranco la siguiente
        // - No arranco si ya estamos en la última
        if (waveStarted || WaveInProgress) return;
        if (IsLastWave()) return;

        Debug.Log($"[WaveManager] Autostart por {source}. Oleada actual: {currentWave}, enemigos vivos: {enemiesAlive}.");
        StartNextWave();
    }

    // ====== API PÚBLICA ======
    public void TryStartFirstWave()
    {
        if (!waveStarted && currentWave == 0)
            StartNextWave();
    }

    public void StartNextWave()
    {
        if (waveStarted) return;
        if (IsLastWave())
        {
            Debug.Log("[WaveManager] Ya se alcanzó la última oleada.");
            return;
        }

        waveStarted = true;


        currentWave++;


        int totalEnemies = CalculateEnemiesThisWave();
        enemiesThisWave = totalEnemies;
        enemiesAlive = totalEnemies;

        Debug.Log($"[WaveManager] Oleada {currentWave} iniciada con {totalEnemies} enemigos.");
        OnWaveStarted?.Invoke(currentWave, totalEnemies);

    }

    public void NotifyEnemyKilled()
    {
        enemiesAlive--;
        //WaveUIController.Instance?.UpdateEnemiesRemaining(enemiesAlive);

        if (enemiesAlive <= 0)
        {
            Debug.Log($"[WaveManager] Oleada {currentWave} finalizada.");
            waveStarted = false;
        }
    }

    public int GetCurrentWave() => currentWave;
    public int GetEnemiesAlive() => enemiesAlive;
    public int GetEnemiesThisWave() => enemiesThisWave;

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

        Debug.Log($"[WaveManager] Total enemigos esta oleada: {total} (SO + x{enemyCountMultiplier})");
        return total;
    }

    public void SetEnemyCountMultiplier(float value)
        => enemyCountMultiplier = Mathf.Clamp(value, 0.5f, 3f);
}
