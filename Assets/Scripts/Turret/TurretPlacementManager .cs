using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TurretPlacementManager : MonoBehaviour
{
    public static TurretPlacementManager Instance;

    [Header("Raycast")]
    [Tooltip("Capa de las celdas (opcional). Si no usás celdas, podés dejar ~0.")]
    public LayerMask cellLayer; // Asigná "Cell" si la usás

    [Header("Placement")]
    [Tooltip("Segundos de espera entre colocaciones.")]
    public float placementDelay = 0.5f;

    [Tooltip("Offset vertical del preview para evitar z-fighting.")]
    public float previewYOffset = 0.02f;

    private float nextAllowedPlacementTime = 0f;

    // Selección actual (ya no usamos TurretSelectionManager)
    private GameObject selectedPrefab;

    // Preview
    private GameObject previewInstance;
    private GameObject lastPreviewSource; // para evitar reinstanciar si no cambió el prefab
    private Material previewGreenMat;
    private Material previewRedMat;

    [Tooltip("Altura adicional al colocar torretas.")]
    public float placeYOffset = 0.6f;

    void Awake()
    {
        Instance = this;
        // Cache materiales de preview
        previewGreenMat = BuildPreviewMaterial(new Color(0.3f, 1f, 0.3f, 0.5f));
        previewRedMat = BuildPreviewMaterial(new Color(1f, 0.3f, 0.3f, 0.5f));
    }

    void Update()
    {
        // Si no hay prefab seleccionado o el mouse está sobre UI ⇒ esconder preview
        if (selectedPrefab == null ||
            (EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
        {
            HidePreview();
            return;
        }

        // Raycast
        if (Physics.Raycast(GetMouseRay(), out RaycastHit hit, 2000f, cellLayer.value == 0 ? ~0 : cellLayer))
        {
            // Si la celda tiene CellSlot y está ocupada ⇒ no se puede
            bool canPlaceByCell = true;
            var slot = hit.collider.GetComponentInParent<CellSlot>();
            if (slot != null)
                canPlaceByCell = slot.IsEmpty;

            bool finalCanPlace = canPlaceByCell && CanPlaceTurret();

            // Mover/crear preview
            Vector3 p = hit.point;
            p.y += placeYOffset + previewYOffset; // 0.6f + 0.02f
            ShowOrMovePreview(p, selectedPrefab, finalCanPlace);

            // Colocar con click izquierdo
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceAt(hit, slot, finalCanPlace);
            }
        }
        else
        {
            HidePreview();
        }
    }

    // ----------------- API pública -----------------

    public void SetSelectedPrefab(GameObject prefab)
    {
        selectedPrefab = prefab;
        // reiniciar preview para forzar regeneración con el nuevo prefab
        HidePreview();
    }

    public void ClearSelection()
    {
        selectedPrefab = null;
        HidePreview();
    }

    public bool CanPlaceTurret() => Time.time >= nextAllowedPlacementTime;

    // ----------------- Internals -----------------

    private void TryPlaceAt(RaycastHit hit, CellSlot slot, bool finalCanPlace)
    {
        if (!finalCanPlace || selectedPrefab == null) return;

        Vector3 pos;
        Transform parent = null;

        if (slot != null)
        {
            pos = slot.GetPlacePosition();
            parent = slot.transform;
        }
        else
        {
            pos = hit.point;
        }

        pos.y += placeYOffset; //aplicar offset

        var turret = Instantiate(selectedPrefab, pos, Quaternion.identity, parent);
        turret.name = $"{selectedPrefab.name}_Placed";

        if (slot != null)
            slot.Occupy(turret);

        RegisterPlacement();
        HidePreview();
    }
    private void RegisterPlacement()
    {
        nextAllowedPlacementTime = Time.time + placementDelay;
    }

    private Ray GetMouseRay() => Camera.main.ScreenPointToRay(Input.mousePosition);

    // ---------- Preview ----------

    private void ShowOrMovePreview(Vector3 position, GameObject sourcePrefab, bool canPlace)
    {
        // Si el prefab cambió, recrear
        if (previewInstance == null || lastPreviewSource != sourcePrefab)
        {
            HidePreview();
            previewInstance = Instantiate(sourcePrefab, position, Quaternion.identity);
            lastPreviewSource = sourcePrefab;

            // Desactivar colliders/scripts del preview
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            foreach (var mb in previewInstance.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        }

        // Mover y tintar
        previewInstance.transform.position = position;
        var mat = canPlace ? previewGreenMat : previewRedMat;
        foreach (var r in previewInstance.GetComponentsInChildren<Renderer>(true))
            r.material = mat;
    }

    public void HidePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
            lastPreviewSource = null;
        }
    }

    private Material BuildPreviewMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        return mat;
    }
}
