using UnityEngine;

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
        if (!_placing) return;

        var cameraToUse = cam != null ? cam : Camera.main;
        if (cameraToUse == null) return;

        // Cancelar
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitPlacementMode();
            return;
        }

        // Raycast al mouse
        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 500f, cellLayers))
        {
            var go = hit.collider.gameObject;

            // filtrar por tag "Cell"
            if (go.CompareTag("Cell"))
            {
                // preview/ghost
                if (_ghost != null)
                {
                    _ghost.transform.position = hit.collider.bounds.center + ghostOffset;
                }

                // click izquierdo = colocar
                if (Input.GetMouseButtonDown(0))
                {
                    // asegurar CellSlot
                    var slot = go.GetComponent<CellSlot>();
                    if (slot == null) slot = go.AddComponent<CellSlot>();

                    // obtener prefab desde el SO
                    GameObject turretPrefab = _selectedTurret != null ? _selectedTurret.prefab : null;
                    if (turretPrefab == null)
                    {
                        Debug.LogWarning("[TurretPlacer] El SO de la torreta no tiene prefab asignado.");
                        return;
                    }

                    if (slot.TryPlace(turretPrefab))
                    {
                        Debug.Log($"[TurretPlacer] Torreta colocada en {go.name}");
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
                if (_ghost != null) _ghost.transform.position = hit.point + ghostOffset;
            }
        }
        else
        {
            if (_ghost != null) _ghost.transform.position = cameraToUse.transform.position + cameraToUse.transform.forward * 2f;
        }
    }
}
