using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 5;
    public int height = 5;
    public float cellSize = 1f;

    public GameObject cellPrefab;
    public GameObject pathPrefab;
    public GameObject corePrefab;

    public GameObject tilePreviewPrefab;
    private GameObject currentPreview;
    private List<Vector3> orderedPathPositions = new List<Vector3>();

    private Dictionary<Vector2Int, GameObject> gridCells = new();
    private List<Vector3> pathPositions = new();

    private Vector2Int currentPathEnd;
    private int tileCount = 0;

    private List<PlacedTileData> placedTiles = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    IEnumerator Start()
    {
        GenerateInitialGrid();
        yield return new WaitForSeconds(0.1f); // pequeño delay para asegurar que la UI esté lista

        UIManager.Instance?.ShowTileSelection(GetTileOptions());

    }

    void GenerateInitialGrid()
    {
        placedTiles.Clear();
        gridCells.Clear();
        pathPositions.Clear();

        GameObject tileCore = new GameObject("Tile-Core");
        tileCore.transform.parent = this.transform;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(x * cellSize, 0, z * cellSize);
                gridCells[pos] = Instantiate(cellPrefab, worldPos, Quaternion.identity, tileCore.transform);
                gridCells[pos].AddComponent<CellVisual>();
            }
        }

        // Posición del núcleo: centro en X, parte inferior en Z (z = 0)
        Vector2Int corePos = new Vector2Int(width / 2, 0);
        Vector3 coreWorldPos = new Vector3(corePos.x * cellSize, 0, corePos.y * cellSize);

        //Eliminar celda del core
        if (gridCells.ContainsKey(corePos))
        {
            Destroy(gridCells[corePos]);
            gridCells.Remove(corePos);
        }

        //  Instanciar core
        Instantiate(corePrefab, coreWorldPos, Quaternion.identity, tileCore.transform);
        pathPositions.Add(coreWorldPos);
        currentPathEnd = corePos;

        // Generar camino hacia arriba hasta el borde
        for (int z = 1; z < height; z++)
        {
            Vector2Int pathPos = new Vector2Int(corePos.x, z);
            Vector3 pathWorldPos = new Vector3(pathPos.x * cellSize, 0, pathPos.y * cellSize);

            if (gridCells.ContainsKey(pathPos))
            {
                Destroy(gridCells[pathPos]);
                gridCells.Remove(pathPos);
            }

            GameObject pathGO = Instantiate(pathPrefab, pathWorldPos, Quaternion.identity, tileCore.transform);

            // Guardar tile inicial
            pathGO.AddComponent<PathVisual>();
            pathPositions.Add(pathWorldPos);
            currentPathEnd = pathPos;

            Vector2Int initialBottomLeft = new Vector2Int(corePos.x - 2, 0);
            placedTiles.Add(new PlacedTileData("Inicial", initialBottomLeft));

        }
        //WaveManager.Instance?.StartNextWave();

    }

    public List<TileExpansion> GetTileOptions()
    {
        List<TileExpansion> tiles = new()
    {
        new TileExpansion("Recto", new[]
        {
            new Vector2Int(0, 0),
            Vector2Int.up,
            Vector2Int.up * 2,
            Vector2Int.up * 3,
            Vector2Int.up * 4
        }),

        new TileExpansion("L-Shape", new[]
        {
            new Vector2Int(0, 0),
            Vector2Int.up,
            Vector2Int.up * 2,
            Vector2Int.up * 2 + Vector2Int.right,
            Vector2Int.up * 2 + Vector2Int.right * 2
        }),

        new TileExpansion("L-Inverso", new[]
        {
            new Vector2Int(0, 0),
            Vector2Int.up,
            Vector2Int.up * 2,
            Vector2Int.up * 2 + Vector2Int.left,
            Vector2Int.up * 2 + Vector2Int.left * 2
        })
    };
        return tiles;
    }
    void ExpandGridIfNeeded(Vector2Int position)
    {
        if (!gridCells.ContainsKey(position))
        {
            Vector3 pos = new Vector3(position.x * cellSize, 0, position.y * cellSize);
            gridCells[position] = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
            gridCells[position].AddComponent<CellVisual>();

        }
    }
    public void ApplyTileExpansionAtWorldPosition(TileExpansion tile, Vector3 worldPosition)
    {
        tileCount++;
        GameObject tileParent = new GameObject($"Tile-{tileCount}");
        tileParent.transform.parent = this.transform;

        Vector2Int pathDirection = GetPathDirection();

        // Rotar offsets para que sigan la dirección actual del camino
        List<Vector2Int> rotatedOffsets = RotateOffsets(tile.pathOffsets, pathDirection);

        // Usar el final del camino como inicio del nuevo tile

        Vector2Int bottomLeft;

        Vector2Int tileOffset = Vector2Int.zero;

        if (pathDirection == Vector2Int.up)
        {
            tileOffset = new Vector2Int(-tile.tileSize.x / 2, 1);
        }
        else if (pathDirection == Vector2Int.right)
        {
            tileOffset = new Vector2Int(1, -tile.tileSize.y / 2);
        }
        else if (pathDirection == Vector2Int.down)
        {
            tileOffset = new Vector2Int(-tile.tileSize.x / 2, -tile.tileSize.y);
        }
        else if (pathDirection == Vector2Int.left)
        {
            tileOffset = new Vector2Int(-tile.tileSize.x, -tile.tileSize.y / 2);
        }

        bottomLeft = currentPathEnd  + tileOffset;
        Vector2Int startPath = currentPathEnd + pathDirection;

        /*
        // Aplicar desplazamiento lateral si el camino no es vertical
        if (pathDirection == Vector2Int.right || pathDirection == Vector2Int.left)
        {
            bottomLeft = new Vector2Int(
                startPath.x,
                startPath.y - tile.tileSize.y / 2
            );
        }*/


        // 4. Crear celdas (5x5)
        for (int x = 0; x < tile.tileSize.x; x++)
        {
            for (int y = 0; y < tile.tileSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(bottomLeft.x + x, bottomLeft.y + y);
                Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
                gridCells[pos] = Instantiate(cellPrefab, spawnPos, Quaternion.identity, tileParent.transform);
                gridCells[pos].AddComponent<CellVisual>();
            }
        }
        foreach (var offset in rotatedOffsets)
        {
            Vector2Int pathPos = startPath + offset;
            Vector3 spawnPos = new Vector3(pathPos.x * cellSize, 0, pathPos.y * cellSize);

            if (gridCells.ContainsKey(pathPos))
            {
                Destroy(gridCells[pathPos]);
                gridCells.Remove(pathPos);
            }

            gridCells[pathPos] = Instantiate(pathPrefab, spawnPos, Quaternion.identity, tileParent.transform);
            gridCells[pathPos].AddComponent<PathVisual>();

            pathPositions.Add(spawnPos);
        }

        currentPathEnd = startPath + rotatedOffsets[^1];
        placedTiles.Add(new PlacedTileData(tile.tileName, bottomLeft));

    }
    private List<Vector2Int> RotateOffsets(List<Vector2Int> original, Vector2Int direction)
    {
        List<Vector2Int> rotated = new();

        foreach (var offset in original)
        {
            Vector2Int rotatedOffset = offset;

            if (direction == Vector2Int.right)
            {
                rotatedOffset = new Vector2Int(offset.y, -offset.x);
            }
            else if (direction == Vector2Int.down)
            {
                rotatedOffset = new Vector2Int(-offset.x, -offset.y);
            }
            else if (direction == Vector2Int.left)
            {
                rotatedOffset = new Vector2Int(-offset.y, offset.x);
            }
            // if direction == up, keep original

            rotated.Add(rotatedOffset);
        }

        return rotated;
    }
    public bool IsTileAtPosition(Vector2Int pos)
    {
        return placedTiles.Any(p => p.basePosition == pos);
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // --- Mostrar tiles ya colocados (cyan) ---
        Gizmos.color = Color.cyan;
        foreach (var tile in placedTiles)
        {
            Vector3 center = new Vector3((tile.basePosition.x + 2) * cellSize, 0, (tile.basePosition.y + 2) * cellSize);
            Gizmos.DrawWireCube(center + Vector3.up * 0.1f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));
            UnityEditor.Handles.Label(center + Vector3.up * 0.3f, tile.name);
        }

        // --- Mostrar adyacentes ocupados (rojo) ---
        Vector2Int[] directions = new[]
        {
        Vector2Int.up * 5,
        Vector2Int.down * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
    };

        HashSet<Vector2Int> allTilePositions = new(placedTiles.Select(p => p.basePosition));

        foreach (var tile in placedTiles)
        {
            foreach (var dir in directions)
            {
                Vector2Int adjacent = tile.basePosition + dir;
                if (!allTilePositions.Contains(adjacent)) continue;

                Vector3 center = new Vector3((adjacent.x + 2) * cellSize, 0, (adjacent.y + 2) * cellSize);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center + Vector3.up * 0.05f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));
            }
        }

        // --- Tile siguiente (azul) ---
        Vector2Int pathDir = GetPathDirection();
        Vector2Int tileOffset = Vector2Int.zero;

        if (pathDir == Vector2Int.up)
            tileOffset = new Vector2Int(-5 / 2, 1);
        else if (pathDir == Vector2Int.right)
            tileOffset = new Vector2Int(1, -5 / 2);
        else if (pathDir == Vector2Int.down)
            tileOffset = new Vector2Int(-5 / 2, -5);
        else if (pathDir == Vector2Int.left)
            tileOffset = new Vector2Int(-5, -5 / 2);

        Vector2Int nextBottomLeft = GetLastPathGridPosition() + tileOffset;

        Vector3 blueCenter = new Vector3(
            (nextBottomLeft.x + 2) * cellSize,
            0,
            (nextBottomLeft.y + 2) * cellSize
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(blueCenter + Vector3.up * 0.15f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));

        // --- Adyacentes del próximo tile (naranja/violeta) con tile sugerido ---
        Vector2Int[] localDirs = new[]
        {
        Vector2Int.up * 5,
        Vector2Int.down * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
    };

        foreach (var dir in localDirs)
        {
            Vector2Int adjacent = nextBottomLeft + dir;

            // No mostrar si es de donde vino el camino
            if (adjacent == currentPathEnd)
                continue;

            Vector3 center = new Vector3((adjacent.x + 2) * cellSize, 0, (adjacent.y + 2) * cellSize);

            // Color según si hay tile en esa posición
            Gizmos.color = allTilePositions.Contains(adjacent)
                ? new Color(0.6f, 0f, 0.8f)   //  Violeta (ocupado)
                : new Color(1f, 0.5f, 0f);    //  Naranja (libre)

            Gizmos.DrawWireCube(center + Vector3.up * 0.05f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));

            // --- Mostrar nombre del tile sugerido ---
            Vector2Int delta = adjacent - nextBottomLeft;
            string sugerido = "";

            if (delta == pathDir * 5)
                sugerido = "Recto";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.up * 5))
                sugerido = "L-Shape";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.up * 5))
                sugerido = "L-Inverso";

            if (!string.IsNullOrEmpty(sugerido))
                UnityEditor.Handles.Label(center + Vector3.up * 0.2f, sugerido);
        }
    }
#endif

    public Vector2Int GetPathDirection()
    {
        if (pathPositions.Count < 2)
            return Vector2Int.up;

        Vector3 penultimo = pathPositions[^2];
        Vector3 ultimo = pathPositions[^1];

        Vector2Int from = new Vector2Int(Mathf.RoundToInt(penultimo.x / cellSize), Mathf.RoundToInt(penultimo.z / cellSize));
        Vector2Int to = new Vector2Int(Mathf.RoundToInt(ultimo.x / cellSize), Mathf.RoundToInt(ultimo.z / cellSize));

        return to - from;
    }
    public Vector2Int GetLastPathGridPosition()
    {
        if (pathPositions.Count == 0)
            return Vector2Int.zero;

        Vector3 lastWorld = pathPositions[^1];
        return new Vector2Int(
            Mathf.RoundToInt(lastWorld.x / cellSize),
            Mathf.RoundToInt(lastWorld.z / cellSize)
        );
    }

    public Vector3[] GetPathPositions()
    {
        return pathPositions.ToArray();
    }

    public Vector3 GetLastPathWorldPosition()
    {
        if (pathPositions.Count > 0)
            return pathPositions[^1]; // Última posición del camino
        else
            return Vector3.zero;
    }

    public void ShowPreviewTile(TileExpansion tile)
    {
        ClearCurrentPreview();

        Vector2Int pathDir = GetPathDirection();
        Vector2Int center = GetLastPathGridPosition() + pathDir;
        Vector3 worldPos = new Vector3(center.x * cellSize, 0, center.y * cellSize);


        GameObject preview = Instantiate(tilePreviewPrefab, worldPos, Quaternion.identity);
        //preview.GetComponent<PreviewClickable>().Initialize(tile, worldPos);

        currentPreview = preview;
    }
    public void ClearCurrentPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    public List<string> GetDisabledTileNamesFromNextAdyacents()
    {
        List<string> disabledTiles = new();

        Vector2Int pathDir = GetPathDirection();
        Vector2Int tileOffset = Vector2Int.zero;

        if (pathDir == Vector2Int.up)
            tileOffset = new Vector2Int(-5 / 2, 1);
        else if (pathDir == Vector2Int.right)
            tileOffset = new Vector2Int(1, -5 / 2);
        else if (pathDir == Vector2Int.down)
            tileOffset = new Vector2Int(-5 / 2, -5);
        else if (pathDir == Vector2Int.left)
            tileOffset = new Vector2Int(-5, -5 / 2);

        Vector2Int nextBottomLeft = GetLastPathGridPosition() + tileOffset;

        Vector2Int[] localDirs = new[]
        {
        Vector2Int.up * 5,
        Vector2Int.down * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
    };

        HashSet<Vector2Int> allTilePositions = new(placedTiles.Select(p => p.basePosition));

        foreach (var dir in localDirs)
        {
            Vector2Int adjacent = nextBottomLeft + dir;
            if (adjacent == currentPathEnd) continue;

            if (!allTilePositions.Contains(adjacent)) continue;

            Vector2Int delta = adjacent - nextBottomLeft;
            string sugerido = "";

            if (delta == pathDir * 5)
                sugerido = "Recto";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.up * 5))
                sugerido = "L-Shape";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.up * 5))
                sugerido = "L-Inverso";

            if (!string.IsNullOrEmpty(sugerido) && !disabledTiles.Contains(sugerido))
                disabledTiles.Add(sugerido);
        }

        return disabledTiles;
    }

}

[System.Serializable]
public class TileExpansion
{
    public string tileName;
    public Vector2Int tileSize = new Vector2Int(5, 5);
    public List<Vector2Int> pathOffsets;


    public TileExpansion(string name, Vector2Int[] offsets) 
    {
        tileName = name;
        pathOffsets = new List<Vector2Int>(offsets);
    }
}
[System.Serializable]
public class PlacedTileData
{
    public string name;
    public Vector2Int basePosition;

    public PlacedTileData(string name, Vector2Int basePosition)
    {
        this.name = name;
        this.basePosition = basePosition;
    }
}