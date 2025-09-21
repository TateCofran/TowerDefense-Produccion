using System.Collections.Generic;
using UnityEngine;

public class TileOrientationCalculator : MonoBehaviour
{
    private Dictionary<(TileLayout, int, bool), (int w, int h, Vector2Int entry)> _orientationCache = new();

    public (int w, int h, Vector2Int entryOriented) GetOrientedData(TileLayout layout, int rotSteps, bool flip)
    {
        var key = (layout, rotSteps, flip);
        if (_orientationCache.TryGetValue(key, out var cached))
            return cached;

        OrientedSize(layout, rotSteps, out int w, out int h);
        Vector2Int entryOriented = ApplyOrientationToCell(layout.entry, layout, rotSteps, flip);

        var result = (w, h, entryOriented);
        _orientationCache[key] = result;
        return result;
    }

    public static void OrientedSize(TileLayout layout, int rotSteps, out int w, out int h)
    {
        bool swap = (rotSteps % 2) != 0;
        w = swap ? layout.gridHeight : layout.gridWidth;
        h = swap ? layout.gridWidth : layout.gridHeight;
    }

    public static Vector2Int ApplyOrientationToCell(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
    {
        int W = layout.gridWidth;
        int H = layout.gridHeight;
        Vector2 p = new Vector2(c.x, c.y);

        if (flip) p.x = (W - 1) - p.x;
        for (int i = 0; i < rotSteps; i++)
        {
            float x = p.x;
            p.x = p.y;
            p.y = (W - 1) - x;
            int tmp = W; W = H; H = tmp;
        }
        return new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
    }
    public static Vector2Int ApplyInverseOrientationToDir(Vector2Int d, int rotSteps, bool flip)
    {
        Vector2 p = new Vector2(d.x, d.y);
        for (int i = 0; i < rotSteps; i++)
        {
            float x = p.x; p.x = -p.y; p.y = x;
        }
        if (flip) p.x = -p.x;
        return new Vector2Int(Mathf.RoundToInt(Mathf.Clamp(p.x, -1, 1)),
                              Mathf.RoundToInt(Mathf.Clamp(p.y, -1, 1)));
    }

    public static Vector3 CellToWorldLocal(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
    {
        Vector2Int oc = ApplyOrientationToCell(c, layout, rotSteps, flip);
        return new Vector3(oc.x * layout.cellSize, 0f, oc.y * layout.cellSize);
    }

    public void ClearCache()
    {
        _orientationCache.Clear();
    }

}