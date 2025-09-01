using System;
using UnityEngine;

public class PlayerExperienceManager : MonoBehaviour
{
    public static PlayerExperienceManager Instance { get; private set; }

    // ESENCIA TOTAL acumulada (persistente)
    private int totalNormalEssence = 0;
    private int totalOtherWorldEssence = 0;

    // ESENCIA por sesión (solo la run actual)
    private int sessionNormalEssence = 0;
    private int sessionOtherWorldEssence = 0;

    private float timePlayed = 0f;

    [Header("Essence Values")]
    [SerializeField] private int essencePerSecond = 1;
    [SerializeField] private int essencePerBoss = 100;
    [SerializeField] private int essencePerMiniBoss = 50;
    [SerializeField] private int essencePerNoDamageWave = 50;
    [SerializeField] private int essencePerEnemyKill = 2;

    private bool coreDamagedThisWave = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cargar de PlayerPrefs
            totalNormalEssence = PlayerPrefs.GetInt("TotalNormalEssence", 0);
            totalOtherWorldEssence = PlayerPrefs.GetInt("TotalOtherWorldEssence", 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnWaveEnded += OnWaveEnded;
        }
        WorldManager.OnWorldChanged += OnWorldChanged;
        Core.OnCoreDamaged += OnCoreDamaged;
        EnemyTracker.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            WaveManager.Instance.OnWaveEnded -= OnWaveEnded;
        }
        WorldManager.OnWorldChanged -= OnWorldChanged;
        Core.OnCoreDamaged -= OnCoreDamaged;
        EnemyTracker.OnEnemyKilled -= OnEnemyKilled;
    }

    private void Update()
    {
        // Sumar esencia solo si hay oleada activa
        if (WaveManager.Instance != null && WaveManager.Instance.WaveInProgress)
        {
            timePlayed += Time.deltaTime;
            if (timePlayed >= 1f)
            {
                AddEssencePerWorld(essencePerSecond, WorldManager.Instance.CurrentWorld);
                timePlayed -= 1f;
            }
        }
        // --- DEBUG: Agregar esencia manual desde inspector ---
        if (addEssenceNow)
        {
            if (addNormalEssence > 0)
            {
                AddEssenceManually(WorldState.Normal, addNormalEssence);
                addNormalEssence = 0;
            }
            if (addOtherWorldEssence > 0)
            {
                AddEssenceManually(WorldState.OtherWorld, addOtherWorldEssence);
                addOtherWorldEssence = 0;
            }
            addEssenceNow = false;
        }
    }

    private void OnWaveStarted(int wave, int totalEnemies)
    {
        coreDamagedThisWave = false;
    }

    private void OnWaveEnded()
    {
        // Sumar esencia solo si el core NO recibió daño
        if (!coreDamagedThisWave)
            AddEssencePerWorld(essencePerNoDamageWave, WorldManager.Instance.CurrentWorld);
    }

    private void OnWorldChanged(WorldState newWorld)
    {
        // Acá podrías sumar esencia extra si querés, al cambiar de mundo.
        // Ejemplo: AddEssencePerWorld(10, newWorld);
    }

    private void OnCoreDamaged()
    {
        coreDamagedThisWave = true;
    }

    // NUEVO: Cada vez que se mata un enemigo, sumar esencia según el mundo del enemigo
    private void OnEnemyKilled(EnemyType enemyType, WorldState originWorld)
    {
        // Podés distinguir boss/miniBoss, etc.
        int amount = essencePerEnemyKill;
        if (enemyType == EnemyType.Boss)
            amount = essencePerBoss;
        else if (enemyType == EnemyType.MiniBoss)
            amount = essencePerMiniBoss;

        AddEssencePerWorld(amount, originWorld);
    }

    /// <summary>
    /// Suma esencia a la barra correspondiente según el mundo.
    /// </summary>
    public void AddEssencePerWorld(int amount, WorldState world)
    {
        if (world == WorldState.Normal)
            sessionNormalEssence += amount;
        else
            sessionOtherWorldEssence += amount;

        WaveUIController.Instance?.UpdateExperienceUI();
    }

    /// <summary>
    /// Llamar esto al terminar la partida para volcar la sesión al total (persistente).
    /// </summary>
    public void AddEssenceSessionToTotal()
    {
        totalNormalEssence += sessionNormalEssence;
        totalOtherWorldEssence += sessionOtherWorldEssence;
        PlayerPrefs.SetInt("TotalNormalEssence", totalNormalEssence);
        PlayerPrefs.SetInt("TotalOtherWorldEssence", totalOtherWorldEssence);
        sessionNormalEssence = 0;
        sessionOtherWorldEssence = 0;
    }

    public int GetTotalEssence(WorldState world) =>
        (world == WorldState.Normal) ? totalNormalEssence : totalOtherWorldEssence;

    public int GetSessionEssence(WorldState world) =>
        (world == WorldState.Normal) ? sessionNormalEssence : sessionOtherWorldEssence;

    public void RemoveEssence(WorldState world, int amount)
    {
        if (world == WorldState.Normal)
            totalNormalEssence = Mathf.Max(0, totalNormalEssence - amount);
        else
            totalOtherWorldEssence = Mathf.Max(0, totalOtherWorldEssence - amount);

        PlayerPrefs.SetInt("TotalNormalEssence", totalNormalEssence);
        PlayerPrefs.SetInt("TotalOtherWorldEssence", totalOtherWorldEssence);
        PlayerPrefs.Save();
    }
    public bool RemoveEssenceBoth(int amount)
    {
        int half = Mathf.CeilToInt(amount / 2f);
        // Solo permite gastar si hay suficiente de ambas
        if (totalNormalEssence >= half && totalOtherWorldEssence >= half)
        {
            totalNormalEssence -= half;
            totalOtherWorldEssence -= half;

            PlayerPrefs.SetInt("TotalNormalEssence", totalNormalEssence);
            PlayerPrefs.SetInt("TotalOtherWorldEssence", totalOtherWorldEssence);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
    [Header("Debug Essence")]
    [SerializeField] private int addNormalEssence = 0;
    [SerializeField] private int addOtherWorldEssence = 0;
    [SerializeField] private bool addEssenceNow = false;
    /// <summary>
    /// Permite sumar esencia a mano desde el inspector para test.
    /// </summary>
    public void AddEssenceManually(WorldState world, int amount)
    {
        if (world == WorldState.Normal)
        {
            sessionNormalEssence += amount;
            totalNormalEssence += amount;
            PlayerPrefs.SetInt("TotalNormalEssence", totalNormalEssence);
        }
        else
        {
            sessionOtherWorldEssence += amount;
            totalOtherWorldEssence += amount;
            PlayerPrefs.SetInt("TotalOtherWorldEssence", totalOtherWorldEssence);
        }
        PlayerPrefs.Save();

        WaveUIController.Instance?.UpdateExperienceUI();
    }


}
