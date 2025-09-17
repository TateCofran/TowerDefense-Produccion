using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;
    [SerializeField] private ShiftingWorldUI ui;   // <-- referencia a tu UI

    [Header("Estado")]
    [SerializeField] private World currentWorld = World.Normal;

    [Header("Progreso (0..100)")]
    [SerializeField] private float normalProgress = 0f;
    [SerializeField] private float otherProgress = 0f;

    [Header("Ajustes")]
    [SerializeField] private float speedPerSecond = 20f;
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    // Latches
    private bool fireGuard = false;        // evita doble disparo en el frame (generación/acciones)
    private bool normalPanelOpen = false;  // evita reabrir mientras ya está abierto
    private bool otherPanelOpen = false;

    void Update()
    {
        // Cambio de mundo
        if (Input.GetKeyDown(toggleKey))
        {
            currentWorld = (currentWorld == World.Normal) ? World.Otro : World.Normal;
            Debug.Log($"[ShiftingWorldMechanic] Cambié de mundo → {currentWorld}");

            // Reseteo del contador del mundo al que entro (según tu regla previa)
            if (currentWorld == World.Normal) normalProgress = 0f;
            else otherProgress = 0f;
        }

        // Avance del mundo activo
        if (currentWorld == World.Normal)
        {
            if (normalProgress < 100f)
                normalProgress = Mathf.Min(100f, normalProgress + speedPerSecond * Time.deltaTime);

            if (!fireGuard && Mathf.FloorToInt(normalProgress) >= 100)
            {
                fireGuard = true;

                // 1) UI: abrir panel del mundo normal
                if (ui != null && !normalPanelOpen)
                {
                    normalPanelOpen = true;
                    ui.ShowNormalReached(() =>
                    {
                        // Callback opcional cuando se cierra el panel
                        normalPanelOpen = false;
                    });
                }

                // 2) Generar tile (tu comportamiento previo)
                TryAppendTileFromNormal();

                // 3) Reset contador (regla: al instanciar, se reinicia)
                normalProgress = 0f;
            }
        }
        else // Mundo "Otro"
        {
            if (otherProgress < 100f)
                otherProgress = Mathf.Min(100f, otherProgress + speedPerSecond * Time.deltaTime);

            if (!fireGuard && Mathf.FloorToInt(otherProgress) >= 100)
            {
                fireGuard = true;

                // UI: abrir panel del "Otro mundo"
                if (ui != null && !otherPanelOpen)
                {
                    otherPanelOpen = true;
                    ui.ShowOtherReached(() =>
                    {
                        // Callback opcional cuando se cierra el panel
                        otherPanelOpen = false;
                    });
                }

                // Si querés que el otro mundo también dispare algo (p.ej. generar tile),
                // podés agregarlo acá.

                // Reset del contador del "Otro mundo"
                otherProgress = 0f;
            }
        }

        if (fireGuard) fireGuard = false;
    }

    private void TryAppendTileFromNormal()
    {
        if (grid == null)
        {
            Debug.LogWarning("[ShiftingWorldMechanic] GridGenerator no asignado.");
            return;
        }
        grid.UI_AppendNext();
    }

    // Helpers para UI/depurar
    public void SetSpeed(float v) => speedPerSecond = Mathf.Max(0f, v);
    public string GetWorldName() => currentWorld == World.Normal ? "Normal" : "OtroMundo";
    public int GetNormalInt() => Mathf.Min(100, Mathf.FloorToInt(normalProgress));
    public int GetOtherInt() => Mathf.Min(100, Mathf.FloorToInt(otherProgress));
}
