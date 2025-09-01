using UnityEngine;

public class WaveEnemyGeneratorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveEnemyGeneratorNormal normalGenerator;
    [SerializeField] private WaveEnemyGeneratorOther otherWorldGenerator;


    private void Awake()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
    }
    private void OnEnable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
    }
    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
        }
    }

    private void OnWaveStarted(int waveNumber, int totalEnemiesThisWave)
    {
        // ¿En qué mundo comenzó la oleada?
        bool inOtherWorld = WorldManager.Instance.CurrentWorld == WorldState.OtherWorld;

        Debug.Log($"[WaveEnemyGeneratorManager] OnWaveStarted llamada - Mundo: {(inOtherWorld ? "OtherWorld" : "NormalWorld")}, Wave: {waveNumber}, Total: {totalEnemiesThisWave}");

        // Enemigos base
        int finalEnemiesToSpawn = totalEnemiesThisWave;

        // (Podés mantener tus bonificaciones por jefes u oleadas especiales acá si querés)
        // if (waveNumber % 5 == 0) finalEnemiesToSpawn++;
        // if (waveNumber % 15 == 0) finalEnemiesToSpawn++;

        // Penalidad por corrupción
        var corruptionLevel = CorruptionManager.Instance.CurrentLevel;
        float corruptionMultiplier = 1f;

        switch (corruptionLevel)
        {
            case CorruptionManager.CorruptionLevel.Level1:
                corruptionMultiplier = 1.05f; // +5%
                break;
            case CorruptionManager.CorruptionLevel.Level3:
                corruptionMultiplier = 1.15f; // +15%
                break;
            default:
                corruptionMultiplier = 1f; // sin extra
                break;
        }

        finalEnemiesToSpawn = Mathf.RoundToInt(finalEnemiesToSpawn * corruptionMultiplier);

        if (corruptionLevel == CorruptionManager.CorruptionLevel.Level1 || corruptionLevel == CorruptionManager.CorruptionLevel.Level3)
            Debug.Log($"[WaveEnemyGeneratorManager] Penalidad por corrupción: x{corruptionMultiplier} enemigos ({finalEnemiesToSpawn})");

        // Genera los enemigos en el generador correspondiente
        if (inOtherWorld)
        {
            if (otherWorldGenerator != null)
                otherWorldGenerator.Spawn(waveNumber, finalEnemiesToSpawn);
            else
                Debug.LogError("[WaveEnemyGeneratorManager] Referencia a OtherWorldGenerator faltante.");
        }
        else
        {
            if (normalGenerator != null)
                normalGenerator.Spawn(waveNumber, finalEnemiesToSpawn);
            else
                Debug.LogError("[WaveEnemyGeneratorManager] Referencia a NormalGenerator faltante.");
        }
    }


}
