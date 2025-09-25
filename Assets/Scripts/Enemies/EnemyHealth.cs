using UnityEngine;

/// <summary>
/// Salud de enemigo, segura para pooling:
/// - Reengancha dependencias si quedaron null (IHealthDisplay / IEnemyDeathHandler / Enemy)
/// - Expone SetMaxHealth / SetCurrentHealth para resets desde el pool
/// - Actualiza la barra siempre que cambie la vida
/// - Aplica multiplicador global de daño (GameModifiersManager)
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float defense;
    [SerializeField] private float currentHealth;

    private bool isDead = false;

    // Refs (pueden venir null al salir del pool; las re‐cacheamos)
    private Enemy enemyReference;
    private IHealthDisplay healthBarDisplay;
    private IEnemyDeathHandler deathHandler;

    // Eventos opcionales (por si querés enganchar VFX/SFX)
    public event System.Action<float, float> OnDamaged; // (current, max)
    public event System.Action OnDied;

    private void Awake()
    {
        CacheDependencies();
    }

    private void OnEnable()
    {
        // En pooling puede re‐activarse sin Awake: aseguramos dependencias y estado coherente
        CacheDependenciesIfMissing();
        // Si estaba marcado como muerto (de una corrida anterior), lo "revivimos" al activar
        if (isDead) isDead = false;
        // Aseguramos que la barra refleje el estado actual
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    // ---------- Dependencias / Cache ----------

    private void CacheDependencies()
    {
        enemyReference = GetComponent<Enemy>();
        if (healthBarDisplay == null) healthBarDisplay = GetComponent<IHealthDisplay>();
        if (deathHandler == null) deathHandler = GetComponent<IEnemyDeathHandler>();
    }

    private void CacheDependenciesIfMissing()
    {
        if (enemyReference == null || ReferenceEquals(enemyReference, null))
            enemyReference = GetComponent<Enemy>();

        if (healthBarDisplay == null)
            healthBarDisplay = GetComponent<IHealthDisplay>();

        if (deathHandler == null)
            deathHandler = GetComponent<IEnemyDeathHandler>();
    }

    // ---------- Inicialización / Reset ----------

    /// <summary>Inicializa vida/defensa y deja el enemigo vivo.</summary>
    public void Initialize(float maxHealth, float defense)
    {
        this.maxHealth = Mathf.Max(1f, maxHealth);
        this.defense = Mathf.Max(0f, defense);
        currentHealth = this.maxHealth;
        isDead = false;

        CacheDependenciesIfMissing();
        healthBarDisplay?.UpdateHealthBar(currentHealth, this.maxHealth);
    }

    /// <summary>Setup directo desde EnemyData (si lo usás).</summary>
    public void InitializeFromData(EnemyData data)
    {
        if (data == null) { Initialize(10f, 0f); return; }
        Initialize(data.maxHealth, data.defense);
    }

    /// <summary>Para usar desde el pool durante ResetEnemy().</summary>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    /// <summary>Para usar desde el pool durante ResetEnemy().</summary>
    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        isDead = (currentHealth <= 0f);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void SetDefense(float value)
    {
        defense = Mathf.Max(0f, value);
    }

    public void ReviveFull()
    {
        isDead = false;
        currentHealth = Mathf.Max(1f, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    // ---------- Daño / Muerte ----------

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Multiplicador global
        float modAmount = amount;
        if (GameModifiersManager.Instance != null)
            modAmount *= GameModifiersManager.Instance.enemyDamageTakenMultiplier;

        float realDamage = Mathf.Max(0f, modAmount - defense);
        if (realDamage <= 0f) return;

        currentHealth -= realDamage;
        if (currentHealth < 0f) currentHealth = 0f;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        OnDamaged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
        OnDamaged?.Invoke(currentHealth, maxHealth);
    }

    public void MultiplyMaxHealth(float multiplier)
    {
        multiplier = Mathf.Max(0.01f, multiplier);
        maxHealth = Mathf.Round(maxHealth * multiplier);
        if (maxHealth < 1f) maxHealth = 1f;

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Limpieza visual de barra (si la barra se instancia aparte)
        healthBarDisplay?.DestroyBar();

        // Notificar muerte (el handler debería devolver al pool y avisar a WaveManager)
        deathHandler?.OnEnemyDeath(enemyReference);

        OnDied?.Invoke();
    }

    // ---------- Getters / Estado ----------

    public bool IsDead() => isDead;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetDefense() => defense;
}
