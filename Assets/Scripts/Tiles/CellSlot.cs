using UnityEngine;

[DisallowMultipleComponent]
public class CellSlot : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Transform parentForTurrets;
    [SerializeField] private float extraYOffset = 0f;

    [Header("State (runtime)")]
    [SerializeField] private bool occupied = false;
    [SerializeField] private GameObject currentTurret;

    public bool TryPlace(GameObject turretPrefab, float yOffset = 0f)
    {
        if (occupied || turretPrefab == null) return false;

        var col = GetComponent<Collider>();
        if (!col)
        {
            Debug.LogWarning("[CellSlot] La celda no tiene Collider.");
            return false;
        }

        var b = col.bounds;
        Vector3 topCenter = new Vector3(b.center.x, b.max.y, b.center.z);

        var parent = parentForTurrets ? parentForTurrets : transform;
        currentTurret = Instantiate(turretPrefab, parent);
        currentTurret.transform.position = topCenter;

        var rend = currentTurret.GetComponentInChildren<Renderer>();
        if (rend)
        {
            float targetBottom = topCenter.y + yOffset + extraYOffset;
            float delta = targetBottom - rend.bounds.min.y;
            currentTurret.transform.position += Vector3.up * delta;
        }
        else
        {
            currentTurret.transform.position += Vector3.up * (yOffset + extraYOffset);
        }

        occupied = true;
        return true;
    }

    public bool TryRemove()
    {
        if (!occupied || !currentTurret) return false;
        Destroy(currentTurret);
        currentTurret = null;
        occupied = false;
        return true;
    }

    public bool IsOccupied => occupied;
    public GameObject GetCurrentTurret() => currentTurret;
}
