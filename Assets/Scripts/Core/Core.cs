using UnityEngine;

public class Core : MonoBehaviour
{
    [Header("Core Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    public event System.Action<int> OnHealthChanged;
    public event System.Action OnCoreDestroyed;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            OnCoreDestroyed?.Invoke();
            HandleCoreDestruction();
        }
    }

    private void HandleCoreDestruction()
    {
        Debug.Log("Core destruido!");
        // Aquí puedes agregar efectos de destrucción, game over, etc.

        // Opcional: desactivar o destruir el objeto
        gameObject.SetActive(false);
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
}
