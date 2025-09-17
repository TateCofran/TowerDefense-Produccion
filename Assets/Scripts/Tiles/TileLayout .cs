using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Tile Layout 5x5", fileName = "NewTileLayout")]
public class TileLayout : ScriptableObject
{
    public enum TileType { Grass, Path }

    [Header("Grid")]
    [Min(1)] public int gridWidth = 5;
    [Min(1)] public int gridHeight = 5;
    [Min(0.1f)] public float cellSize = 1f;
    public Vector3 origin = Vector3.zero;

    [System.Serializable]
    public struct TileEntry
    {
        public Vector2Int grid;   // posición (x,y)
        public TileType type;     // Grass o Path
    }

    [Header("Tiles definidos")]
    public List<TileEntry> tiles = new List<TileEntry>();

    [Header("Endpoints")]
    public Vector2Int entry = new Vector2Int(-1, -1);
    public List<Vector2Int> exits = new List<Vector2Int>(); // ahora pueden ser varios

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

    public static readonly Vector2Int[] OrthoDirs =
    {
        new Vector2Int( 1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 0,-1),
    };

    /// <summary>
    /// Autodetecta endpoints: toma todos los nodos Path con un solo vecino Path (grado 1).
    /// Usa el primero como Entry (si no tenías uno válido) y el resto como Exits.
    /// </summary>
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

        // Si ya tenés entry válido, lo mantenemos.
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
