using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITileGenerator
{
    void GenerateFirst();
    void AppendNextUsingSelectedExit();
    IEnumerator AppendNextAsync();
    List<PlacementPreview> GetPlacementPreviews();
    bool TryGetSelectedExitWorld(out Vector3 pos, out Vector2Int dirOutWorld);
    List<(string label, Vector3 worldPos)> GetAvailableExits();
    List<string> GetAvailableExitLabels();
    void CleanupCaches();
}

public interface ITilePoolManager
{
    GameObject GetPooledObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent);
    void ReturnToPool(GameObject prefab, GameObject obj);
    void InitializePools();
}

public interface ISpatialPartitioner
{
    void AddToSpatialGrid(PlacedTile tile, int index);
    bool OverlapsAnyOptimized(Vector3 nMin, Vector3 nMax);
    void ClearSpatialGrid();
}