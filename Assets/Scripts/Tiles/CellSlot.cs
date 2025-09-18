using UnityEngine;

[DisallowMultipleComponent]
public class CellSlot : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Transform parentForTurrets; // opcional
    [SerializeField] private float extraYOffset = 0f;    // margen fino opcional

    [Header("State (runtime)")]
    [SerializeField] private bool occupied = false;
    [SerializeField] private GameObject currentTurret;

    /// <summary>
    /// Coloca la torreta apoyada sobre la cara superior del collider de la celda.
    /// Si el pivot del prefab no está en la base, corrige usando Renderer.bounds.
    /// </summary>
    public bool TryPlace(GameObject turretPrefab, float yOffset = 0f)
    {
        if (occupied || turretPrefab == null) return false;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("[CellSlot] La celda no tiene Collider.");
            return false;
        }

        // 1) Cara superior de la celda
        var b = col.bounds;
        Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);

        // 2) Instanciar
        var parent = parentForTurrets ? parentForTurrets : transform;
        currentTurret = Instantiate(turretPrefab, parent);
        currentTurret.transform.position = topCenter;

        // 3) Corregir por pivot usando el Renderer real
        var rend = currentTurret.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            float targetBottom = topCenter.y + yOffset + extraYOffset;
            float delta = targetBottom - rend.bounds.min.y;
            currentTurret.transform.position += Vector3.up * delta;
        }
        else
        {
            // Fallback: subir un poco igual
            currentTurret.transform.position += Vector3.up * (yOffset + extraYOffset);
        }

        occupied = true;
        return true;
    }

    public void Clear()
    {
        if (currentTurret) Destroy(currentTurret);
        currentTurret = null;
        occupied = false;
    }

    public bool IsOccupied => occupied;
    public GameObject GetCurrentTurret() => currentTurret;
}
