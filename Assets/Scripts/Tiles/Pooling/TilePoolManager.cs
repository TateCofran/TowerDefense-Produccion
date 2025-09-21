using System.Collections.Generic;
using UnityEngine;

public class TilePoolManager : MonoBehaviour, ITilePoolManager
{
    [Header("Object Pooling")]
    [SerializeField] private int initialPoolSize = 100;
    [SerializeField] private bool useObjectPooling = true;

    private Dictionary<GameObject, Queue<GameObject>> _objectPools = new();

    public void InitializePools()
    {
        if (!useObjectPooling) return;

        // Estos prefabs se deben asignar desde el GridGenerator principal
        var gridGenerator = GetComponent<GridGenerator>();
        if (gridGenerator != null)
        {
            InitializePool(gridGenerator.GrassPrefab, initialPoolSize);
            InitializePool(gridGenerator.PathPrefab, initialPoolSize);
        }
    }

    private void InitializePool(GameObject prefab, int size)
    {
        if (prefab == null) return;

        var queue = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }

        _objectPools[prefab] = queue;
    }

    public GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!useObjectPooling || !_objectPools.ContainsKey(prefab) || _objectPools[prefab].Count == 0)
        {
            return Instantiate(prefab, position, rotation, parent);
        }

        GameObject obj = _objectPools[prefab].Dequeue();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(parent);
        obj.SetActive(true);
        return obj;
    }

    public void ReturnToPool(GameObject prefab, GameObject obj)
    {
        if (!useObjectPooling || !_objectPools.ContainsKey(prefab))
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _objectPools[prefab].Enqueue(obj);
    }
}