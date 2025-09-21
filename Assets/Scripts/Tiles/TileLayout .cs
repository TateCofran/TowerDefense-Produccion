using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Tile Layout 5x5", fileName = "NewTileLayout")]
public class TileLayout : ScriptableObject
{
    public enum TileType { Grass, Path }

    [Header("Grid Settings")]
    [Min(1)] public int gridWidth = 5;
    [Min(1)] public int gridHeight = 5;
    [Min(0.1f)] public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [Header("Core Configuration")]
    public bool hasCore = true;
    public Vector2Int coreCell = new Vector2Int(-1, -1);

    [System.Serializable]
    public struct TileEntry
    {
        public Vector2Int grid;
        public TileType type;
    }

    [Header("Tile Definitions")]
    public List<TileEntry> tiles = new List<TileEntry>();

    [Header("Path Endpoints")]
    public Vector2Int entry = new Vector2Int(-1, -1);
    public List<Vector2Int> exits = new List<Vector2Int>();

    public Vector3 GridToWorld(Vector2Int g)
        => origin + new Vector3(g.x * cellSize, 0f, g.y * cellSize);

    public bool IsInside(Vector2Int g)
        => g.x >= 0 && g.x < gridWidth && g.y >= 0 && g.y < gridHeight;

    public bool TryGetTile(Vector2Int g, out TileEntry t)
    {
        int idx = g.y * gridWidth + g.x;
        if (idx >= 0 && idx < tiles.Count)
        {
            t = tiles[idx];
            return true;
        }
        t = default;
        return false;
    }

    public bool IsPath(Vector2Int g)
    {
        if (!IsInside(g)) return false;
        if (TryGetTile(g, out var t)) return t.type == TileType.Path;
        return false;
    }

    public static readonly Vector2Int[] OrthoDirs = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

    /// <summary>
    /// Obtiene la celda donde colocar el Core
    /// </summary>
    public Vector2Int GetCoreCell()
    {
        // Si se especificó una celda específica y es válida, usarla
        if (coreCell.x >= 0 && coreCell.y >= 0 &&
            coreCell.x < gridWidth && coreCell.y < gridHeight &&
            IsPath(coreCell))
        {
            return coreCell;
        }

        // Si no, usar la entrada
        if (IsInside(entry) && IsPath(entry))
        {
            return entry;
        }

        // Fallback: buscar cualquier celda path
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (IsPath(cell)) return cell;
            }
        }

        return new Vector2Int(0, 0); // Último recurso
    }

    public bool AutoDetectEndpoints(out Vector2Int autoEntry, out List<Vector2Int> autoExits)
    {
        autoEntry = new Vector2Int(-1, -1);
        autoExits = new List<Vector2Int>();

        if (tiles == null || tiles.Count == 0) return false;

        var degree1 = new List<Vector2Int>();

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                var p = new Vector2Int(x, y);
                if (!IsPath(p)) continue;

                int neighPath = 0;
                foreach (var d in OrthoDirs)
                    if (IsPath(p + d)) neighPath++;

                if (neighPath == 1)
                    degree1.Add(p);
            }
        }

        if (degree1.Count == 0) return false;

        if (IsInside(entry) && IsPath(entry))
        {
            autoEntry = entry;
            foreach (var p in degree1)
                if (p != entry && !autoExits.Contains(p)) autoExits.Add(p);
        }
        else
        {
            autoEntry = degree1[0];
            for (int i = 1; i < degree1.Count; i++)
                autoExits.Add(degree1[i]);
        }

        return true;
    }
}