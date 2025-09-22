using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Configuración de Enemigos")]
    public GameObject[] enemyPrefabs;
    public float spawnInterval = 3f;
    public int maxEnemies = 20;

    [Header("Sistema de Oleadas")]
    public bool enableWaves = true;
    public float timeBetweenWaves = 30f;
    public int baseEnemiesPerWave = 5;
    public int enemiesPerWaveIncrement = 2;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private GridGenerator gridGenerator;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentWave = 0;
    private bool waveInProgress = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        gridGenerator = FindObjectOfType<GridGenerator>();
        if (gridGenerator == null)
        {
            Debug.LogError("SpawnManager: No se encontró GridGenerator");
            return;
        }

        // Debug inicial
        if (showDebugInfo)
        {
            gridGenerator.DebugSpawnPoints();
        }

        if (enableWaves)
        {
            StartCoroutine(WaveSystem());
        }
        else
        {
            StartCoroutine(ContinuousSpawning());
        }
    }

    IEnumerator WaveSystem()
    {
        yield return new WaitForSeconds(2f);

        while (true)
        {
            if (showDebugInfo) Debug.Log($"Preparando oleada {currentWave + 1}...");
            yield return new WaitForSeconds(timeBetweenWaves);

            StartWave(currentWave);
            currentWave++;

            while (waveInProgress) yield return new WaitForSeconds(1f);
        }
    }

    void StartWave(int waveNumber)
    {
        waveInProgress = true;
        int enemiesThisWave = baseEnemiesPerWave + (waveNumber * enemiesPerWaveIncrement);

        if (showDebugInfo) Debug.Log($"Iniciando oleada {waveNumber + 1} con {enemiesThisWave} enemigos");
        StartCoroutine(SpawnWave(enemiesThisWave));
    }

    IEnumerator SpawnWave(int enemyCount)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            if (activeEnemies.Count >= maxEnemies)
            {
                yield return new WaitUntil(() => activeEnemies.Count < maxEnemies);
            }

            if (SpawnEnemyAtPathEnd())
            {
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                if (showDebugInfo) Debug.Log("No se pudo spawnear enemigo, reintentando...");
                yield return new WaitForSeconds(1f);
            }
        }

        yield return new WaitUntil(() => activeEnemies.Count == 0);
        waveInProgress = false;
        if (showDebugInfo) Debug.Log($"¡Oleada {currentWave} completada!");
    }

    IEnumerator ContinuousSpawning()
    {
        while (true)
        {
            if (activeEnemies.Count < maxEnemies)
            {
                SpawnEnemyAtPathEnd();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // NUEVO: Spawnear solo en FINALES de camino
    public bool SpawnEnemyAtPathEnd()
    {
        if (gridGenerator == null || enemyPrefabs.Length == 0)
        {
            if (showDebugInfo) Debug.LogWarning("SpawnManager: Configuración incompleta");
            return false;
        }

        // Obtener FINALES de camino (no cualquier exit)
        List<Vector3> spawnPoints = gridGenerator.GetSpawnPoints();

        if (spawnPoints.Count == 0)
        {
            if (showDebugInfo) Debug.LogWarning("No hay finales de camino para spawn");
            return false;
        }

        // Elegir spawn point aleatorio
        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        activeEnemies.Add(enemy);

        // Configurar el enemigo
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            Transform core = FindCore();
            if (core != null)
            {
                enemyScript.Initialize(spawnPos, core, this);
                if (showDebugInfo) Debug.Log($"Enemigo spawneado en FINAL de camino: {spawnPos}");
                return true;
            }
        }

        // Limpieza si falla
        Destroy(enemy);
        activeEnemies.Remove(enemy);
        return false;
    }

    // Mantener por compatibilidad
    public bool SpawnEnemyAtExit()
    {
        return SpawnEnemyAtPathEnd();
    }

    Transform FindCore()
    {
        GameObject coreObj = GameObject.FindGameObjectWithTag("Core");
        if (coreObj == null) Debug.LogError("SpawnManager: Core no encontrado");
        return coreObj != null ? coreObj.transform : null;
    }

    public void OnEnemyDied(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            if (showDebugInfo) Debug.Log($"Enemigo eliminado. Restantes: {activeEnemies.Count}");
        }
    }

    public int GetActiveEnemiesCount() => activeEnemies.Count;
    public int GetCurrentWave() => currentWave;
    public bool IsWaveInProgress() => waveInProgress;

    // Método para debug rápido
    [ContextMenu("Spawn Enemy Now")]
    public void SpawnEnemyNow()
    {
        SpawnEnemyAtPathEnd();
    }
}