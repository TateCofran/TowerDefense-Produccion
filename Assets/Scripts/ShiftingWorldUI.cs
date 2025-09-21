using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ShiftingWorldMechanic;

public class ShiftingWorldUI : MonoBehaviour
{
    public enum PanelMode { Normal, Otro, TilePlacement }

    [Header("Grid (para tiles)")]
    [SerializeField] private GridGenerator grid;

    [Header("Tile Selection")]
    [SerializeField] private GameObject tilePanelRoot;
    [SerializeField] private Button[] tileButtons;
    [SerializeField] private TMP_Text[] tileLabels;
    private bool tileChoiceLocked = false;

    [Header("Turret Selection")]
    [SerializeField] private GameObject turretPanelRoot;
    [SerializeField] private Button[] turretButtons;
    [SerializeField] private TMP_Text[] turretLabels;

    [Header("Exit Buttons")]
    [SerializeField] private GameObject exitButtonPrefab;
    [SerializeField] private Canvas exitButtonsCanvas;
    private List<Button> exitButtons = new List<Button>();

    [Header("Torretas (fuente)")]
    [SerializeField] private TurretDatabaseSO turretDatabase;
    [SerializeField] private List<TurretDataSO> turretPool = new();

    [Header("Placement")]
    [SerializeField] private Camera cam;
    [SerializeField] private LayerMask cellLayers = ~0;
    [SerializeField] private float turretYOffset = 0.5f;

    [Header("Sistema de Dupes")]
    [SerializeField] private bool enableDupesSystem = true;

    private ITurretDupeSystem dupeSystem;

    public event Action<World> OnTurretPlacedSuccessfully;
    public event Action<TurretDataSO, TurretLevelData> OnTurretLevelUp;

    // Estado de colocación
    private TurretDataSO selectedTurret;
    private TileLayout selectedTileLayout;
    private bool placingMode = false;
    private bool tilePlacingMode = false;

    public event Action<TurretDataSO> OnTurretChosen;

    private Action onClosed;
    private PanelMode currentMode;

    private readonly List<TileLayout> currentTileOptions = new();
    private readonly List<TurretDataSO> currentTurretOptions = new();

    void Awake()
    {
        HideAll();
    }
    private void Start()
    {
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
    }
    void Update()
    {
        HandleTurretPlacementMode();

        // Rotar botones de exit para que miren a la cámara
        if (tilePlacingMode && exitButtonsCanvas != null && cam != null)
        {
            exitButtonsCanvas.transform.rotation = Quaternion.LookRotation(exitButtonsCanvas.transform.position - cam.transform.position);
        }
    }
    // En el método donde manejas los dupes
    private void HandleDupeSystem(TurretDataSO turret)
    {
        if (!enableDupesSystem || dupeSystem == null) return;

        // Guardar el nivel anterior para detectar si subió de nivel
        var previousLevelData = dupeSystem.GetTurretLevelData(turret);
        int previousLevel = previousLevelData.currentLevel;

        // Agregar dupe
        dupeSystem.AddDupe(turret);

        // Obtener los nuevos datos de nivel
        var newLevelData = dupeSystem.GetTurretLevelData(turret);

        // Verificar si subió de nivel
        if (newLevelData.currentLevel > previousLevel)
        {
            HandleTurretLevelUp(turret, newLevelData);
        }
    }
    private void HandleTurretLevelUp(TurretDataSO turret, TurretLevelData levelData)
    {
        Debug.Log($" {turret.displayName} subió al nivel {levelData.currentLevel}!");

        // Opcional: puedes mostrar un efecto visual o sonido aquí
        // ShowLevelUpEffect(turret, levelData);

        // Disparar el evento
        OnTurretLevelUp?.Invoke(turret, levelData);
    }
    private void HandleTurretPlacementMode()
    {
        if (!placingMode) return;

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Camera c = cam ?? Camera.main;
            if (c == null) return;

            Ray ray = c.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
            {
                var slot = hit.collider.GetComponentInParent<CellSlot>();
                if (slot == null) return;

                if (selectedTurret == null || selectedTurret.prefab == null)
                {
                    CancelPlacement();
                    return;
                }

                if (slot.TryPlace(selectedTurret.prefab, selectedTurret))
                {
                    Debug.Log("[ShiftingWorldUI] Torreta colocada exitosamente.");

                    // Notificar que la torreta fue colocada exitosamente
                    OnTurretPlacedSuccessfully?.Invoke(GetCurrentWorld());

                    EndPlacement();
                    Close(); // ← Solo cerrar después de colocar exitosamente
                }
                else
                {
                    Debug.Log("[ShiftingWorldUI] La celda está ocupada.");
                    // No cerrar el UI, permitir intentar otra celda
                }
            }
        }
    }
    // -------- API pública --------
    public void ShowNormalReached(Action closedCb = null)
    {
        tileChoiceLocked = false;
        currentMode = PanelMode.Normal;
        onClosed = closedCb;
        BuildNormalOptions();
        tilePanelRoot?.SetActive(true);
        turretPanelRoot?.SetActive(false);
        HideExitButtons();
    }
    private void EnterTurretPlacementMode()
    {
        // Ocultar panel de selección, pero mantener el UI activo para colocación
        turretPanelRoot?.SetActive(false);

        // Mostrar mensaje o indicador visual de modo colocación
        Debug.Log("Modo colocación: Haz clic en una celda para colocar la torreta");

        // Opcional: puedes agregar un UI de feedback para el modo colocación
        // ShowPlacementUI();
    }
    private World GetCurrentWorld()
    {
        // Necesitas determinar si estás en mundo Normal u Otro
        // Basado en tu currentMode o otra lógica
        return currentMode == PanelMode.Normal ? World.Normal : World.Otro;
    }
    public void ShowOtherReached(Action closedCb = null)
    {
        currentMode = PanelMode.Otro;
        onClosed = closedCb;
        BuildOtherOptions();
        tilePanelRoot?.SetActive(false);
        turretPanelRoot?.SetActive(true);
        HideExitButtons();
    }

    // -------- Lógica de selección de tile --------
    private void OnChooseTile(int index)
    {
        if (tileChoiceLocked) return;
        tileChoiceLocked = true;

        var chosen = (index >= 0 && index < currentTileOptions.Count) ? currentTileOptions[index] : null;
        if (chosen == null)
        {
            Close();
            return;
        }

        selectedTileLayout = chosen;
        EnterTilePlacementMode();
    }

    private void EnterTilePlacementMode()
    {
        currentMode = PanelMode.TilePlacement;
        tilePlacingMode = true;

        // Ocultar panel de selección, mostrar botones de exit
        tilePanelRoot?.SetActive(false);
        CreateExitButtons();
    }

    private void CreateExitButtons()
    {
        // Limpiar botones existentes
        ClearExitButtons();

        var exits = grid.GetAvailableExits();
        Camera mainCamera = cam ?? Camera.main;

        foreach (var (label, worldPos) in exits)
        {
            // Crear botón en la posición mundial del exit
            if (exitButtonPrefab != null && exitButtonsCanvas != null)
            {
                GameObject buttonObj = Instantiate(exitButtonPrefab, exitButtonsCanvas.transform);
                Button button = buttonObj.GetComponent<Button>();

                if (button != null)
                {
                    // Posicionar el botón en la posición mundial del exit
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos + Vector3.up * 1f);
                    button.transform.position = screenPos;

                    // Configurar texto
                    TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = label;
                    }

                    // Configurar evento de clic
                    string exitLabel = label;
                    button.onClick.AddListener(() => OnExitButtonClicked(exitLabel));

                    exitButtons.Add(button);
                }
            }
        }

        exitButtonsCanvas?.gameObject.SetActive(true);
    }

    private void OnExitButtonClicked(string exitLabel)
    {
        if (selectedTileLayout == null) return;

        // Encontrar el índice del exit por su label
        int exitIndex = FindExitIndexByLabel(exitLabel);
        if (exitIndex == -1) return;

        // Configurar el grid generator para usar el exit seleccionado
        grid.UI_SetExitByLabel(exitLabel);

        // Colocar el tile
        bool ok = grid.AppendNextUsingSelectedExitWithLayout(selectedTileLayout);

        if (ok)
        {
            Debug.Log($"[ShiftingWorldUI] Tile {selectedTileLayout.name} colocado exitosamente en exit {exitLabel}");
        }
        else
        {
            Debug.LogWarning($"[ShiftingWorldUI] No se pudo colocar el tile: {selectedTileLayout.name}");
        }

        ExitTilePlacementMode();
        Close();
    }

    private int FindExitIndexByLabel(string label)
    {
        var exits = grid.GetAvailableExits();
        for (int i = 0; i < exits.Count; i++)
        {
            if (exits[i].label == label)
                return i;
        }
        return -1;
    }

    private void ClearExitButtons()
    {
        foreach (var button in exitButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }
        exitButtons.Clear();
    }

    private void HideExitButtons()
    {
        exitButtonsCanvas?.gameObject.SetActive(false);
        ClearExitButtons();
    }

    private void ExitTilePlacementMode()
    {
        tilePlacingMode = false;
        selectedTileLayout = null;
        HideExitButtons();
    }

    // -------- Métodos existentes --------
    private void BuildNormalOptions()
    {
        currentTileOptions.Clear();
        if (grid == null) return;

        var candidates = grid.GetRandomCandidateSet(3);
        for (int i = 0; i < 3; i++)
        {
            var layout = (i < candidates.Count) ? candidates[i] : null;
            currentTileOptions.Add(layout);
            string label = layout != null ? layout.name : "N/D";
            int idx = i;
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
            return;
        }

        Shuffle(pool);
        for (int i = 0; i < 3; i++)
        {
            var so = pool[i % pool.Count];
            currentTurretOptions.Add(so);

            string label = GetTurretDisplayName(so); // ← Usa el método que muestra el nivel
            int idx = i;
            BindButton(turretButtons, turretLabels, i, label, () => OnChooseTurret(idx));
        }
    }

    private void OnChooseTurret(int index)
    {
        var so = (index >= 0 && index < currentTurretOptions.Count) ? currentTurretOptions[index] : null;
        if (so == null || so.prefab == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] Opción de torreta inválida.");
            Close();
            return;
        }

        // Manejar sistema de dupes
        HandleDupeSystem(so);

        selectedTurret = so;
        placingMode = true;
        OnTurretChosen?.Invoke(so);

        // NO cerrar el panel aquí, solo entrar en modo colocación
        Debug.Log($"[ShiftingWorldUI] Elegiste torreta: {so.displayName}. Selecciona una celda para colocarla.");

        // Cambiar a modo colocación
        EnterTurretPlacementMode();
    }
    private string GetTurretDisplayName(TurretDataSO turret)
    {
        if (!enableDupesSystem || dupeSystem == null)
            return !string.IsNullOrEmpty(turret.displayName) ? turret.displayName : turret.name;

        var levelData = dupeSystem.GetTurretLevelData(turret);
        return $"{(!string.IsNullOrEmpty(turret.displayName) ? turret.displayName : turret.name)}\n{levelData.GetStatusText()}";
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
        Close(); // Cerrar el UI al cancelar
    }

    // -------- Métodos de UI --------
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
    }

    public void Close()
    {
        HideAll();
        onClosed?.Invoke();
        onClosed = null;
        currentTileOptions.Clear();
        currentTurretOptions.Clear();
        ExitTilePlacementMode();
        EndPlacement();
    }

    private void HideAll()
    {
        if (tilePanelRoot) tilePanelRoot.SetActive(false);
        if (turretPanelRoot) turretPanelRoot.SetActive(false);
        HideExitButtons();
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