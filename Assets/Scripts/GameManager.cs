using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI References")]
    public Text healthText;
    public Text waveText;
    public Text enemiesText;
    public GameObject gameOverPanel;

    private Core core;
    private bool gameOver = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Buscar componentes de forma segura
        core = FindFirstObjectByType<Core>();

        // Suscribirse a eventos del Core
        if (core != null)
        {
            core.OnHealthChanged += OnCoreHealthChanged;
            core.OnCoreDestroyed += OnCoreDestroyed;
            OnCoreHealthChanged(core.currentHealth);
        }
        else
        {
            Debug.LogWarning("GameManager: No se encontró Core en la escena");
        }

        // Inicializar UI
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateWaveInfo();
        UpdateEnemiesText();
    }

    void Update()
    {
        // Actualizar info de enemigos de forma segura
        UpdateEnemiesText();
    }

    // Opción A: Hacerlo opcional
    void UpdateEnemiesText()
    {
        if (enemiesText == null) return; // Salir si es null

    }

    void OnCoreHealthChanged(int currentHealth)
    {
        if (healthText != null && core != null)
        {
            healthText.text = $"Core: {currentHealth}/{core.maxHealth}";
        }
    }

    void OnCoreDestroyed()
    {
        if (gameOver) return;

        gameOver = true;
        Debug.Log("Game Over activado");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void UpdateWaveInfo()
    {

    }

    void OnDestroy()
    {
        // Desuscribirse de eventos de forma segura
        if (core != null)
        {
            core.OnHealthChanged -= OnCoreHealthChanged;
            core.OnCoreDestroyed -= OnCoreDestroyed;
        }
    }

    // Método para botón de reinicio
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}