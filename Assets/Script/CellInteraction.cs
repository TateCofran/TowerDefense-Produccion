using UnityEngine;

public class CellInteraction : MonoBehaviour
{
    public static CellInteraction hoveredCell;

    private bool hasTurret = false;
    private Renderer cellRenderer;
    private Color originalColor;

    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f;

    void Start()
    {
        cellRenderer = GetComponent<Renderer>();

        if (cellRenderer != null)
        {
            originalColor = cellRenderer.material.color;
        }
        else
        {
            Debug.LogError("CellInteraction: No se encontró el Renderer en " + gameObject.name);
        }
    }

    void OnMouseEnter()
    {
        hoveredCell = this;
        RefreshPreview();
    }


    void OnMouseExit()
    {
        if (hoveredCell == this)
            hoveredCell = null;

        TurretPlacementManager.Instance.HidePreview();
    }

    void OnMouseDown()
    {
        float timeSinceLastClick = Time.unscaledTime - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            if (!hasTurret && CanPlaceTurret() && TurretPlacementManager.Instance.CanPlaceTurret())
            {
                var selection = TurretSelectionManager.Instance.selectedTurret;
                if (selection == null) return;

                var turretData = TurretDatabase.Instance.GetTurretData(selection.turretId);
                if (turretData == null) return;

                int currentCost = TurretCostManager.Instance.GetCurrentCost(turretData.id);
                if (!GoldManager.Instance.HasEnoughGold(currentCost)) return;

                GoldManager.Instance.SpendGold(currentCost);

                GameObject newTurretGO = Instantiate(selection.turretPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                hasTurret = true;

                var cellLink = newTurretGO.GetComponent<TurretCellLink>();
                cellLink?.SetCell(this);

                var dataHolder = newTurretGO.GetComponent<TurretDataHolder>();
                dataHolder?.ApplyData(turretData);

                TurretManager.Instance.RegisterTurret(newTurretGO.GetComponent<Turret>());

                TurretPlacementManager.Instance.RegisterPlacement();
                TurretTooltipUI.Instance.RefreshIfVisible(turretData.id);

                TurretCostManager.Instance.OnTurretPlaced(turretData.id);

                TurretPlacementManager.Instance.HidePreview(); // Oculta el preview al colocar
            }
        }

        lastClickTime = Time.unscaledTime;
    }

    public void RefreshPreview()
    {
        var selection = TurretSelectionManager.Instance?.selectedTurret;
        bool canPlace = false;

        if (selection == null || hasTurret || CompareTag("Path") || CompareTag("Core"))
        {
            TurretPlacementManager.Instance.HidePreview();
            return;
        }
        if (string.IsNullOrEmpty(selection.turretId))
        {
            TurretPlacementManager.Instance.HidePreview();
            return;
        }
        var data = TurretDatabase.Instance?.GetTurretData(selection.turretId);
        if (data != null && GoldManager.Instance.HasEnoughGold(data.cost))
        {
            canPlace = true;
        }
        TurretPlacementManager.Instance.ShowPreview(transform.position + Vector3.up * 0.5f, selection, canPlace);
    }


    bool CanPlaceTurret()
    {
        return !hasTurret && !CompareTag("Path") && !CompareTag("Core");
    }

    public void RemoveTurret(string turretId)
    {
        hasTurret = false;
        TurretCostManager.Instance.OnTurretRemoved(turretId);
    }

}
