using System.Collections.Generic;
using UnityEngine;

public class EnemyMovement : MonoBehaviour, IMovable
{
    private Vector3[] pathPositions;
    private int currentPathIndex;
    private float speed;
    private Enemy enemyReference;

    private List<Vector3> debugPath = new List<Vector3>();

    public void Initialize(Vector3[] path, float baseSpeed)
    {
        speed = baseSpeed; // Asignar primero la velocidad

        //Debug.Log($"[EnemyMovement] Inicializando con velocidad: {speed}, path: {path?.Length}");

        pathPositions = path;
        currentPathIndex = 0;
        transform.position = pathPositions[0];

        debugPath = new List<Vector3>(path);
    }

    private void Awake()
    {
        enemyReference = GetComponent<Enemy>();
    }

    public void Move()
    {
        if (pathPositions == null || currentPathIndex >= pathPositions.Length) return;

        Vector3 target = pathPositions[currentPathIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            AdvanceToNextSegment();
        }

        DebugDrawPath();
    }

    private void AdvanceToNextSegment()
    {
        if (currentPathIndex + 1 < pathPositions.Length)
        {
            currentPathIndex++;
        }
    }

    private void DebugDrawPath()
    {
        if (debugPath.Count > 1)
        {
            for (int i = 0; i < debugPath.Count - 1; i++)
            {
                Debug.DrawLine(debugPath[i], debugPath[i + 1], Color.magenta);
            }

            Debug.DrawLine(transform.position, pathPositions[currentPathIndex], Color.green);
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, (pathPositions[currentPathIndex] - transform.position), Color.red);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public float GetSpeed()
    {
        return speed;
    }
    public void MultiplySpeed(float multiplier)
    {
        speed *= multiplier;
    }

    public bool HasReachedEnd()
    {
        return currentPathIndex >= pathPositions.Length;
    }
}
