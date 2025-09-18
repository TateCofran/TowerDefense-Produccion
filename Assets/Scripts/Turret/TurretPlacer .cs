using UnityEngine;
using UnityEngine.EventSystems;

public class TurretPlacer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShiftingWorldUI ui;      // arrastra tu ShiftingWorldUI
    [SerializeField] private Camera cam;              // si es null usa Camera.main

    [Header("Preview (opcional)")]
    [SerializeField] private GameObject ghostPrefab;  // prefab visual para preview
    [SerializeField] private Vector3 ghostOffset = new Vector3(0f, 0.01f, 0f);

    [Header("Raycast")]
    [SerializeField] private LayerMask cellLayers = ~0;  // qué capas rayo detecta

    private TurretDataSO _selectedTurret;
    private GameObject _ghost;
    private bool _placing;

    [SerializeField] private bool removeWithMiddleClick = true;

    private bool _removing;
    private void Awake()
    {
        if (ui != null) ui.OnTurretChosen += HandleTurretChosen;
    }
    private void OnDestroy()
    {
        if (ui != null) ui.OnTurretChosen -= HandleTurretChosen;
    }

    private void HandleTurretChosen(TurretDataSO so)
    {
        _selectedTurret = so;
        EnterPlacementMode();
    }

    private void EnterPlacementMode()
    {
        _placing = (_selectedTurret != null);
        if (_placing && ghostPrefab != null)
        {
            if (_ghost != null) Destroy(_ghost);
            _ghost = Instantiate(ghostPrefab);
        }
    }

    private void ExitPlacementMode()
    {
        _placing = false;
        _selectedTurret = null;
        if (_ghost != null) Destroy(_ghost);
    }

    private void Update()
    {
        if (removeWithMiddleClick && Input.GetMouseButtonDown(2)   // clic medio
            || (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))) // Shift+Click
        {
            var camRemove = cam ? cam : Camera.main;
            if (!camRemove) return;

            // Evitá borrar si el puntero está sobre la UI
            if (UnityEngine.EventSystems.EventSystem.current &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray rayRemove = camRemove.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(rayRemove, out var hitRemove, 500f, cellLayers))
            {
                var slot = hitRemove.collider.GetComponentInParent<CellSlot>();
                if (slot != null && slot.IsOccupied)
                {
                    if (slot.TryRemove())
                        Debug.Log("[TurretPlacer] Torreta eliminada.");
                }
            }

            // Si estabas viendo el ghost de colocar, ocultalo
            if (_ghost) _ghost.SetActive(false);
            return; // importante: no sigas con la lógica de colocar este frame
        }
        if (!_placing) return;

        var cameraToUse = cam ? cam : Camera.main;
        if (!cameraToUse) return;

        // Cancelar
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }

        // Evitar clicks a través de la UI
        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
        {
            if (_ghost) _ghost.SetActive(false);
            return;
        }

        // Raycast al mouse
        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
        {
            // Buscar slot en el objeto golpeado o sus padres
            var slot = hit.collider.GetComponentInParent<CellSlot>();

            if (slot != null && slot.enabled && !slot.IsOccupied)
            {
                // Mostrar y posicionar ghost EXACTAMENTE en donde se va a colocar
                if (_ghost)
                {
                    _ghost.SetActive(true);
                    var slotCol = slot.GetComponent<Collider>();
                    var b = (slotCol ? slotCol.bounds : hit.collider.bounds);
                    Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);
                    _ghost.transform.position = topCenter + ghostOffset;
                }

                // Click izquierdo = colocar
                if (Input.GetMouseButtonDown(0))
                {
                    var prefab = _selectedTurret ? _selectedTurret.prefab : null;
                    if (!prefab) return;

                    if (slot.TryPlace(prefab)) // tu TryPlace ya coloca en topCenter
                    {
                        Debug.Log($"[TurretPlacer] Torreta colocada en {slot.name}");
                        ExitPlacementMode();
                    }
                    else
                    {
                        Debug.Log("Esa celda ya está ocupada.");
                    }
                }
            }
            else
            {
                // No es celda válida u ocupada: ocultar ghost
                if (_ghost) _ghost.SetActive(false);
            }
        }
        else
        {
            // Nada bajo el mouse: ocultar ghost
            if (_ghost) _ghost.SetActive(false);
        }
    }
    public void EnterRemoveMode()
    {
        _placing = false; // por si acaso
        _removing = true;
        if (_ghost) _ghost.SetActive(false);
    }

    public void ExitRemoveMode()
    {
        _removing = false;
    }

}
