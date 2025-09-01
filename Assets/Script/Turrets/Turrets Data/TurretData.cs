using System.Collections.Generic;

public enum AllowedWorld
{
    Normal,
    Other,
    Both
}
[System.Serializable]
public class TurretData
{
    public string id;
    public string name;
    public float damage;
    public float range;
    public float fireRate;
    public int cost;
    public string type; // "attack" o "support"

    //solo para supports
    public int goldPerWave; // Solo se usa si es tipo support

    //solo para slow 
    public float slowAmount;     // Para torretas slow
    public float slowDuration;

    //solo para aoe
    public float explosionRadius; // Para torretas AOE

    public string world = "Both"; // ¡Ahora string!

    // Función para convertir string a enum
    public AllowedWorld GetAllowedWorld()
    {
        return world switch
        {
            "Normal" => AllowedWorld.Normal,
            "Other" => AllowedWorld.Other,
            "Both" => AllowedWorld.Both,
            _ => AllowedWorld.Both
        };
    }
}

[System.Serializable]
public class TurretDataList
{
    public List<TurretData> turrets;
}

