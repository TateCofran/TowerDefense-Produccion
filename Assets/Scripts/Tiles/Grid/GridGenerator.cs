using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;

public class GridGenerator : MonoBehaviour, ITileGenerator
{
    [Header("Dependencies")]
    [SerializeField] private TilePoolManager tilePoolManager;
    [SerializeField] private SpatialPartitioner spatialPartitioner;
    [SerializeField] private TileOrientationCalculator orientationCalculator;

    [Header("Layouts")]
    [SerializeField] private TileLayout initialLayout;
    [SerializeField] private List<TileLayout> candidateLayouts = new List<TileLayout>();

    [Header("Prefabs")]
    [SerializeField] private GameObject grassPrefab;
    [SerializeField] private GameObject pathPrefab;

    [Header("Parent / Salida")]
    [SerializeField] private Transform tilesRoot;
    [SerializeField] private string rootName = "TileChainRoot";
    [SerializeField] private string tileGroupBaseName = "Tile_";

    [Header("Cadena")]
    [SerializeField] private bool clearOnStartGenerate = true;
    [SerializeField] private int connectionTries = 24;

    [Header("Offsets por celda")]
    [SerializeField] private Vector3 grassCellOffset = Vector3.zero;
    [SerializeField] private Vector3 pathCellOffset = Vector3.zero;

    [Header("Offset de unión entre tiles (nivel TILE)")]
    [SerializeField] private float tileGap = 0.25f;
    [SerializeField] private Vector3 extraTileOffset = Vector3.zero;

    [Header("Orientaciones")]
    [SerializeField] private bool allowRotations = true;
    [SerializeField] private bool allowFlip = true;

    public enum PreviewStatus { Valid, Overlap, WrongEdge }

    // Propiedades públicas para acceso de las dependencias
    public GameObject GrassPrefab => grassPrefab;
    public GameObject PathPrefab => pathPrefab;

    // Propiedades públicas para TileLayoutGizmos
    public TileLayout CurrentLayout => _chainCount > 0 ? _chainArray[_chainCount - 1].layout : initialLayout;
    public Vector3 CurrentWorldOrigin => _chainCount > 0 ? _chainArray[_chainCount - 1].worldOrigin : Vector3.zero;
    public int CurrentRotSteps => _chainCount > 0 ? _chainArray[_chainCount - 1].rotSteps : 0;
    public bool CurrentFlipped => _chainCount > 0 && _chainArray[_chainCount - 1].flipped;

    private PlacedTile[] _chainArray = Array.Empty<PlacedTile>();
    private ExitRecord[] _exitsArray = Array.Empty<ExitRecord>();
    private int _chainCount = 0;
    private int _exitsCount = 0;
    private System.Random _rng = new System.Random();

    [SerializeField] private int selectedExitIndex = 0;

    private void Start()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        if (tilePoolManager == null) tilePoolManager = GetComponent<TilePoolManager>();
        if (spatialPartitioner == null) spatialPartitioner = GetComponent<SpatialPartitioner>();
        if (orientationCalculator == null) orientationCalculator = GetComponent<TileOrientationCalculator>();

        tilePoolManager?.InitializePools();
    }

    #region Implementación de ITileGenerator
    public void GenerateFirst()
    {
        if (!CheckPrefabs()) return;
        if (initialLayout == null)
        {
            Debug.LogError("[GridGenerator] Asigná initialLayout.");
            return;
        }
        EnsureRoot();

        if (clearOnStartGenerate)
        {
            ClearRootChildren();
            _chainArray = Array.Empty<PlacedTile>();
            _exitsArray = Array.Empty<ExitRecord>();
            _chainCount = 0;
            _exitsCount = 0;
            spatialPartitioner?.ClearSpatialGrid();
            selectedExitIndex = 0;
        }

        int rot = 0; bool flip = false;
        Vector3 worldOrigin = initialLayout.origin;

        var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chainCount);
        int count = InstantiateLayout(initialLayout, worldOrigin, rot, flip, parent);

        TileOrientationCalculator.OrientedSize(initialLayout, rot, out int w, out int h);
        float W = w * initialLayout.cellSize;
        float H = h * initialLayout.cellSize;

        var placed = new PlacedTile
        {
            layout = initialLayout,
            worldOrigin = worldOrigin,
            rotSteps = rot,
            flipped = flip,
            parent = parent,
            aabbMin = worldOrigin,
            aabbMax = worldOrigin + new Vector3(W, 0f, H)
        };

        AddToChain(placed);
        spatialPartitioner?.AddToSpatialGrid(placed, _chainCount - 1);
        AddTileExitsToPool(_chainCount - 1, excludeAssetCell: null);

        Debug.Log($"[GridGenerator] Primer tile instanciado ({count} celdas). Exits disponibles: {GetAvailableExitLabels().Count}");
    }

    public void AppendNextUsingSelectedExit()
    {
        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0)
        {
            Debug.LogWarning("[GridGenerator] No hay EXITS disponibles.");
            return;
        }

        ExitRecord chosen = _exitsArray[globalIdx];
        if (AppendTileToExit(chosen))
        {
            chosen.SetUsed(true);
            _exitsArray[globalIdx] = chosen;
            RelabelAvailableExits();
        }
        else
        {
            if (!HasAnyValidPlacementForExit(chosen))
            {
                chosen.SetClosed(true);
                chosen.label = "EXIT CERRADO";
                _exitsArray[globalIdx] = chosen;
                RelabelAvailableExits();
                selectedExitIndex = 0;
            }
        }
    }

    public IEnumerator AppendNextAsync()
    {
        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exitsCount == 0) yield break;

        ExitRecord chosen = _exitsArray[globalIdx];
        if (chosen.Used) yield break;

        bool success = false;
        for (int attempt = 0; attempt < connectionTries; attempt++)
        {
            if (AppendTileToExit(chosen))
            {
                success = true;
                break;
            }
            yield return null;
        }

        if (success)
        {
            chosen.SetUsed(true);
            _exitsArray[globalIdx] = chosen;
            RelabelAvailableExits();
        }
        else
        {
            if (!HasAnyValidPlacementForExit(chosen))
            {
                chosen.SetClosed(true);
                chosen.label = "EXIT CERRADO";
                _exitsArray[globalIdx] = chosen;
                RelabelAvailableExits();
                selectedExitIndex = 0;
            }
        }
    }

    public List<PlacementPreview> GetPlacementPreviews()
    {
        var previews = new List<PlacementPreview>();
        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exitsCount == 0) return previews;

        ExitRecord chosen = _exitsArray[globalIdx];
        if (chosen.Used) return previews;

        var tile = GetPlacedTile(chosen.tileIndex);
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return previews;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return previews;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = TileOrientationCalculator.ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + TileOrientationCalculator.CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

        foreach (var candidate in candidateLayouts)
        {
            if (candidate == null) continue;

            int rotMax = allowRotations ? 4 : 1;
            for (int rot = 0; rot < rotMax; rot++)
            {
                int flipCount = allowFlip ? 2 : 1;
                for (int f = 0; f < flipCount; f++)
                {
                    bool flip = allowFlip && (f == 1);
                    var entry = candidate.entry;
                    if (!candidate.IsInside(entry) || !candidate.IsPath(entry)) continue;

                    var orientedData = orientationCalculator.GetOrientedData(candidate, rot, flip);
                    float width = orientedData.w * candidate.cellSize;
                    float height = orientedData.h * candidate.cellSize;

                    bool okBorde = false;
                    if (dirOut == Vector2Int.right) okBorde = (orientedData.entryOriented.x == 0);
                    if (dirOut == Vector2Int.left) okBorde = (orientedData.entryOriented.x == orientedData.w - 1);
                    if (dirOut == Vector2Int.up) okBorde = (orientedData.entryOriented.y == 0);
                    if (dirOut == Vector2Int.down) okBorde = (orientedData.entryOriented.y == orientedData.h - 1);

                    Vector3 newOrigin = ComputeNewOrigin(candidate, rot, orientedData.entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
                    Vector3 nMin = newOrigin;
                    Vector3 nMax = newOrigin + new Vector3(width, 0f, height);
                    bool noOverlap = !spatialPartitioner.OverlapsAnyOptimized(nMin, nMax);

                    var status = okBorde
                        ? (noOverlap ? PreviewStatus.Valid : PreviewStatus.Overlap)
                        : PreviewStatus.WrongEdge;

                    previews.Add(new PlacementPreview
                    {
                        origin = newOrigin,
                        sizeXZ = new Vector2(width, height),
                        valid = (status == PreviewStatus.Valid),
                        status = status,
                        note = $"{candidate.name} | {rot * 90}° | flip={(flip ? 1 : 0)}",
                        cellSize = candidate.cellSize
                    });
                }
            }
        }

        return previews;
    }

    public bool TryGetSelectedExitWorld(out Vector3 pos, out Vector2Int dirOutWorld)
    {
        pos = Vector3.zero;
        dirOutWorld = Vector2Int.zero;

        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exitsCount == 0) return false;

        ExitRecord chosen = _exitsArray[globalIdx];
        if (chosen.Used) return false;

        var tile = GetPlacedTile(chosen.tileIndex);
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int d = (exitCell - nb.Value);
        d = TileOrientationCalculator.ApplyInverseOrientationToDir(d, tile.rotSteps, tile.flipped);
        d = ClampToOrtho(d);

        pos = tile.worldOrigin + TileOrientationCalculator.CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);
        dirOutWorld = d;
        return true;
    }

    public List<(string label, Vector3 worldPos)> GetAvailableExits()
    {
        var list = new List<(string, Vector3)>();
        for (int i = 0; i < _exitsCount; i++)
        {
            if (_exitsArray[i].Used || _exitsArray[i].Closed) continue;
            var pt = GetPlacedTile(_exitsArray[i].tileIndex);
            Vector3 wpos = pt.worldOrigin + TileOrientationCalculator.CellToWorldLocal(
                _exitsArray[i].cell, pt.layout, pt.rotSteps, pt.flipped);
            list.Add((_exitsArray[i].label, wpos));
        }
        return list;
    }

    public List<string> GetAvailableExitLabels()
    {
        var labels = new List<string>();
        for (int i = 0; i < _exitsCount; i++)
        {
            if (!_exitsArray[i].Used && !_exitsArray[i].Closed)
                labels.Add(_exitsArray[i].label);
        }
        return labels;
    }

    public void CleanupCaches()
    {
        orientationCalculator?.ClearCache();
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
    #endregion

    #region Métodos Públicos UI
    public void UI_SetExitIndex(int index) => selectedExitIndex = Mathf.Max(0, index);
    public void UI_SetExitByLabel(string label)
    {
        int idx = GetAvailableExitIndexByLabel(label);
        if (idx >= 0) selectedExitIndex = idx;
    }

    public void UI_GenerateFirst() => GenerateFirst();
    public void UI_AppendNext() => AppendNextUsingSelectedExit();
    public void UI_AppendNextAsync() => StartCoroutine(AppendNextAsync());
    public void UI_SetTileGap(float g) => tileGap = g;
    public void UI_SetExtraOffsetX(float x) => extraTileOffset.x = x;
    public void UI_SetExtraOffsetY(float y) => extraTileOffset.y = y;
    public void UI_SetExtraOffsetZ(float z) => extraTileOffset.z = z;
    public void UI_CleanupCaches() => CleanupCaches();
    #endregion

    #region Métodos Internos
    private bool AppendTileToExit(ExitRecord chosen)
    {
        int tileIdx = chosen.tileIndex;
        var tile = GetPlacedTile(tileIdx);
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell))
            return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue)
            return false;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = TileOrientationCalculator.ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + TileOrientationCalculator.CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

        var validCandidates = GetValidCandidates(dirOut, prevMin, prevMax, prevExitWorld);

        foreach (var (candidate, rot, flip) in validCandidates.Take(connectionTries))
        {
            var entry = candidate.entry;
            if (!candidate.IsInside(entry) || !candidate.IsPath(entry)) continue;

            var orientedData = orientationCalculator.GetOrientedData(candidate, rot, flip);
            float width = orientedData.w * candidate.cellSize;
            float height = orientedData.h * candidate.cellSize;

            Vector3 newOrigin = ComputeNewOrigin(candidate, rot, orientedData.entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
            Vector3 nMin = newOrigin;
            Vector3 nMax = newOrigin + new Vector3(width, 0f, height);

            if (spatialPartitioner.OverlapsAnyOptimized(nMin, nMax))
                continue;

            var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chainCount);
            int count = InstantiateLayout(candidate, newOrigin, rot, flip, parent);

            var placed = new PlacedTile
            {
                layout = candidate,
                worldOrigin = newOrigin,
                rotSteps = rot,
                flipped = flip,
                parent = parent,
                aabbMin = nMin,
                aabbMax = nMax
            };

            AddToChain(placed);
            spatialPartitioner.AddToSpatialGrid(placed, _chainCount - 1);
            AddTileExitsToPool(_chainCount - 1, excludeAssetCell: candidate.entry);

            Debug.Log($"[GridGenerator] Conectado via EXIT {chosen.label} (tile #{tileIdx}) → Nuevo tile #{_chainCount - 1} (rot={rot * 90}°, flip={flip}). Instancias: {count}");
            return true;
        }

        return false;
    }

    private List<(TileLayout, int, bool)> GetValidCandidates(Vector2Int dirOut, Vector3 prevMin, Vector3 prevMax, Vector3 prevExitWorld)
    {
        var validCandidates = new List<(TileLayout, int, bool)>();
        foreach (var candidate in candidateLayouts)
        {
            if (candidate == null) continue;

            int rotMax = allowRotations ? 4 : 1;
            for (int rot = 0; rot < rotMax; rot++)
            {
                int flipCount = allowFlip ? 2 : 1;
                for (int f = 0; f < flipCount; f++)
                {
                    bool flip = allowFlip && (f == 1);
                    var entry = candidate.entry;
                    if (!candidate.IsInside(entry) || !candidate.IsPath(entry)) continue;

                    var orientedData = orientationCalculator.GetOrientedData(candidate, rot, flip);

                    bool okBorde = false;
                    if (dirOut == Vector2Int.right) okBorde = (orientedData.entryOriented.x == 0);
                    if (dirOut == Vector2Int.left) okBorde = (orientedData.entryOriented.x == orientedData.w - 1);
                    if (dirOut == Vector2Int.up) okBorde = (orientedData.entryOriented.y == 0);
                    if (dirOut == Vector2Int.down) okBorde = (orientedData.entryOriented.y == orientedData.h - 1);

                    if (okBorde)
                    {
                        validCandidates.Add((candidate, rot, flip));
                    }
                }
            }
        }
        return validCandidates.OrderBy(x => _rng.Next()).ToList();
    }

    private IEnumerator AppendTileToExitAsync(ExitRecord chosen)
    {
        // Implementación asíncrona simplificada
        yield return null;
    }

    public PlacedTile GetPlacedTile(int index)
    {
        if (index >= 0 && index < _chainCount)
            return _chainArray[index];
        return new PlacedTile();
    }

    private int InstantiateLayout(TileLayout layout, Vector3 worldOrigin, int rotSteps, bool flip, Transform parent)
    {
        var map = new Dictionary<Vector2Int, TileLayout.TileType>(layout.tiles.Count);
        foreach (var t in layout.tiles)
        {
            if (t.grid.x < 0 || t.grid.x >= layout.gridWidth || t.grid.y < 0 || t.grid.y >= layout.gridHeight)
                continue;
            map[t.grid] = t.type;
        }

        int count = 0;
        for (int y = 0; y < layout.gridHeight; y++)
        {
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!map.TryGetValue(cell, out var type)) type = TileLayout.TileType.Grass;

                GameObject prefab = (type == TileLayout.TileType.Path) ? pathPrefab : grassPrefab;
                if (!prefab) continue;

                Vector3 basePos = worldOrigin + TileOrientationCalculator.CellToWorldLocal(cell, layout, rotSteps, flip);
                Vector3 offset = (type == TileLayout.TileType.Path) ? pathCellOffset : grassCellOffset;

                GameObject go;
                if (tilePoolManager != null)
                {
                    go = tilePoolManager.GetPooledObject(prefab, basePos + offset,
                        Quaternion.Euler(0f, rotSteps * 90f, 0f), parent);
                }
                else
                {
                    go = Instantiate(prefab, basePos + offset, Quaternion.Euler(0f, rotSteps * 90f, 0f), parent);
                }

                go.name = $"{prefab.name}_({x},{y})";
                count++;

                if (type == TileLayout.TileType.Grass)
                {
                    SetupGrassCell(go, layout);
                }
            }
        }
        return count;
    }

    private void SetupGrassCell(GameObject go, TileLayout layout)
    {
        go.tag = "Cell";
        if (go.GetComponent<Collider>() == null)
        {
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(layout.cellSize, 0.9f, layout.cellSize);
            col.center = new Vector3(0f, 0.05f, 0f);
        }

        if (go.GetComponent<CellSlot>() == null)
            go.AddComponent<CellSlot>();

        go.layer = LayerMask.NameToLayer("Cell");
    }

    private void EnsureChainCapacity(int requiredCapacity)
    {
        if (_chainArray.Length >= requiredCapacity) return;

        int newCapacity = Mathf.Max(_chainArray.Length * 2, requiredCapacity);
        PlacedTile[] newArray = new PlacedTile[newCapacity];
        Array.Copy(_chainArray, newArray, _chainCount);
        _chainArray = newArray;
    }

    private void AddToChain(PlacedTile tile)
    {
        EnsureChainCapacity(_chainCount + 1);
        _chainArray[_chainCount] = tile;
        _chainCount++;
    }

    private void EnsureExitsCapacity(int requiredCapacity)
    {
        if (_exitsArray.Length >= requiredCapacity) return;

        int newCapacity = Mathf.Max(_exitsArray.Length * 2, requiredCapacity);
        ExitRecord[] newArray = new ExitRecord[newCapacity];
        Array.Copy(_exitsArray, newArray, _exitsCount);
        _exitsArray = newArray;
    }

    private void AddToExits(ExitRecord exit)
    {
        EnsureExitsCapacity(_exitsCount + 1);
        _exitsArray[_exitsCount] = exit;
        _exitsCount++;
    }

    private void AddTileExitsToPool(int tileIndex, Vector2Int? excludeAssetCell)
    {
        var pt = GetPlacedTile(tileIndex);
        var exits = pt.layout.exits ?? new List<Vector2Int>();
        foreach (var ex in exits)
        {
            if (!pt.layout.IsInside(ex) || !pt.layout.IsPath(ex))
                continue;
            if (excludeAssetCell.HasValue && ex == excludeAssetCell.Value)
                continue;

            AddToExits(new ExitRecord
            {
                tileIndex = tileIndex,
                cell = ex,
                flags = 0,
                label = ""
            });
        }
        RelabelAvailableExits();
    }

    private void RelabelAvailableExits()
    {
        int n = 0;
        for (int i = 0; i < _exitsCount; i++)
        {
            if (_exitsArray[i].Used || _exitsArray[i].Closed) continue;
            _exitsArray[i].label = IndexToLetters(n++);
        }
    }

    private int GetGlobalExitIndexFromAvailable(int availableIdx)
    {
        int count = 0;
        for (int i = 0; i < _exitsCount; i++)
        {
            if (_exitsArray[i].Used || _exitsArray[i].Closed) continue;
            if (count == availableIdx) return i;
            count++;
        }
        return -1;
    }

    private int GetAvailableExitIndexByLabel(string label)
    {
        int count = 0;
        for (int i = 0; i < _exitsCount; i++)
        {
            if (_exitsArray[i].Used || _exitsArray[i].Closed) continue;
            if (_exitsArray[i].label == label) return count;
            count++;
        }
        return -1;
    }

    private static string IndexToLetters(int index)
    {
        string s = "";
        index += 1;
        while (index > 0)
        {
            int rem = (index - 1) % 26;
            s = (char)('A' + rem) + s;
            index = (index - 1) / 26;
        }
        return s;
    }

    private Vector3 ComputeNewOrigin(
        TileLayout candidate,
        int rotSteps,
        Vector2Int entryOriented,
        Vector2Int dirOut,
        Vector3 prevMin, Vector3 prevMax,
        Vector3 prevExitWorld)
    {
        TileOrientationCalculator.OrientedSize(candidate, rotSteps, out int w, out int h);
        float width = w * candidate.cellSize;
        float height = h * candidate.cellSize;

        Vector3 newOrigin = Vector3.zero;

        if (dirOut == Vector2Int.right)
        {
            newOrigin.x = prevMax.x + tileGap;
            float entryLocalZ = entryOriented.y * candidate.cellSize;
            newOrigin.z = prevExitWorld.z - entryLocalZ;
        }
        else if (dirOut == Vector2Int.left)
        {
            newOrigin.x = (prevMin.x - tileGap) - width;
            float entryLocalZ = entryOriented.y * candidate.cellSize;
            newOrigin.z = prevExitWorld.z - entryLocalZ;
        }
        else if (dirOut == Vector2Int.up)
        {
            newOrigin.z = prevMax.z + tileGap;
            float entryLocalX = entryOriented.x * candidate.cellSize;
            newOrigin.x = prevExitWorld.x - entryLocalX;
        }
        else // down
        {
            newOrigin.z = (prevMin.z - tileGap) - height;
            float entryLocalX = entryOriented.x * candidate.cellSize;
            newOrigin.x = prevExitWorld.x - entryLocalX;
        }

        newOrigin += extraTileOffset;
        return newOrigin;
    }

    private static Vector2Int? FindSinglePathNeighbor(TileLayout layout, Vector2Int cell)
    {
        int count = 0; Vector2Int found = default;
        foreach (var d in TileLayout.OrthoDirs)
        {
            var n = cell + d;
            if (layout.IsPath(n)) { count++; found = n; }
        }
        return count == 1 ? found : (Vector2Int?)null;
    }

    private static Vector2Int ClampToOrtho(Vector2Int v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return new Vector2Int(Mathf.Sign(v.x) >= 0 ? 1 : -1, 0);
        if (Mathf.Abs(v.y) > 0)
            return new Vector2Int(0, Mathf.Sign(v.y) >= 0 ? 1 : -1);
        return Vector2Int.up;
    }

    private bool CheckPrefabs()
    {
        if (grassPrefab == null || pathPrefab == null)
        {
            Debug.LogError("[GridGenerator] Asigná grassPrefab y pathPrefab.");
            return false;
        }
        return true;
    }

    private void EnsureRoot()
    {
        if (tilesRoot == null)
        {
            var found = GameObject.Find(rootName);
            if (found == null) found = new GameObject(rootName);
            tilesRoot = found.transform;
        }
    }

    private void ClearRootChildren()
    {
        if (tilePoolManager != null)
        {
            foreach (Transform child in tilesRoot)
            {
                if (child.CompareTag("Cell"))
                    tilePoolManager.ReturnToPool(grassPrefab, child.gameObject);
                else
                    tilePoolManager.ReturnToPool(pathPrefab, child.gameObject);
            }
        }
        else
        {
#if UNITY_EDITOR
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(tilesRoot.GetChild(i).gameObject);
#else
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                Destroy(tilesRoot.GetChild(i).gameObject);
#endif
        }
    }

    private Transform CreateTileParent(Transform root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    private bool HasAnyValidPlacementForExit(ExitRecord chosen)
    {
        var tile = GetPlacedTile(chosen.tileIndex);
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = TileOrientationCalculator.ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + TileOrientationCalculator.CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

        foreach (var candidate in candidateLayouts)
        {
            if (candidate == null) continue;

            int rotMax = allowRotations ? 4 : 1;
            for (int rot = 0; rot < rotMax; rot++)
            {
                int flipCount = allowFlip ? 2 : 1;
                for (int f = 0; f < flipCount; f++)
                {
                    bool flip = allowFlip && (f == 1);
                    var entry = candidate.entry;
                    if (!candidate.IsInside(entry) || !candidate.IsPath(entry)) continue;

                    var orientedData = orientationCalculator.GetOrientedData(candidate, rot, flip);
                    float width = orientedData.w * candidate.cellSize;
                    float height = orientedData.h * candidate.cellSize;

                    bool okBorde = false;
                    if (dirOut == Vector2Int.right) okBorde = (orientedData.entryOriented.x == 0);
                    if (dirOut == Vector2Int.left) okBorde = (orientedData.entryOriented.x == orientedData.w - 1);
                    if (dirOut == Vector2Int.up) okBorde = (orientedData.entryOriented.y == 0);
                    if (dirOut == Vector2Int.down) okBorde = (orientedData.entryOriented.y == orientedData.h - 1);
                    if (!okBorde) continue;

                    Vector3 newOrigin = ComputeNewOrigin(candidate, rot, orientedData.entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
                    Vector3 nMin = newOrigin;
                    Vector3 nMax = newOrigin + new Vector3(width, 0f, height);

                    if (!spatialPartitioner.OverlapsAnyOptimized(nMin, nMax))
                        return true;
                }
            }
        }
        return false;
    }
    #endregion
    #region Métodos adicionales para ShiftingWorldUI
    public List<TileLayout> GetRandomCandidateSet(int count)
    {
        var pool = new List<TileLayout>();
        foreach (var c in candidateLayouts)
            if (c != null) pool.Add(c);

        // Fisher–Yates shuffle
        for (int i = 0; i < pool.Count; i++)
        {
            int j = _rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        if (pool.Count > count)
            pool.RemoveRange(count, pool.Count - count);

        return pool;
    }

    public bool AppendNextUsingSelectedExitWithLayout(TileLayout forced)
    {
        if (forced == null) return false;

        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exitsCount == 0) return false;

        ExitRecord chosen = _exitsArray[globalIdx];
        if (chosen.Used || chosen.Closed) return false;

        int tileIdx = chosen.tileIndex;
        var tile = GetPlacedTile(tileIdx);
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = TileOrientationCalculator.ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + TileOrientationCalculator.CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

        int rotMax = allowRotations ? 4 : 1;
        for (int rot = 0; rot < rotMax; rot++)
        {
            int flipCount = allowFlip ? 2 : 1;
            for (int f = 0; f < flipCount; f++)
            {
                bool flip = allowFlip && (f == 1);

                var entry = forced.entry;
                if (!forced.IsInside(entry) || !forced.IsPath(entry)) continue;

                var orientedData = orientationCalculator.GetOrientedData(forced, rot, flip);
                float width = orientedData.w * forced.cellSize;
                float height = orientedData.h * forced.cellSize;

                bool okBorde = false;
                if (dirOut == Vector2Int.right) okBorde = (orientedData.entryOriented.x == 0);
                if (dirOut == Vector2Int.left) okBorde = (orientedData.entryOriented.x == orientedData.w - 1);
                if (dirOut == Vector2Int.up) okBorde = (orientedData.entryOriented.y == 0);
                if (dirOut == Vector2Int.down) okBorde = (orientedData.entryOriented.y == orientedData.h - 1);
                if (!okBorde) continue;

                Vector3 newOrigin = ComputeNewOrigin(forced, rot, orientedData.entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
                Vector3 nMin = newOrigin;
                Vector3 nMax = newOrigin + new Vector3(width, 0f, height);

                if (spatialPartitioner.OverlapsAnyOptimized(nMin, nMax)) continue;

                var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chainCount);
                int count = InstantiateLayout(forced, newOrigin, rot, flip, parent);

                var placed = new PlacedTile
                {
                    layout = forced,
                    worldOrigin = newOrigin,
                    rotSteps = rot,
                    flipped = flip,
                    parent = parent,
                    aabbMin = nMin,
                    aabbMax = nMax
                };

                AddToChain(placed);
                spatialPartitioner.AddToSpatialGrid(placed, _chainCount - 1);
                chosen.SetUsed(true);
                _exitsArray[globalIdx] = chosen;
                RelabelAvailableExits();
                AddTileExitsToPool(_chainCount - 1, excludeAssetCell: forced.entry);

                Debug.Log($"[GridGenerator] Conectado con layout forzado {forced.name} (rot={rot * 90}°, flip={flip}). Instancias: {count}");
                return true;
            }
        }

        if (!HasAnyValidPlacementForExit(chosen))
        {
            chosen.SetClosed(true);
            chosen.label = "EXIT CERRADO";
            _exitsArray[globalIdx] = chosen;
            RelabelAvailableExits();
            selectedExitIndex = 0;
        }
        return false;
    }
    #endregion
}