using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShiftingWorldUI : MonoBehaviour
{
    public enum PanelMode { Normal, Otro }

    [Header("Grid (para tiles)")]
    [SerializeField] private GridGenerator grid;

    [Header("Tile Selection")]
    [SerializeField] private GameObject tilePanelRoot;
    [SerializeField] private Button[] tileButtons;
    [SerializeField] private TMP_Text[] tileLabels;

    [Header("Turret Selection")]
    [SerializeField] private GameObject turretPanelRoot;
    [SerializeField] private Button[] turretButtons;
    [SerializeField] private TMP_Text[] turretLabels;

    [Header("Torretas (fuente)")]
    [SerializeField] private TurretDatabaseSO turretDatabase;
    [SerializeField] private List<TurretDataSO> turretPool = new();

    // === NUEVO: parámetros de colocación ===
    [Header("Placement")]
    [SerializeField] private Camera cam;               // si está vacío usa Camera.main
    [SerializeField] private LayerMask cellLayers = ~0; // filtros de raycast
    [SerializeField] private float turretYOffset = 0.5f;

    // === NUEVO: estado de colocación ===
    private TurretDataSO selectedTurret;  // torreta elegida
    private bool placingMode = false;     // estoy en modo colocación

    public event Action<TurretDataSO> OnTurretChosen;

    private Action onClosed;
    private PanelMode currentMode;

    private readonly List<TileLayout> currentTileOptions = new();
    private readonly List<TurretDataSO> currentTurretOptions = new();

    void Awake() => HideAll();

    void Update()
    {
        // --- Modo colocación activo: clic en Cell para instanciar ---
        if (!placingMode || selectedTurret == null) return;

        var cameraToUse = cam != null ? cam : Camera.main;
        if (cameraToUse == null) return;

        // Cancelar
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }
        // Click izquierdo para colocar
        if (Input.GetMouseButtonDown(0))
        {
            Camera c = cam ?? Camera.main;
            if (c == null) return;

            Ray ray = c.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
            {
                // Buscar el slot en el collider o en sus padres
                var slot = hit.collider.GetComponentInParent<CellSlot>();
                if (slot == null)
                {
                    Debug.Log("[ShiftingWorldUI] No hay CellSlot en el objeto cliqueado.");
                    return;
                }

                // (Opcional) Si querés seguir exigiendo el tag:
                // if (!slot.CompareTag("Cell")) return;  // solo si el slot está en el GO con tag

                if (selectedTurret == null || selectedTurret.prefab == null)
                {
                    Debug.LogWarning("[ShiftingWorldUI] La torreta seleccionada no tiene prefab.");
                    CancelPlacement();
                    return;
                }

                // Intentar colocar con offset Y
                if (slot.TryPlace(selectedTurret.prefab))
                {
                    Debug.Log("[ShiftingWorldUI] Torreta colocada.");
                    EndPlacement();
                }
                else
                {
                    Debug.Log("[ShiftingWorldUI] La celda figura ocupada.");
                }
            }
        }

    }

    private void EndPlacement()
    {
        placingMode = false;
        selectedTurret = null;
    }

    private void CancelPlacement()
    {
        Debug.Log("[ShiftingWorldUI] Colocación cancelada.");
        EndPlacement();
    }

    // -------- API que llama el Mechanic --------
    public void ShowNormalReached(Action closedCb = null)
    {
        currentMode = PanelMode.Normal;
        onClosed = closedCb;

        BuildNormalOptions();
        tilePanelRoot?.SetActive(true);
        turretPanelRoot?.SetActive(false);
    }

    public void ShowOtherReached(Action closedCb = null)
    {
        currentMode = PanelMode.Otro;
        onClosed = closedCb;

        BuildOtherOptions();
        tilePanelRoot?.SetActive(false);
        turretPanelRoot?.SetActive(true);
    }

    // -------- Construcción de opciones --------
    private void BuildNormalOptions()
    {
        currentTileOptions.Clear();

        if (grid == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] Grid no asignado.");
            SetButtonsDisabled(tileButtons, tileLabels, "Grid no asignado");
            return;
        }

        var candidates = grid.GetRandomCandidateSet(3);

        for (int i = 0; i < 3; i++)
        {
            var layout = (i < candidates.Count) ? candidates[i] : null;
            currentTileOptions.Add(layout);

            string label = layout != null ? layout.name : "N/D";
            int idx = i; // evitar captura de i
            BindButton(tileButtons, tileLabels, i, label, () => OnChooseTile(idx));
        }
    }

    private void BuildOtherOptions()
    {
        currentTurretOptions.Clear();

        var pool = new List<TurretDataSO>();
        if (turretDatabase != null && turretDatabase.allTurrets != null)
            foreach (var t in turretDatabase.allTurrets) if (t) pool.Add(t);
        if (turretPool != null)
            foreach (var t in turretPool) if (t) pool.Add(t);

        if (pool.Count == 0)
        {
            Debug.LogWarning("[ShiftingWorldUI] No hay torretas en la DB o turretPool.");
            SetButtonsDisabled(turretButtons, turretLabels, "Sin torretas");
            return;
        }

        Shuffle(pool);
        for (int i = 0; i < 3; i++)
        {
            var so = pool[i % pool.Count];
            currentTurretOptions.Add(so);

            string label = !string.IsNullOrEmpty(so.displayName) ? so.displayName : so.name;
            int idx = i; // evitar captura de i
            BindButton(turretButtons, turretLabels, i, label, () => OnChooseTurret(idx));
        }
    }

    // -------- Callbacks de elección --------
    private void OnChooseTile(int index)
    {
        var chosen = (index >= 0 && index < currentTileOptions.Count) ? currentTileOptions[index] : null;
        if (chosen == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] Opción de tile inválida.");
            Close();
            return;
        }

        bool ok = grid.AppendNextUsingSelectedExitWithLayout(chosen);
        if (!ok) Debug.LogWarning($"[ShiftingWorldUI] No se pudo colocar el layout: {chosen.name}");
        Close();
    }

    private void OnChooseTurret(int index)
    {
        var so = (index >= 0 && index < currentTurretOptions.Count) ? currentTurretOptions[index] : null;
        if (so == null || so.prefab == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] Opción de torreta inválida.");
        }
        else
        {
            // Guardamos y entramos en modo colocación (el panel se cierra)
            selectedTurret = so;
            placingMode = true;
            OnTurretChosen?.Invoke(so);
            Debug.Log($"[ShiftingWorldUI] Elegiste torreta: {(string.IsNullOrEmpty(so.displayName) ? so.name : so.displayName)} → Modo colocación activo.");
        }

        Close(); // cerramos panel pero seguimos en colocación
    }

    // -------- Utilidades de UI --------
    private void BindButton(Button[] buttons, TMP_Text[] labels, int index, string label, Action onClick)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return;
        var btn = buttons[index];
        if (!btn) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick());
        btn.interactable = true;
        btn.gameObject.SetActive(true);

        TMP_Text tmp = (labels != null && index < labels.Length) ? labels[index] : null;
        if (tmp == null) tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
        else
        {
            var legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy) legacy.text = label;
        }
    }

    private void SetButtonsDisabled(Button[] buttons, TMP_Text[] labels, string text)
    {
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (!btn) continue;

            btn.onClick.RemoveAllListeners();
            btn.interactable = false;
            btn.gameObject.SetActive(true);

            TMP_Text tmp = (labels != null && i < labels.Length) ? labels[i] : null;
            if (tmp == null) tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp) tmp.text = text;
            else
            {
                var legacy = btn.GetComponentInChildren<Text>(true);
                if (legacy) legacy.text = text;
            }
        }
    }

    public void Close()
    {
        HideAll();
        onClosed?.Invoke();
        onClosed = null;
        currentTileOptions.Clear();
        currentTurretOptions.Clear();
    }

    private void HideAll()
    {
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        if (turretPanelRoot) turretPanelRoot.SetActive(false);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        var rng = new System.Random();
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
