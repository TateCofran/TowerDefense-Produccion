using UnityEngine;

[DisallowMultipleComponent]
public class CellSlot : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private Transform parentForTurrets;   // opcional: dónde parentear torretas
    [SerializeField] private float extraYOffset = 0.01f;   // leve separación para evitar z-fighting

    [Header("State (runtime)")]
    [SerializeField] private bool occupied = false;
    [SerializeField] private GameObject currentTurret;

    [SerializeField] private ITurretDupeSystem dupeSystem;

    private Collider _cellCollider;      // collider de la celda (o hijo)
    private Transform _topAnchor;        // opcional: si querés anclar manualmente la “tapa” de la celda

    private void Awake()
    {
        // Intentamos cachear un collider válido para medir la tapa (bounds.max.y)
        _cellCollider = GetComponent<Collider>();
        if (!_cellCollider) _cellCollider = GetComponentInChildren<Collider>();

        // Si querés, podés poner un hijo llamado "TopAnchor" para definir la tapa exacta
        var ta = transform.Find("TopAnchor");
        if (ta) _topAnchor = ta;
    }

    private void Start()
    {
        dupeSystem = FindFirstObjectByType<TurretDupeSystem>();
    }

    /// <summary>
    /// Punto donde apoyar la torreta (centro de la tapa de la celda).
    /// Usa TopAnchor si existe, si no usa bounds del collider.
    /// </summary>
    private Vector3 GetTopCenter()
    {
        if (_topAnchor) return _topAnchor.position;

        if (_cellCollider)
        {
            var b = _cellCollider.bounds;
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }

        // Fallback: transform.position (no ideal, pero evita nulls)
        return transform.position;
    }

    /// <summary>
    /// Devuelve mitad de altura visual del GO combinando todos los Renderers;
    /// si no hay, intenta con Colliders. Sirve cuando el pivot está al centro.
    /// </summary>
    private static float GetVisualHalfHeight(GameObject go)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            return b.size.y * 0.5f;
        }

        var cols = go.GetComponentsInChildren<Collider>();
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b.size.y * 0.5f;
        }

        return 0.5f; // valor por defecto si no hay nada
    }

    /// <summary>
    /// Coloca la torreta apoyando su base sobre la tapa de la celda.
    /// Si el prefab tiene un hijo "BaseAnchor", se alinea exactamente con la tapa.
    /// Si no tiene, se calcula por altura visual.
    /// </summary>
    public bool TryPlace(GameObject turretPrefab, TurretDataSO turretData = null)
    {
        if (occupied || !turretPrefab) return false;

        Vector3 top = GetTopCenter();

        // Instanciar parentado si corresponde (posición provisoria, se corrige luego)
        Transform parent = parentForTurrets ? parentForTurrets : null;
        GameObject turret = Instantiate(turretPrefab, top, Quaternion.identity, parent);

        // 1) ¿Tiene BaseAnchor?
        Transform baseAnchor = turret.transform.Find("BaseAnchor");
        if (baseAnchor != null)
        {
            // Queremos que baseAnchor quede EXACTAMENTE en la tapa + extraYOffset
            float deltaY = (top.y + extraYOffset) - baseAnchor.position.y;
            Vector3 p = turret.transform.position;
            turret.transform.position = new Vector3(top.x, p.y + deltaY, top.z);
        }
        else
        {
            // 2) Sin ancla: apoyar por media altura visual
            float halfH = GetVisualHalfHeight(turret);
            float targetY = top.y + halfH + extraYOffset;
            turret.transform.position = new Vector3(top.x, targetY, top.z);
        }

        // Guardar estado
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
        // Protegemos contra referencia destruida
        if (!occupied || !currentTurret) { occupied = false; currentTurret = null; return false; }

        Destroy(currentTurret);
        currentTurret = null;
        occupied = false;
        return true;
    }

    public bool IsOccupied => occupied;
    public GameObject GetCurrentTurret() => currentTurret;
}
