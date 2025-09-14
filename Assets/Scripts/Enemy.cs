using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 1f;

    public Canvas healthCanvas;
    public Image healthBar;

    private void Start()
    {
        currentHealth = maxHealth;

        // Crear Canvas y barra de vida si no tiene
        if (healthCanvas == null)
        {
            GameObject canvasObj = new GameObject("HealthCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 2f, 0); // altura sobre el enemigo

            healthCanvas = canvasObj.AddComponent<Canvas>();
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.transform.localScale = Vector3.one * 0.1f;

            GameObject barObj = new GameObject("HealthBar");
            barObj.transform.SetParent(canvasObj.transform);
            healthBar = barObj.AddComponent<Image>();
            healthBar.color = Color.green;

            RectTransform rt = healthBar.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 0.2f);
            rt.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        // Actualizar barra de vida
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
            if (currentHealth / maxHealth > 0.5f) healthBar.color = Color.green;
            else if (currentHealth / maxHealth > 0.2f) healthBar.color = Color.yellow;
            else healthBar.color = Color.red;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}


