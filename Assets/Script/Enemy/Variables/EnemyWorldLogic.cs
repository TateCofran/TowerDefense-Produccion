using UnityEngine;

public class EnemyWorldLogic : MonoBehaviour, IWorldAware
{
    private WorldState originWorld;
    private WorldState currentWorld;

    public void SetOriginWorld(WorldState world)
    {
        originWorld = world;
        currentWorld = world; // Por defecto empieza en su mundo de origen
    }

    public void ShiftWorld(WorldState newWorld)
    {
        currentWorld = newWorld;
        // Acá podés agregar lógica visual de cambio de mundo
    }

    public bool IsTargetable()
    {
        // Se puede atacar solo si está en el mundo activo del juego
        return currentWorld == WorldManager.Instance.CurrentWorld;
    }

    public WorldState GetOriginWorld()
    {
        return originWorld;
    }

    public WorldState GetCurrentWorld()
    {
        return currentWorld;
    }
}
