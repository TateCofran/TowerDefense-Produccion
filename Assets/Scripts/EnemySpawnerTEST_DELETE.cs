using UnityEngine;

public class EnemySpawnerTEST_DELETE : MonoBehaviour
{
    [Header("Prefabs y cantidad")]
    public GameObject enemyPrefab;
    public int numberOfEnemies = 5;

    [Header("Área de spawn")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector3 spawnSize = new Vector3(20, 10, 20);

    private void Start()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 randomPos = spawnCenter + new Vector3(
                Random.Range(-spawnSize.x / 2, spawnSize.x / 2),
                0f,
                Random.Range(-spawnSize.z / 2, spawnSize.z / 2)
            );

            GameObject enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
            enemy.name = "Enemy_" + i;
            enemy.tag = "Enemy";

            if (enemy.GetComponent<Enemy>() == null)
                enemy.AddComponent<Enemy>();
        }
    }
}
