using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private ShiftingWorldUI ui;   // referencia a la UI

    [Header("Estado")]
    [SerializeField] private World currentWorld = World.Normal;

    [Header("Progreso (0..100)")]
    [SerializeField] private float normalProgress = 0f;
    [SerializeField] private float otherProgress = 0f;

    [Header("Ajustes")]
    [SerializeField] private float speedPerSecond = 20f;
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    // Latches / flags
    private bool fireGuard = false;
    private bool normalPanelOpen = false;
    private bool otherPanelOpen = false;

    // NUEVO: pausas hasta que elijas
    private bool normalWaitingChoice = false;
    private bool otherWaitingChoice = false;

    void Update()
    {
        // Cambio de mundo
        if (Input.GetKeyDown(toggleKey))
        {
            currentWorld = (currentWorld == World.Normal) ? World.Otro : World.Normal;
            Debug.Log($"[ShiftingWorldMechanic] Cambié de mundo → {currentWorld}");

            ResetAllProgressAndPanels(); // <-- SIEMPRE reinicia todo al cambiar
        }


        // ----- Mundo Normal -----
        if (currentWorld == World.Normal)
        {
            // Si está esperando elección, no sumar ni tocar el progreso
            if (!normalWaitingChoice)
            {
                if (normalProgress < 100f)
                    normalProgress = Mathf.Min(100f, normalProgress + speedPerSecond * Time.deltaTime);

                if (!fireGuard && Mathf.FloorToInt(normalProgress) >= 100)
                {
                    fireGuard = true;
                    normalWaitingChoice = true; // PAUSA el contador

                    if (ui != null && !normalPanelOpen)
                    {
                        normalPanelOpen = true;
                        Debug.Log("[ShiftingWorldMechanic] Normal llegó a 100 → abrir panel (pausado hasta elegir).");
                        ui.ShowNormalReached(() =>
                        {
                            // La UI cierra porque ELEGISTE una opción → liberar pausa
                            normalPanelOpen = false;
                            normalWaitingChoice = false;
                            normalProgress = 0f; // recién ahora reseteamos
                            Debug.Log("[ShiftingWorldMechanic] Panel Normal cerrado → reanudar progreso (reseteado a 0).");
                        });
                    }
                    else if (ui == null)
                    {
                        Debug.LogWarning("[ShiftingWorldMechanic] Falta referencia a ShiftingWorldUI.");
                    }
                }
            }
        }
        // ----- Otro Mundo -----
        else
        {
            if (!otherWaitingChoice)
            {
                if (otherProgress < 100f)
                    otherProgress = Mathf.Min(100f, otherProgress + speedPerSecond * Time.deltaTime);

                if (!fireGuard && Mathf.FloorToInt(otherProgress) >= 100)
                {
                    fireGuard = true;
                    otherWaitingChoice = true; // PAUSA el contador

                    if (ui != null && !otherPanelOpen)
                    {
                        otherPanelOpen = true;
                        Debug.Log("[ShiftingWorldMechanic] Otro llegó a 100 → abrir panel (pausado hasta elegir).");
                        ui.ShowOtherReached(() =>
                        {
                            otherPanelOpen = false;
                            otherWaitingChoice = false;
                            otherProgress = 0f; // reset tras elegir
                            Debug.Log("[ShiftingWorldMechanic] Panel Otro cerrado → reanudar progreso (reseteado a 0).");
                        });
                    }
                    else if (ui == null)
                    {
                        Debug.LogWarning("[ShiftingWorldMechanic] Falta referencia a ShiftingWorldUI.");
                    }
                }
            }
        }

        if (fireGuard) fireGuard = false;
    }
    // En ShiftingWorldMechanic.cs
    private void Awake()
    {
        // Si querés hacer algo cuando el jugador elige una torreta:
        if (ui != null)
            ui.OnTurretChosen += HandleTurretChosen;
    }

    private void OnDestroy()
    {
        if (ui != null)
            ui.OnTurretChosen -= HandleTurretChosen;
    }

    // Recibe la torreta elegida desde la UI
    private void HandleTurretChosen(TurretDataSO so)
    {
        Debug.Log($"[ShiftingWorldMechanic] Torreta elegida: {(string.IsNullOrEmpty(so.displayName) ? so.name : so.displayName)}");
        // TODO: acá tu lógica (spawn de torreta, abrir tienda, etc.)
    }
    private void ResetAllProgressAndPanels()
    {
        // cerrar cualquier panel abierto
        if (ui != null) ui.Close();

        // limpiar flags de espera y de panel abierto
        normalWaitingChoice = false;
        otherWaitingChoice = false;
        normalPanelOpen = false;
        otherPanelOpen = false;
        fireGuard = false;

        // reiniciar porcentajes
        normalProgress = 0f;
        otherProgress = 0f;
    }

    // Helpers (si los usás en UI/debug)
    public void SetSpeed(float v) => speedPerSecond = Mathf.Max(0f, v);
    public string GetWorldName() => currentWorld == World.Normal ? "Normal" : "OtroMundo";
    public int GetNormalInt() => Mathf.Min(100, Mathf.FloorToInt(normalProgress));
    public int GetOtherInt() => Mathf.Min(100, Mathf.FloorToInt(otherProgress));
}
