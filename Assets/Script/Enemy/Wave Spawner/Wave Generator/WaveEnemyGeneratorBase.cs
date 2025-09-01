using System.Collections;
using UnityEngine;

public abstract class WaveEnemyGeneratorBase : MonoBehaviour
{
    [SerializeField] protected EnemySpawner enemySpawner;
    [SerializeField] protected GridManager gridManager;
    [SerializeField] protected float spawnDelay = 0.7f;

    protected abstract WorldState TargetWorld { get; }
    protected virtual int GetEnemyCount(int baseCount) => baseCount;

    protected virtual EnemyType GetEnemyType()
    {
        return EnemyType.Minion; // Por defecto, o el tipo común en esa implementación
    }

    public void Spawn(int waveNumber, int totalEnemies)
    {
        if (WorldManager.Instance.CurrentWorld != TargetWorld) return;
        StartCoroutine(SpawnEnemies(waveNumber, totalEnemies)); // Llama al override si existe
    }

    protected virtual IEnumerator SpawnEnemies(int waveNumber, int totalEnemies)
    {
        Vector3[] path = gridManager.GetPathPositions();
        if (path == null || path.Length == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] El path está vacío.");
            yield break;
        }

        int finalCount = GetEnemyCount(totalEnemies);

        for (int i = 0; i < finalCount; i++)
        {
            Enemy enemy = enemySpawner.SpawnEnemy(path[^1], path, GetEnemyType());

            if (enemy != null)
            {
                enemy.SetOriginWorld(TargetWorld);
            }

            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"[{GetType().Name}] Oleada {WaveManager.Instance.GetCurrentWave()} - {TargetWorld} - Generados: {finalCount} enemigos.");
    }

}
