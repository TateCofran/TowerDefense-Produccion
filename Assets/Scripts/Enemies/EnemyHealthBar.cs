using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour, IHealthDisplay
{
    [Header("Prefabs & Refs")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 1.5f;

    [SerializeField] private GameObject damageTextPrefab; // <-- NUEVO (TextMeshProUGUI dentro)
    [SerializeField] private float damageTextYOffset = 0.35f; // separación arriba de la barra

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

        if (healthBarInstance == null)
        {
            healthBarInstance = Instantiate(
                healthBarPrefab,
                parent.position + Vector3.up * healthBarHeight,
                Quaternion.identity,
                parent
            );
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
        else
        {
            healthBarInstance.transform.SetParent(parent);
            healthBarInstance.transform.position = parent.position + Vector3.up * healthBarHeight;
            healthBarInstance.SetActive(true);
            if (healthBarFill == null)
                healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }

        UpdateHealthBar(maxHealth, maxHealth);
    }

    // Llama esto cada vez que el enemigo recibe daño
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        float fill = Mathf.Clamp01(currentHealth / maxHealth);
        healthBarFill.fillAmount = fill;

        // Mantener la barra arriba del enemigo (por si se mueve)
        if (healthBarInstance && healthBarInstance.transform.parent)
        {
            var p = healthBarInstance.transform.parent;
            healthBarInstance.transform.position = p.position + Vector3.up * healthBarHeight;
        }
    }

    public void ShowDamageText(float damageValue, Transform parent)
    {
        if (!damageTextPrefab) return;

        // 1) Instanciar
        GameObject go = Instantiate(damageTextPrefab, parent);

        // 2) Asegurar Canvas en World Space y con cámara
        Canvas canvas = go.GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = go.AddComponent<Canvas>(); // fallback si el prefab no tenía canvas

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 5000;  // arriba de todo
                                     // Ajustes típicos para que no se haga gigante
        var rectCanvas = canvas.GetComponent<RectTransform>();
        if (rectCanvas != null)
        {
            rectCanvas.sizeDelta = new Vector2(200, 100);
            rectCanvas.localScale = Vector3.one * 0.01f; // escala razonable en mundo
        }

        // 3) Posición: un poco arriba de la barra
        Vector3 worldPos = parent.position + Vector3.up * (healthBarHeight + damageTextYOffset);
        go.transform.position = worldPos;

        // 4) Texto numérico
        var tmp = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmp == null) tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmp != null)
        {
            // asegurar alpha visible
            var c = tmp.color; c.a = 1f; tmp.color = c;
            string txt = Mathf.Approximately(damageValue, Mathf.Round(damageValue))
                ? ((int)Mathf.Round(damageValue)).ToString()
                : damageValue.ToString("0.0");
            tmp.text = txt;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // 5) Anim (si el prefab tiene el componente)
        var popup = go.GetComponent<DamageTextPopup>();
        if (popup != null) popup.Play();
        else Destroy(go, 1.0f);
    }


    // Llama esto cuando el enemigo muere o se devuelve al pool
    public void DestroyBar()
    {
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(false);
            UpdateHealthBar(1, 1);
        }
    }
}
