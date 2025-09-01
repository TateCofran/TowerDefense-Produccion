using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour, IHealthDisplay
{
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 1.5f;

    private GameObject healthBarInstance;
    private Image healthBarFill;

    // Llama esto cuando el enemigo se saca del pool o spawnea
    public void Initialize(Transform parent, float maxHealth)
    {
        if (healthBarPrefab == null)
        {
            Debug.LogWarning("No se asignó el prefab de la barra de vida.");
            return;
        }

        // Si ya hay barra, no la instancies de nuevo, solo reusala y ponela arriba del enemigo
        if (healthBarInstance == null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, parent.position + Vector3.up * healthBarHeight, Quaternion.identity, parent);
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
        else
        {
            healthBarInstance.transform.SetParent(parent);
            healthBarInstance.transform.position = parent.position + Vector3.up * healthBarHeight;
            healthBarInstance.SetActive(true); // Reactivá si venís de un pool
            // Si la referencia al fill se perdió, buscala de nuevo
            if (healthBarFill == null)
                healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }

        UpdateHealthBar(maxHealth, maxHealth); // Barra llena al iniciar
    }

    // Llama esto cada vez que el enemigo recibe daño
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        float fill = Mathf.Clamp01(currentHealth / maxHealth);
        healthBarFill.fillAmount = fill;
    }

    // Llama esto cuando el enemigo muere o se devuelve al pool
    public void DestroyBar()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false); // Pool-friendly (opcional: destroy si no usás pool de barras)
            // Opcional: resetear barra llena para el próximo uso
            UpdateHealthBar(1, 1);
        }
    }
}
