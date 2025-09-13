using UnityEngine;

public class ShiftingWorldMechanic : MonoBehaviour
{
    public enum World { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;

    [Header("Estado")]
    [SerializeField] private World currentWorld = World.Normal;

    [Header("Progreso (0..100)")]
    [SerializeField] private float normalProgress = 0f;
    [SerializeField] private float otherProgress = 0f;

    [Header("Ajustes")]
    [SerializeField] private float speedPerSecond = 20f; // cuánto “suma” por segundo
    [SerializeField] private KeyCode toggleKey = KeyCode.J;

    private bool fireGuard = false;   // evita doble disparo en el mismo frame

    void Update()
    {
        // Cambio de mundo
        if (Input.GetKeyDown(toggleKey))
        {
            currentWorld = (currentWorld == World.Normal) ? World.Otro : World.Normal;
            Debug.Log($"[ShiftingWorldMechanic] Cambié de mundo → {currentWorld}");
            // Nota: por tu diseño anterior, el contador del mundo activo se resetea al cambiar:
            if (currentWorld == World.Normal) normalProgress = 0f;
            else otherProgress = 0f;
        }

        // Avance del mundo activo
        if (currentWorld == World.Normal)
        {
            if (normalProgress < 100f)
                normalProgress = Mathf.Min(100f, normalProgress + speedPerSecond * Time.deltaTime);

            // Al llegar a 100 → generamos tile y reseteamos
            if (!fireGuard && Mathf.FloorToInt(normalProgress) >= 100)
            {
                fireGuard = true;
                TryAppendTileFromNormal();
                normalProgress = 0f; // por tu requisito “al instanciar, se reinicia”
            }
        }
        else
        {
            if (otherProgress < 100f)
                otherProgress = Mathf.Min(100f, otherProgress + speedPerSecond * Time.deltaTime);

            // (Si quisieras que el OTRO mundo también genere tiles, duplicá lógica aquí)
        }

        // Liberar guard cada frame
        if (fireGuard) fireGuard = false;
    }

    private void TryAppendTileFromNormal()
    {
        if (grid == null)
        {
            Debug.LogWarning("[ShiftingWorldMechanic] GridGenerator no asignado.");
            return;
        }

        // Si no hay exits disponibles, no hace nada (GridGenerator ya loguea el motivo)
        grid.UI_AppendNext();
    }

    // Opcional: helpers para UI/depurar
    public void SetSpeed(float v) => speedPerSecond = Mathf.Max(0f, v);
    public string GetWorldName() => currentWorld == World.Normal ? "Normal" : "OtroMundo";
    public int GetNormalInt() => Mathf.Min(100, Mathf.FloorToInt(normalProgress));
    public int GetOtherInt() => Mathf.Min(100, Mathf.FloorToInt(otherProgress));
}
