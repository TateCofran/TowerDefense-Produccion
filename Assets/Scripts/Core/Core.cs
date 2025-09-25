using UnityEngine;

public class Core : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        //Actualizamos la UI al inicio
        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        // Actualizamos la UI cada vez que se da�a el n�cleo
        UIController.Instance.UpdateCoreHealth(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Core destroyed!");
            // L�gica de game over
        }
    }
}
