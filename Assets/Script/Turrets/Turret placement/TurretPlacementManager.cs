using UnityEngine;

public class TurretPlacementManager : MonoBehaviour
{
    public static TurretPlacementManager Instance;

    private GameObject previewInstance;

    public float placementDelay = 1.0f;
    private float nextAllowedPlacementTime = 0f;

    void Awake()
    {
        Instance = this;
    }

    public bool CanPlaceTurret()
    {
        return Time.time >= nextAllowedPlacementTime &&
               TurretSelectionManager.Instance.selectedTurret != null;
    }


    public void RegisterPlacement()
    {
        nextAllowedPlacementTime = Time.time + placementDelay;
    }
    public bool TryPlaceTurret()
    {
        if (!CanPlaceTurret())
        {
            Debug.Log("No se puede colocar torreta todavía.");
            return false;
        }

        RegisterPlacement();
        return true;
    }
    // Ahora el método recibe el color (verde o rojo)
    private Material CreatePreviewMaterial(bool canPlace)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color color = canPlace ? new Color(0.3f, 1f, 0.3f, 0.5f) : new Color(1f, 0.3f, 0.3f, 0.5f); // Verde o rojo, 50% opacidad
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
    public void ShowPreview(Vector3 position, TurretSelection selection, bool canPlace)
    {
        // Buscá los datos de la torreta seleccionada
        var data = TurretCostManager.Instance.GetCurrentCost(selection.turretId);

        // Validá si hay oro suficiente
        bool hasGold = GoldManager.Instance.HasEnoughGold(data);

        // El preview solo puede ser verde si ambas condiciones se cumplen
        bool finalCanPlace = canPlace && hasGold;

        //Debug.Log($"ShowPreview: canPlace={canPlace}, oro={GoldManager.Instance.currentGold}");

        if (selection == null || selection.turretPrefab == null)
            return;

        // Destruir preview anterior
        if (previewInstance != null)
            Destroy(previewInstance);

        previewInstance = Instantiate(selection.turretPrefab, position, Quaternion.identity);

        // Desactivar colliders y scripts
        foreach (var collider in previewInstance.GetComponentsInChildren<Collider>())
            collider.enabled = false;
        foreach (var comp in previewInstance.GetComponentsInChildren<MonoBehaviour>())
            comp.enabled = false;

        // Crear material transparente según si se puede o no
        Material previewMaterial = CreatePreviewMaterial(finalCanPlace);

        // Asignar material a todos los renderers del preview
        foreach (var renderer in previewInstance.GetComponentsInChildren<Renderer>())
            renderer.material = previewMaterial;
    }


    public void HidePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }
}