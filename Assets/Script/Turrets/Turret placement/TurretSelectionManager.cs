using UnityEngine;

public class TurretSelectionManager : MonoBehaviour
{
    public static TurretSelectionManager Instance;

    public TurretSelection heavyTurret;
    public TurretSelection basicTurret;
    public TurretSelection goldTurret;
    public TurretSelection AOETurret;
    public TurretSelection SlowTurret;
    public TurretSelection OtherWorldTurret;

    [HideInInspector] public TurretSelection selectedTurret;

    void Awake()
    {
        Instance = this;
    }

    public void SelectTurretById(string turretId)
    {
        // Buscá el objeto TurretSelection por id
        if (turretId == "basic") selectedTurret = basicTurret;
        else if (turretId == "basic_otherworld") selectedTurret = OtherWorldTurret;
        else if (turretId == "gold_generator") selectedTurret = goldTurret;
        // ... etc para las otras
        Debug.Log($"Seleccionaste torreta id={turretId}");
    }

}

[System.Serializable]
public class TurretSelection
{
    public string turretId;            // ID usado para buscar en el JSON
    public GameObject turretPrefab;   // Prefab base de la torreta
}
