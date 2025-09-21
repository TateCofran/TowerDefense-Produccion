using System.Collections.Generic;
using UnityEngine;

public class SpatialPartitioner : MonoBehaviour, ISpatialPartitioner
{
    [SerializeField] private float spatialCellSize = 10f;

    private Dictionary<Vector2Int, List<int>> _spatialGrid = new();

    public void AddToSpatialGrid(PlacedTile tile, int index)
    {
        Vector2Int minCell = WorldToSpatialCell(tile.aabbMin);
        Vector2Int maxCell = WorldToSpatialCell(tile.aabbMax);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector2Int cellKey = new Vector2Int(x, y);
                if (!_spatialGrid.ContainsKey(cellKey))
                    _spatialGrid[cellKey] = new List<int>();

                _spatialGrid[cellKey].Add(index);
            }
        }
    }

    public bool OverlapsAnyOptimized(Vector3 nMin, Vector3 nMax)
    {
        Vector2Int minCell = WorldToSpatialCell(nMin);
        Vector2Int maxCell = WorldToSpatialCell(nMax);

        var gridGenerator = GetComponent<GridGenerator>();
        if (gridGenerator == null) return false;

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                Vector2Int cellKey = new Vector2Int(x, y);
                if (_spatialGrid.TryGetValue(cellKey, out var indices))
                {
                    foreach (int index in indices)
                    {
                        var t = gridGenerator.GetPlacedTile(index);
                        if (AabbOverlap(nMin, nMax, t.aabbMin, t.aabbMax))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    public void ClearSpatialGrid()
    {
        _spatialGrid.Clear();
    }

    private Vector2Int WorldToSpatialCell(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / spatialCellSize),
            Mathf.FloorToInt(worldPos.z / spatialCellSize)
        );
    }

    private static bool AabbOverlap(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        if (aMax.x <= bMin.x || aMin.x >= bMax.x) return false;
        if (aMax.z <= bMin.z || aMin.z >= bMax.z) return false;
        return true;
    }
}