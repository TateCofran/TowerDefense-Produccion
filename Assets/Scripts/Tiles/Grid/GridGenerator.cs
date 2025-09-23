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

    [Header("Core")]
    [SerializeField] private GameObject corePrefab; // Prefab del Core
    [SerializeField] private Vector3 coreOffset = Vector3.zero; // Offset para posicionamiento

    private GameObject spawnedCore; // Referencia al Core instanciado

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
    private List<Vector3> spawnPoints = new List<Vector3>();

    [SerializeField] private int selectedExitIndex = 0;

    #region Spawn Exits & Gizmos

    [Header("Spawn / Exits")]
    [SerializeField] private float minDistanceToCore = 0f;
    [SerializeField] private bool excludeCoreNeighbors = true;

    [Header("Gizmos de Exits")]
    [SerializeField] private bool drawExitGizmos = true;
    [SerializeField] private bool drawExitLabels = true;
    [SerializeField] private float exitGizmoY = 0.05f;
    [SerializeField] private float exitSphereRadius = 0.18f;
    [SerializeField] private Color colorOpenExit = new Color(0.1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color colorUsedOrClosedExit = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private Color colorLastUsed = new Color(1f, 1f, 0.2f, 1f);
    [SerializeField] private Color colorCore = new Color(0f, 0.9f, 0.9f, 1f);

    [Header("Historial de spawns usados")]
    [SerializeField] private int lastUsedCapacity = 6;
    private readonly Queue<Vector3> _lastUsedSpawns = new Queue<Vector3>();

    // Round-robin opcional
    private int _spawnRR = 0;

    #endregion
    #region Spawn Exits Helpers
    // === EXIT RUNNERS ===
    [Header("Exit Runners")]
    [SerializeField] private GameObject exitRunnerPrefab; // Prefab a spawnear en cada EXIT abierto
    [SerializeField] private float exitRunnerSpeed = 3.5f;
    [SerializeField] private float exitRunnerHeightOffset = 0.05f;
    [SerializeField] private bool autoSpawnRunnersOnGenerate = false;   // Opcional: spawnear automáticamente en GenerateFirst
    [SerializeField] private bool autoSpawnRunnersOnAppend = false;     // Opcional: spawnear automáticamente tras un Append

    [Header("Exit Runners (Path)")]

    [SerializeField] private float runnerSpeed = 3.5f;
    [SerializeField] private float runnerYOffset = 0.05f;

    private bool IsCoreNeighborWorld(Vector3 exitWorld)
    {
        var core = GameObject.FindGameObjectWithTag("Core")?.transform;
        if (!core) return false;

        // Vecinos inmediatos: chequeo por distancia ~ cellSize/2
        float cell = (_chainCount > 0 && _chainArray[0].layout) ? _chainArray[0].layout.cellSize : 1f;
        return Vector3.Distance(new Vector3(exitWorld.x, 0f, exitWorld.z), new Vector3(core.position.x, 0f, core.position.z)) < (cell * 1.1f);
    }

    private Vector3 ExitToWorld(ExitRecord ex)
    {
        var pt = GetPlacedTile(ex.tileIndex);
        return pt.worldOrigin + TileOrientationCalculator.CellToWorldLocal(ex.cell, pt.layout, pt.rotSteps, pt.flipped);
    }

    /// <summary>
    /// Devuelve todas las salidas “abiertas” (no Used ni Closed) en coordenadas de mundo.
    /// </summary>
    private List<Vector3> GetOpenExitWorldPositions()
    {
        var list = new List<Vector3>();
        for (int i = 0; i < _exitsCount; i++)
        {
            var ex = _exitsArray[i];
            if (ex.Used || ex.Closed) continue;
            list.Add(ExitToWorld(ex));
        }
        return list;
    }

    private void MarkSpawnUsed(Vector3 worldPos)
    {
        _lastUsedSpawns.Enqueue(worldPos);
        while (_lastUsedSpawns.Count > Mathf.Max(1, lastUsedCapacity))
            _lastUsedSpawns.Dequeue();
    }

    #endregion

    private void Start()
    {
        InitializeDependencies();


        GenerateFirst(); // crea el primer tile y (si corresponde) el Core

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            var exits = GetAvailableExits(); // (label, worldPos) solo abiertos/No usados
            foreach (var (_, worldPos) in exits)
                SpawnRunnerFollowingPath(worldPos);
        }
    }

    private void InitializeDependencies()
    {
        if (tilePoolManager == null) tilePoolManager = GetComponent<TilePoolManager>();
        if (spatialPartitioner == null) spatialPartitioner = GetComponent<SpatialPartitioner>();
        if (orientationCalculator == null) orientationCalculator = GetComponent<TileOrientationCalculator>();

        tilePoolManager?.InitializePools();
    }

    #region Métodos Públicos Adicionales
    public int GetChainCount()
    {
        return _chainCount;
    }

    public PlacedTile GetPlacedTile(int index)
    {
        if (index >= 0 && index < _chainCount)
            return _chainArray[index];
        return new PlacedTile();
    }

    public List<PlacedTile> GetAllPlacedTiles()
    {
        var tiles = new List<PlacedTile>();
        for (int i = 0; i < _chainCount; i++)
        {
            tiles.Add(_chainArray[i]);
        }
        return tiles;
    }

    public List<Vector3> SpawnPoints
    {
        get { return new List<Vector3>(spawnPoints); }
    }

    #endregion

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

            if (spawnedCore != null)
            {
                Destroy(spawnedCore);
                spawnedCore = null;
            }
        }

        int rot = 0; bool flip = false;
        Vector3 worldOrigin = initialLayout.origin;

        var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chainCount);
        int count = InstantiateLayout(initialLayout, worldOrigin, rot, flip, parent);

        if (initialLayout.hasCore)
        {
            SpawnCoreAtEntry(initialLayout, worldOrigin, rot, flip);
        }

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
        if (autoSpawnRunnersOnGenerate)
            SpawnRunnersAtAllOpenExits();
    }

    private void SpawnCoreAtEntry(TileLayout layout, Vector3 worldOrigin, int rotSteps, bool flip)
    {
        if (corePrefab == null)
        {
            Debug.LogWarning("[GridGenerator] No hay corePrefab asignado.");
            return;
        }

        if (!layout.hasCore)
        {
            Debug.Log($"[GridGenerator] Layout {layout.name} no genera Core (hasCore = false)");
            return;
        }

        Vector2Int coreCell = layout.GetCoreCell();

        if (!layout.IsPath(coreCell))
        {
            Debug.LogWarning($"[GridGenerator] La celda del Core ({coreCell}) no es un Path. Usando entry.");
            coreCell = layout.entry;

            if (!layout.IsPath(coreCell))
            {
                for (int y = 0; y < layout.gridHeight; y++)
                {
                    for (int x = 0; x < layout.gridWidth; x++)
                    {
                        var testCell = new Vector2Int(x, y);
                        if (layout.IsPath(testCell))
                        {
                            coreCell = testCell;
                            break;
                        }
                    }
                    if (layout.IsPath(coreCell)) break;
                }
            }
        }

        Vector2Int coreOriented = TileOrientationCalculator.ApplyOrientationToCell(coreCell, layout, rotSteps, flip);
        Vector3 corePosition = worldOrigin + TileOrientationCalculator.CellToWorldLocal(coreOriented, layout, rotSteps, flip);
        corePosition += coreOffset;

        spawnedCore = Instantiate(corePrefab, corePosition, Quaternion.identity, tilesRoot);
        spawnedCore.name = "Core";

        SetupCoreCell(spawnedCore);

        Debug.Log($"[GridGenerator] Core instanciado en celda {coreCell} -> posición: {corePosition}");
    }

    public Vector3 GetCorePosition()
    {
        return spawnedCore != null ? spawnedCore.transform.position : Vector3.zero;
    }

    private void SetupCoreCell(GameObject coreObject)
    {
        if (coreObject == null) return;

        coreObject.tag = "Core";

        if (coreObject.GetComponent<Collider>() == null)
        {
            var collider = coreObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }

        if (coreObject.GetComponent<Core>() == null)
        {
            coreObject.AddComponent<Core>();
        }
    }

    public bool HasCore()
    {
        return spawnedCore != null;
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
            if (autoSpawnRunnersOnAppend)
                SpawnRunnersAtAllOpenExits();

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

    private int InstantiateLayout(TileLayout layout, Vector3 worldOrigin, int rotSteps, bool flip, Transform parent)
    {
        var map = new Dictionary<Vector2Int, TileLayout.TileType>(layout.tiles.Count);
        foreach (var t in layout.tiles)
        {
            if (t.grid.x < 0 || t.grid.x >= layout.gridWidth || t.grid.y < 0 || t.grid.y >= layout.gridHeight)
                continue;
            map[t.grid] = t.type;
        }

        // Obtener la celda del Core solo si hasCore es true
        Vector2Int? coreCellToSkip = null;
        if (layout.hasCore)
        {
            Vector2Int coreCell = layout.GetCoreCell();
            Vector2Int coreOriented = TileOrientationCalculator.ApplyOrientationToCell(coreCell, layout, rotSteps, flip);
            coreCellToSkip = coreOriented;
        }

        int count = 0;
        for (int y = 0; y < layout.gridHeight; y++)
        {
            for (int x = 0; x < layout.gridWidth; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!map.TryGetValue(cell, out var type)) type = TileLayout.TileType.Grass;

                Vector2Int orientedCell = TileOrientationCalculator.ApplyOrientationToCell(cell, layout, rotSteps, flip);

                // Saltar la celda del Core solo si hasCore es true y es la celda correcta
                if (layout.hasCore && coreCellToSkip.HasValue && orientedCell == coreCellToSkip.Value)
                {
                    Debug.Log($"[GridGenerator] Saltando tile en celda del Core: {cell} -> {orientedCell}");
                    continue;
                }

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

    #region Métodos para Spawn en Finales de Camino

    // Método principal: Obtener puntos de spawn en finales de camino (exits abiertos)
    public List<Vector3> GetSpawnPoints()
    {
        var result = new List<Vector3>();

        // 1) Tomamos exits abiertos (los que todavía no se usaron ni cerraron)
        var openExits = GetOpenExitWorldPositions();

        // 2) Filtramos por distancia al Core y vecinos del Core si se pide
        Transform core = GameObject.FindGameObjectWithTag("Core")?.transform;
        foreach (var w in openExits)
        {
            if (excludeCoreNeighbors && IsCoreNeighborWorld(w))
                continue;

            if (core && minDistanceToCore > 0f)
            {
                if (Vector3.Distance(new Vector3(w.x, 0f, w.z), new Vector3(core.position.x, 0f, core.position.z)) < minDistanceToCore)
                    continue;
            }

            // Evitar duplicados por floating point
            bool duplicate = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (Vector3.Distance(result[i], w) < 0.05f) { duplicate = true; break; }
            }
            if (!duplicate) result.Add(w);
        }

        // 3) Fallback si no hay
        if (result.Count == 0)
            result = GetFallbackSpawnPoints();

        // 4) Guardamos en cache pública si querés consultarlo desde afuera
        spawnPoints = new List<Vector3>(result);
        return result;
    }
    /// <summary>Devuelve el próximo spawn point en round-robin.</summary>
    public Vector3 GetNextSpawnRoundRobin()
    {
        var points = GetSpawnPoints();
        if (points.Count == 0) return Vector3.zero;
        if (_spawnRR < 0) _spawnRR = 0;
        var idx = _spawnRR % points.Count;
        _spawnRR = (_spawnRR + 1) % points.Count;
        var chosen = points[idx];
        MarkSpawnUsed(chosen);
        return chosen;
    }
    private void OnDrawGizmos()
    {
        if (!drawExitGizmos) return;

        // Core
        var core = GameObject.FindGameObjectWithTag("Core")?.transform;
        if (core)
        {
            Gizmos.color = colorCore;
            Gizmos.DrawCube(new Vector3(core.position.x, exitGizmoY, core.position.z), Vector3.one * (exitSphereRadius * 1.2f));
#if UNITY_EDITOR
            if (drawExitLabels)
                UnityEditor.Handles.Label(core.position + Vector3.up * (exitSphereRadius * 1.6f), "CORE", new GUIStyle(UnityEditor.EditorStyles.boldLabel) { fontSize = 11 });
#endif
        }

        // Exits abiertos (verdes)
        var open = GetOpenExitWorldPositions();
        Gizmos.color = colorOpenExit;
        for (int i = 0; i < open.Count; i++)
        {
            var p = open[i]; p.y = exitGizmoY;
            Gizmos.DrawWireSphere(p, exitSphereRadius);
#if UNITY_EDITOR
            if (drawExitLabels)
                UnityEditor.Handles.Label(p + Vector3.up * (exitSphereRadius * 1.4f), $"EXIT {i}", new GUIStyle(UnityEditor.EditorStyles.boldLabel) { fontSize = 11 });
#endif
        }

        // Exits usados o cerrados (naranja)
        Gizmos.color = colorUsedOrClosedExit;
        for (int i = 0; i < _exitsCount; i++)
        {
            var ex = _exitsArray[i];
            if (!ex.Used && !ex.Closed) continue;
            var p = ExitToWorld(ex); p.y = exitGizmoY;
            Gizmos.DrawSphere(p, exitSphereRadius * 0.85f);
#if UNITY_EDITOR
            if (drawExitLabels)
                UnityEditor.Handles.Label(p + Vector3.up * (exitSphereRadius * 1.1f), ex.Closed ? "CERRADO" : "USADO", new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel));
#endif
        }

        // Últimos usados (amarillo)
        Gizmos.color = colorLastUsed;
        foreach (var u in _lastUsedSpawns)
        {
            var p = new Vector3(u.x, exitGizmoY, u.z);
            Gizmos.DrawSphere(p, exitSphereRadius * 0.7f);
#if UNITY_EDITOR
            if (drawExitLabels)
                UnityEditor.Handles.Label(p + Vector3.up * (exitSphereRadius * 0.9f), "used", new GUIStyle(UnityEditor.EditorStyles.miniBoldLabel));
#endif
        }
    }


    // Verificar si una celda es válida para spawn
    private bool IsValidSpawnPoint(PlacedTile tile, Vector2Int cell)
    {
        if (!tile.layout.IsInside(cell) || !tile.layout.IsPath(cell))
            return false;

        return CountPathNeighbors(tile, cell) == 1;
    }

    private int CountPathNeighbors(PlacedTile tile, Vector2Int cell)
    {
        int pathNeighbors = 0;
        foreach (var dir in TileLayout.OrthoDirs)
        {
            var neighbor = cell + dir;
            if (tile.layout.IsInside(neighbor) && tile.layout.IsPath(neighbor))
            {
                pathNeighbors++;
            }
        }
        return pathNeighbors;
    }

    private Vector3 CalculateWorldPosition(PlacedTile tile, Vector2Int cell)
    {
        return tile.worldOrigin +
            TileOrientationCalculator.CellToWorldLocal(cell, tile.layout, tile.rotSteps, tile.flipped);
    }

    private bool IsDuplicateSpawnPoint(Vector3 newPoint, List<Vector3> existingPoints, float threshold = 0.1f)
    {
        foreach (var point in existingPoints)
        {
            if (Vector3.Distance(point, newPoint) < threshold)
                return true;
        }
        return false;
    }

    private List<Vector3> GetFallbackSpawnPoints()
    {
        var fallbackPoints = new List<Vector3>();

        if (_chainCount > 0)
        {
            var lastTile = GetPlacedTile(_chainCount - 1);
            if (lastTile.layout?.exits != null)
            {
                foreach (var exitCell in lastTile.layout.exits)
                {
                    Vector3 worldPos = CalculateWorldPosition(lastTile, exitCell);
                    fallbackPoints.Add(worldPos);
                }
            }

            if (fallbackPoints.Count == 0)
            {
                Vector3 cornerPos = lastTile.worldOrigin + new Vector3(lastTile.layout.cellSize, 0, lastTile.layout.cellSize);
                fallbackPoints.Add(cornerPos);
            }
        }

        Debug.LogWarning("[Spawn] Usando spawn points de fallback");
        return fallbackPoints;
    }

    public void DebugSpawnPoints()
    {
        var spawnPoints = GetSpawnPoints();
        Debug.Log($"=== SPAWN POINTS DEBUG ===");
        Debug.Log($"Total points: {spawnPoints.Count}");

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Debug.Log($"Point {i}: {spawnPoints[i]}");
        }
    }
    #endregion
    // Devuelve la posición del Core si existe
    private bool TryGetCoreTransform(out Transform coreT)
    {
        coreT = null;
        if (spawnedCore != null) { coreT = spawnedCore.transform; return true; }
        var coreGo = GameObject.FindGameObjectWithTag("Core");
        if (coreGo) { coreT = coreGo.transform; return true; }
        return false;
    }

    // Spawnea UN runner en un punto de inicio específico
   /* private void SpawnExitRunnerAt(Vector3 startWorldPos)
    {
        if (exitRunnerPrefab == null)
        {
            Debug.LogWarning("[GridGenerator] exitRunnerPrefab no asignado.");
            return;
        }
        if (!TryGetCoreTransform(out var coreT))
        {
            Debug.LogWarning("[GridGenerator] No hay Core en escena para dirigir el runner.");
            return;
        }

        // Ajuste de altura para que no quede "pegado" al piso
        var spawnPos = new Vector3(startWorldPos.x, startWorldPos.y + exitRunnerHeightOffset, startWorldPos.z);
        var go = Instantiate(exitRunnerPrefab, spawnPos, Quaternion.identity, tilesRoot);
        go.name = "ExitRunner";

        var runner = go.GetComponent<Enemy>();
        if (!runner) runner = go.AddComponent<Enemy>();

        runner.Init(coreT, exitRunnerSpeed);
    }
    private void SpawnRunnerAtExit(Vector3 startWorldPos)
    {
        if (exitRunnerPrefab == null)
        {
            Debug.LogWarning("[GridGenerator] exitRunnerPrefab no asignado.");
            return;
        }

        // Core: primero el instanciado, si no, por tag
        Transform coreT = null;
        if (spawnedCore != null) coreT = spawnedCore.transform;
        else
        {
            var coreGo = GameObject.FindGameObjectWithTag("Core");
            if (coreGo) coreT = coreGo.transform;
        }
        if (coreT == null)
        {
            Debug.LogWarning("[GridGenerator] No hay Core en escena, el Runner no tendrá destino.");
            // igual lo instanciamos; ExitRunner se autoconfigurará si aparece un Core luego
        }

        Vector3 spawnPos = startWorldPos + Vector3.up * exitRunnerHeightOffset;
        var go = Instantiate(exitRunnerPrefab, spawnPos, Quaternion.identity);
        go.name = "ExitRunner";

        var runner = go.GetComponent<ExitRunner>();
        if (runner == null) runner = go.AddComponent<ExitRunner>();
        runner.Init(coreT, Mathf.Max(0.01f, exitRunnerSpeed));
    }*/

    // Spawnea runners en TODOS los exits abiertos actuales
    public void SpawnRunnersAtAllOpenExits()
    {
        var exits = GetAvailableExits(); // (label, worldPos) SOLO los no usados/ni cerrados
        if (exits.Count == 0)
        {
            Debug.Log("[GridGenerator] No hay exits abiertos para spawnear runners.");
            return;
        }

        foreach (var (_, worldPos) in exits)
            SpawnRunnerFollowingPath(worldPos);
    }
    // Suponemos un cellSize único en layouts (si los tuyos varían, avisame y lo adapto)
    private float GlobalCellSize => (_chainCount > 0 && _chainArray[0].layout) ? _chainArray[0].layout.cellSize : 1f;

    private Vector3 SnapToCell(Vector3 w)
    {
        float cs = Mathf.Max(0.0001f, GlobalCellSize);
        float x = Mathf.Round(w.x / cs) * cs;
        float z = Mathf.Round(w.z / cs) * cs;
        return new Vector3(x, spawnedCore ? spawnedCore.transform.position.y : w.y, z);
    }

    private Vector3Int WorldToKey(Vector3 w)
    {
        float cs = Mathf.Max(0.0001f, GlobalCellSize);
        return new Vector3Int(Mathf.RoundToInt(w.x / cs), 0, Mathf.RoundToInt(w.z / cs));
    }

    private Vector3 KeyToWorld(Vector3Int k)
    {
        float cs = Mathf.Max(0.0001f, GlobalCellSize);
        return new Vector3(k.x * cs, spawnedCore ? spawnedCore.transform.position.y : 0f, k.z * cs);
    }
    // Devuelve set de nodos (keys) y adyacencia 4-dir
    private void BuildPathGraph(out HashSet<Vector3Int> nodes, out Dictionary<Vector3Int, List<Vector3Int>> adj)
    {
        nodes = new HashSet<Vector3Int>();
        adj = new Dictionary<Vector3Int, List<Vector3Int>>();

        float cs = GlobalCellSize;

        // 1) registrar cada celda Path como nodo
        for (int i = 0; i < _chainCount; i++)
        {
            var pt = _chainArray[i];
            if (pt.layout == null) continue;

            foreach (var t in pt.layout.tiles)
            {
                if (t.type != TileLayout.TileType.Path) continue;

                // celda orientada -> world center
                var w = pt.worldOrigin + TileOrientationCalculator.CellToWorldLocal(
                    t.grid, pt.layout, pt.rotSteps, pt.flipped);

                var k = WorldToKey(w);
                if (nodes.Add(k))
                    adj[k] = new List<Vector3Int>();
            }
        }

        // 2) conectar vecinos ortogonales
        var dirs = new Vector3Int[]
        {
        new Vector3Int( 1,0, 0), new Vector3Int(-1,0, 0),
        new Vector3Int( 0,0, 1), new Vector3Int( 0,0,-1),
        };

        foreach (var k in nodes)
        {
            foreach (var d in dirs)
            {
                var nb = new Vector3Int(k.x + d.x, 0, k.z + d.z);
                if (nodes.Contains(nb))
                    adj[k].Add(nb);
            }
        }
    }
    private bool TryBuildRouteExitToCore(Vector3 exitWorld, out List<Vector3> route)
    {
        route = null;
        if (spawnedCore == null) return false;

        BuildPathGraph(out var nodes, out var adj);

        var startKey = WorldToKey(SnapToCell(exitWorld));
        var goalKey = WorldToKey(SnapToCell(spawnedCore.transform.position));

        if (!nodes.Contains(startKey) || !nodes.Contains(goalKey))
        {
            // Si el EXIT o el CORE no caen exactamente en nodos, intentá “snapear” a vecinos inmediatos
            // (simple: si no está, abortamos y que vaya en línea recta como fallback)
            return false;
        }

        // BFS
        var q = new Queue<Vector3Int>();
        var prev = new Dictionary<Vector3Int, Vector3Int>();
        var visited = new HashSet<Vector3Int>();

        q.Enqueue(startKey);
        visited.Add(startKey);

        bool found = false;
        while (q.Count > 0)
        {
            var u = q.Dequeue();
            if (u == goalKey) { found = true; break; }

            foreach (var v in adj[u])
            {
                if (visited.Contains(v)) continue;
                visited.Add(v);
                prev[v] = u;
                q.Enqueue(v);
            }
        }

        if (!found) return false;

        // reconstruir path
        var keys = new List<Vector3Int>();
        for (var cur = goalKey; ;)
        {
            keys.Add(cur);
            if (cur == startKey) break;
            cur = prev[cur];
        }
        keys.Reverse();

        // A world + pequeño offset Y para que “flote” un toque
        route = new List<Vector3>(keys.Count);
        foreach (var k in keys)
        {
            var w = KeyToWorld(k);
            w.y += runnerYOffset;
            route.Add(w);
        }
        return route.Count > 0;
    }
    private void SpawnRunnerFollowingPath(Vector3 exitWorldPos)
    {
        if (exitRunnerPrefab == null)
        {
            Debug.LogWarning("[GridGenerator] exitRunnerPrefab no asignado.");
            return;
        }

        if (!TryBuildRouteExitToCore(exitWorldPos, out var path))
        {
            // Fallback: si no se pudo armar ruta, usá el primer y último punto para que no quede clavado
            path = new List<Vector3> { exitWorldPos + Vector3.up * runnerYOffset };
            if (spawnedCore) path.Add(new Vector3(spawnedCore.transform.position.x,
                                                  (spawnedCore.transform.position.y + runnerYOffset),
                                                  spawnedCore.transform.position.z));
        }

        var go = Instantiate(exitRunnerPrefab, path[0], Quaternion.identity);
        go.name = "Enemy";

        var mover = go.GetComponent<Enemy>();
        if (mover == null) mover = go.AddComponent<Enemy>();
        mover.Init(path, runnerSpeed);
    }

}
