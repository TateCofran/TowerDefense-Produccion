using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    private float maxHealth;
    private float defense;
    private float currentHealth;
    private bool isDead = false;

    private Enemy enemyReference;
    private IHealthDisplay healthBarDisplay;
    private IEnemyDeathHandler deathHandler;

    private void Awake()
    {
        enemyReference = GetComponent<Enemy>();
        healthBarDisplay = GetComponent<IHealthDisplay>();
        deathHandler = GetComponent<IEnemyDeathHandler>();
    }

    public void Initialize(float maxHealth, float defense)
    {
        this.maxHealth = maxHealth;
        this.defense = defense;
        currentHealth = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Aplicar el multiplicador global de daño recibido por enemigos
        float modAmount = amount;
        if (GameModifiersManager.Instance != null)
            modAmount *= GameModifiersManager.Instance.enemyDamageTakenMultiplier;

        float realDamage = Mathf.Max(0, modAmount - defense);
        currentHealth -= realDamage;

        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    public void Die()
    {
        if (isDead) return;
        isDead = true;

        //enemyReference?.NotifyDeath();

        healthBarDisplay?.DestroyBar();
        deathHandler?.OnEnemyDeath(enemyReference);
    }

    public void MultiplyMaxHealth(float multiplier)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * multiplier);
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        // Si usás un sistema de barra de vida, actualizala:
        healthBarDisplay?.UpdateHealthBar(currentHealth, maxHealth);
    }


    public bool IsDead() => isDead;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
}
