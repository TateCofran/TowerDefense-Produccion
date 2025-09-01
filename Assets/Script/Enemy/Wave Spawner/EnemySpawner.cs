using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs del mundo normal")]
    [SerializeField] private GameObject minionEnemyPrefab;
    [SerializeField] private GameObject sprintEnemyPrefab;
    [SerializeField] private GameObject bigEnemyPrefab;
    [SerializeField] private GameObject miniBossPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject superMinionPrefab;
    [SerializeField] private GameObject superSprintPrefab;
    [SerializeField] private GameObject superBigPrefab;

    private Dictionary<EnemyType, GameObject> enemyPrefabs;

    private void Awake()
    {
        enemyPrefabs = new Dictionary<EnemyType, GameObject>
        {
            { EnemyType.Minion, minionEnemyPrefab },
            { EnemyType.Sprint, sprintEnemyPrefab },
            { EnemyType.Big, bigEnemyPrefab },
            { EnemyType.MiniBoss, miniBossPrefab },
            { EnemyType.Boss, bossPrefab },
            { EnemyType.SuperMinion, superMinionPrefab },
            { EnemyType.SuperSprint, superSprintPrefab },
            { EnemyType.SuperBig, superBigPrefab },

        };
    }

    public Enemy SpawnEnemy(Vector3 spawnPosition, Vector3[] path, EnemyType type)
    {
        if (!enemyPrefabs.ContainsKey(type))
        {
            Debug.LogError($"[EnemySpawner] No hay prefab registrado para el tipo: {type}");
            return null;
        }

        GameObject enemyGO = EnemyPool.Instance.GetEnemy(type); // <- Usá el enum
        enemyGO.transform.position = spawnPosition;

        Enemy enemy = enemyGO.GetComponent<Enemy>();

        // Inicialización completa
        enemy.InitializePath(path, Core.Instance.gameObject, GridManager.Instance);

        //enemy.SetOriginWorld(WorldManager.Instance.CurrentWorld);
        //enemy.WorldLogic.UpdateVisibility();

        return enemy;
    }
}
