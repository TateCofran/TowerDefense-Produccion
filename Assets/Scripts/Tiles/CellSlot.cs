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

    [SerializeField] private ITurretDupeSystem dupeSystem;

    private void Start()
    {
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
    }
    public bool TryPlace(GameObject turretPrefab, TurretDataSO turretData = null)
    {
        if (occupied) return false;

        var turret = Instantiate(turretPrefab, transform.position, Quaternion.identity);
        currentTurret = turret;
        occupied = true;

        // Configurar el data holder si existe
        var dataHolder = turret.GetComponent<TurretDataHolder>();
        if (dataHolder != null && turretData != null)
        {
            dataHolder.ApplyDataSO(turretData);
        }

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
