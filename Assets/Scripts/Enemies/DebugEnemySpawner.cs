using UnityEngine;

public class DebugEnemySpawner : MonoBehaviour
{
    [Header("Debug Controls")]
    public KeyCode spawnKey = KeyCode.F;
    public KeyCode infoKey = KeyCode.I;
    public bool showGizmos = true;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnEnemyWithDebug();
        }

        if (Input.GetKeyDown(infoKey))
        {
            ShowSpawnInfo();
        }
    }

    void SpawnEnemyWithDebug()
    {
        Debug.Log("=== INTENTANDO SPAWN ===");

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager.Instance es null");
            return;
        }

        GridGenerator grid = FindObjectOfType<GridGenerator>();
        if (grid == null)
        {
            Debug.LogError("No se encontró GridGenerator");
            return;
        }

        var spawnPoints = grid.GetSpawnPoints();
        Debug.Log("Spawn points encontrados: " + spawnPoints.Count);

        bool success = SpawnManager.Instance.SpawnEnemyAtPathEnd();

        if (success)
        {
            Debug.Log("Enemigo spawneado en FINAL de camino");
        }
        else
        {
            Debug.Log("Fallo el spawn. Verifica con I");
            ShowSpawnInfo();
        }
    }

    void ShowSpawnInfo()
    {
        Debug.Log("=== SPAWN DEBUG INFO ===");

        GridGenerator grid = FindObjectOfType<GridGenerator>();
        if (grid == null)
        {
            Debug.LogError("GridGenerator no encontrado");
            return;
        }

        Debug.Log("GridGenerator encontrado: " + grid.name);

        int tileCount = grid.GetChainCount();
        Debug.Log("Tiles generados: " + tileCount);

        var spawnPoints = grid.GetSpawnPoints();
        Debug.Log("Puntos de spawn detectados: " + spawnPoints.Count);

        if (spawnPoints.Count > 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                Debug.Log("   Spawn " + i + ": " + spawnPoints[i]);
            }
        }
        else
        {
            Debug.LogWarning("No hay spawn points. Posibles causas:");
            Debug.LogWarning("   - No hay tiles generados");
            Debug.LogWarning("   - Los tiles no tienen exits configurados");
            Debug.LogWarning("   - El algoritmo de deteccion no encuentra finales de camino");
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager.Instance es null");
            return;
        }

        Debug.Log("SpawnManager: " + SpawnManager.Instance.name);

        GameObject coreObj = GameObject.FindGameObjectWithTag("Core");
        if (coreObj == null)
        {
            Debug.LogError("No hay objeto con tag 'Core'");
        }
        else
        {
            Debug.Log("Core encontrado: " + coreObj.name + " en " + coreObj.transform.position);
        }

        Debug.Log("=== CONTROLES ===");
        Debug.Log("F - Spawn enemigo con debug detallado");
        Debug.Log("I - Info de spawn");
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        GridGenerator grid = FindObjectOfType<GridGenerator>();
        if (grid != null)
        {
            var spawnPoints = grid.GetSpawnPoints();
            Gizmos.color = Color.green;
            foreach (var point in spawnPoints)
            {
                Gizmos.DrawSphere(point + Vector3.up * 0.5f, 0.5f);
                Gizmos.DrawWireSphere(point, 1.5f);
                Gizmos.DrawLine(point, point + Vector3.up * 2f);
            }

            Gizmos.color = Color.blue;
            for (int i = 0; i < grid.GetChainCount(); i++)
            {
                var tile = grid.GetPlacedTile(i);
                Vector3 center = (tile.aabbMin + tile.aabbMax) / 2f;
                Vector3 size = new Vector3(tile.aabbMax.x - tile.aabbMin.x, 0.1f, tile.aabbMax.z - tile.aabbMin.z);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}