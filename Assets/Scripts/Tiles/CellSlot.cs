using UnityEngine;

[DisallowMultipleComponent]
public class CellSlot : MonoBehaviour
{
    [Tooltip("Si se asigna, la torreta se colocará exactamente aquí; si no, usa transform.position.")]
    public Transform anchor;

    private GameObject placedTurret;

    /// <summary>¿La celda está libre?</summary>
    public bool IsEmpty => placedTurret == null;

    /// <summary>Posición final donde instanciar la torreta.</summary>
    public Vector3 GetPlacePosition()
        => anchor ? anchor.position : transform.position;

    /// <summary>Marca la celda como ocupada por esta torreta (sin instanciar).</summary>
    public void Occupy(GameObject turret)
    {
        placedTurret = turret;
    }

    /// <summary>Libera la celda.</summary>
    public void Vacate()
    {
        placedTurret = null;
    }

    // =========================
    //  NUEVO: API TryPlace
    // =========================

    /// <summary>
    /// Intenta colocar una torreta instanciando el prefab. Devuelve true si pudo.
    /// </summary>
    public bool TryPlace(GameObject turretPrefab)
    {
        GameObject _;
        return TryPlace(turretPrefab, out _);
    }

    /// <summary>
    /// Intenta colocar una torreta instanciando el prefab. Devuelve true y el objeto instanciado si pudo.
    /// </summary>
    public bool TryPlace(GameObject turretPrefab, out GameObject placed)
    {
        return TryPlace(turretPrefab, out placed, Quaternion.identity, null);
    }

    /// <summary>
    /// Intenta colocar una torreta instanciando el prefab con rotación y parent opcional.
    /// </summary>
    public bool TryPlace(GameObject turretPrefab, out GameObject placed, Quaternion rotation, Transform explicitParent)
    {
        placed = null;

        if (!IsEmpty || turretPrefab == null)
            return false;

        Vector3 pos = GetPlacePosition();
        Transform parent = explicitParent ? explicitParent : transform;

        placed = Object.Instantiate(turretPrefab, pos, rotation, parent);
        placed.name = $"{turretPrefab.name}_@{name}";

        Occupy(placed);
        return true;
    }

    /// <summary>
    /// Variante para proyectos que ya instancian la torreta afuera y solo quieren ocupar la celda.
    /// </summary>
    public bool TryPlaceExisting(GameObject alreadyInstantiated)
    {
        if (!IsEmpty || alreadyInstantiated == null)
            return false;

        alreadyInstantiated.transform.SetParent(transform, true);
        alreadyInstantiated.transform.position = GetPlacePosition();
        Occupy(alreadyInstantiated);
        return true;
    }
}
