using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    [SerializeField] private GameObject minionEnemyPrefab;
    [SerializeField] private GameObject sprintEnemyPrefab;
    [SerializeField] private GameObject bigEnemyPrefab;
    [SerializeField] private GameObject miniBossPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject superMinionPrefab;
    [SerializeField] private GameObject superSprintPrefab;
    [SerializeField] private GameObject superBigPrefab;

    [SerializeField] private int initialSizePerType = 10;

    public Dictionary<EnemyType, Queue<GameObject>> pools = new();
    private Dictionary<EnemyType, int> totalInstantiated = new();

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Inicializar colas y contadores por tipo usando el enum
        foreach (EnemyType type in System.Enum.GetValues(typeof(EnemyType)))
        {
            pools[type] = new Queue<GameObject>();
            totalInstantiated[type] = 0;
        }

        // Inicializar objetos del pool
        InitializePool(minionEnemyPrefab, EnemyType.Minion);
        InitializePool(sprintEnemyPrefab, EnemyType.Sprint);
        InitializePool(bigEnemyPrefab, EnemyType.Big);
        InitializePool(miniBossPrefab, EnemyType.MiniBoss, 2);
        InitializePool(bossPrefab, EnemyType.Boss, 1);
        InitializePool(superMinionPrefab, EnemyType.SuperMinion);
        InitializePool(superSprintPrefab, EnemyType.SuperSprint);
        InitializePool(superBigPrefab, EnemyType.SuperBig);

    }

    void InitializePool(GameObject prefab, EnemyType type, int amount = -1)
    {
        int total = amount > 0 ? amount : initialSizePerType;

        for (int i = 0; i < total; i++)
        {
            GameObject enemy = Instantiate(prefab);
            enemy.transform.SetParent(this.transform);
            enemy.SetActive(false);
            pools[type].Enqueue(enemy);
            totalInstantiated[type]++;
        }
    }
    public GameObject GetEnemy(EnemyType type)
    {
        Queue<GameObject> queue = pools[type];
        GameObject enemy;

        if (queue.Count == 0)
        {
            GameObject prefab = type switch
            {
                EnemyType.Minion => minionEnemyPrefab,
                EnemyType.Sprint => sprintEnemyPrefab,
                EnemyType.Big => bigEnemyPrefab,
                EnemyType.MiniBoss => miniBossPrefab,
                EnemyType.Boss => bossPrefab,
                EnemyType.SuperMinion => superMinionPrefab,
                EnemyType.SuperSprint => superSprintPrefab,
                EnemyType.SuperBig => superBigPrefab,
                _ => minionEnemyPrefab,
            };

            enemy = Instantiate(prefab);
            enemy.transform.SetParent(this.transform);
            totalInstantiated[type]++;
        }
        else
        {
            enemy = queue.Dequeue();
        }

        enemy.SetActive(true);

        // Ya no llames a Register/Unregister manualmente
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.ResetEnemy(); // Si lo implementaste
        }

        return enemy;
    }

    public void ReturnEnemy(EnemyType type, GameObject enemy)
    {
        // NO LLAMES UnregisterEnemy acá
        Collider col = enemy.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        enemy.transform.position = Vector3.one * -9999;
        enemy.transform.SetParent(this.transform);
        enemy.SetActive(false);

        pools[type].Enqueue(enemy);

        if (col != null) col.enabled = true;
    }

    public void LogPoolStatus()
    {
        foreach (var type in pools.Keys)
        {
            int instantiated = totalInstantiated[type];
            int unused = pools[type].Count;
            int used = instantiated - unused;

            // Debug.Log($"Estado Pool '{type}': Total instanciados: {instantiated}, Usados: {used}, Sin usar: {unused}");
        }
    }

}
