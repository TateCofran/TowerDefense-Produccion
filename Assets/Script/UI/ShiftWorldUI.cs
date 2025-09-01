using UnityEngine;
using UnityEngine.UI;

public class ShiftWorldUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image fillImage; // Image tipo Filled (asignar en Inspector)

    private WorldManager worldManager;
    private bool isWaveActive = false;

    private float pauseTimestamp;


    void Start()
    {
        worldManager = WorldManager.Instance;
        if (worldManager == null)
        {
            Debug.LogError("[ShiftWorldUI] No se encontr� WorldManager en la escena.");
            enabled = false;
            return;
        }

        // Suscribirse al cambio de mundo (opcional, solo si lo us�s)
        WorldManager.OnWorldChanged += OnWorldChanged;

        // **Suscribirse a los eventos de la oleada**
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveEnded += HandleWaveEnded;
        }

        // La barra solo se actualiza si arranca la oleada, as� que no llamamos UpdateBar() ac�
    }
    private void OnEnable()
    {
        // **Suscribirse tambi�n ac�, por seguridad (si reinici�s el script)**
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += HandleWaveStarted;
            WaveManager.Instance.OnWaveEnded += HandleWaveEnded;
        }
    }
    private void OnDisable()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= HandleWaveStarted;
            WaveManager.Instance.OnWaveEnded -= HandleWaveEnded;
        }
    }
    void OnDestroy()
    {
        WorldManager.OnWorldChanged -= OnWorldChanged;
    }

    void Update()
    {
        // Solo actualizar la barra si la oleada est� activa
        if (isWaveActive)
            UpdateBar();
    }

    private void UpdateBar()
    {
        if (worldManager == null) return;

        float cooldown = GetShiftCooldown();
        float elapsed = Time.time - GetLastShiftTime();
        float t = Mathf.Clamp01(elapsed / cooldown);
        fillImage.fillAmount = t;
    }

    private float GetShiftCooldown()
    {
        // Usamos serialized/private, as� acced�s a shiftCooldown
        var type = typeof(WorldManager);
        var field = type.GetField("shiftCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (float)field.GetValue(worldManager);
    }

    private float GetLastShiftTime()
    {
        var type = typeof(WorldManager);
        var field = type.GetField("lastShiftTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (float)field.GetValue(worldManager);
    }

    private void OnWorldChanged(WorldState _)
    {
        // Si quer�s un feedback especial, ponelo ac�
        // Ejemplo: animar la barra o flashear color
    }

    // Llam� esto si tu bot�n de UI quiere saber si est� listo para shift
    public bool CanShiftWorld()
    {
        float cooldown = GetShiftCooldown();
        float elapsed = Time.time - GetLastShiftTime();
        return elapsed >= cooldown;
    }
    private void HandleWaveStarted(int wave, int enemies)
    {
        isWaveActive = true;
        float pausedDuration = Time.time - pauseTimestamp;
        AdjustLastShiftTime(pausedDuration);
    }


    private void HandleWaveEnded()
    {
        isWaveActive = false;
        pauseTimestamp = Time.time;

    }
    private void AdjustLastShiftTime(float delta)
    {
        var type = typeof(WorldManager);
        var field = type.GetField("lastShiftTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float lastShift = (float)field.GetValue(worldManager);
        field.SetValue(worldManager, lastShift + delta);
    }

}
