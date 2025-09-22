using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Vector3[] pathPositions;
    private int currentPathIndex = 0;

    [Header("Settings")]
    public float speed = 2f;
    public float arrivalThreshold = 0.3f;

    public void Initialize(Vector3 spawnPosition, Transform coreTransform, SpawnManager manager)
    {
        currentPathIndex = 0;

        // Usar Dijkstra REAL
        DijkstraPathfinder pathfinder = FindObjectOfType<DijkstraPathfinder>();
        if (pathfinder != null)
        {
            pathPositions = pathfinder.FindPath(spawnPosition, coreTransform.position);
            Debug.Log("Dijkstra retorno camino de " + (pathPositions?.Length ?? 0) + " puntos");
        }
        else
        {
            Debug.LogError("DijkstraPathfinder no encontrado");
            pathPositions = new Vector3[] { spawnPosition, coreTransform.position };
        }

        transform.position = spawnPosition;

        // Debug del camino
        if (pathPositions != null)
        {
            for (int i = 0; i < pathPositions.Length; i++)
            {
                Debug.Log($"Camino[{i}]: {pathPositions[i]}");
            }
        }
    }

    void Update()
    {
        if (pathPositions == null || currentPathIndex >= pathPositions.Length) return;

        MoveToNextPoint();
    }

    void MoveToNextPoint()
    {
        Vector3 target = pathPositions[currentPathIndex];

        // Rotar y mover
        transform.rotation = Quaternion.LookRotation((target - transform.position).normalized);
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) <= arrivalThreshold)
        {
            currentPathIndex++;
        }
    }

    // ... resto de métodos (TakeDamage, Die, etc) ...
}