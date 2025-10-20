using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridGenerator : MonoBehaviour, ITileGenerator
{
    [Header("Dependencies")][SerializeField] private TilePoolManager tilePoolManager;
    [SerializeField] private SpatialPartitioner spatialPartitioner;
    [SerializeField] private TileOrientationCalculator orientationCalculator;
    [SerializeField] private bool generateOnStart = true;

    [Header("Layouts")][SerializeField] private TileLayout initialLayout; [SerializeField] private List<TileLayout> candidateLayouts = new();

    [Header("Prefabs")][SerializeField] private GameObject grassPrefab, pathBasicPrefab, pathDamagePrefab, pathSlowPrefab, pathStunPrefab;

    [Header("Core")][SerializeField] private GameObject corePrefab; [SerializeField] private Vector3 coreOffset = Vector3.zero;

    [Header("Parent / Salida")][SerializeField] private Transform tilesRoot; [SerializeField] private string rootName = "TileChainRoot"; [SerializeField] private string tileGroupBaseName = "Tile_";

    [Header("Cadena")][SerializeField] private bool clearOnStartGenerate = true; [SerializeField] private int connectionTries = 24;

    [Header("Offsets por celda")][SerializeField] private Vector3 grassCellOffset = Vector3.zero; [SerializeField] private Vector3 pathCellOffset = Vector3.zero;

    [Header("Offset entre tiles")][SerializeField] private float tileGap = 0.25f; [SerializeField] private Vector3 extraTileOffset = Vector3.zero;

    [Header("Orientaciones")][SerializeField] private bool allowRotations = true; [SerializeField] private bool allowFlip = true; [SerializeField] private bool worldPlusZUsesYZeroEdge = true;

    [Header("UI / Exits")][SerializeField] private int selectedExitIndex = 0;

    [Header("Spawn/Runner")][SerializeField] private float minDistanceToCore = 0f; [SerializeField] private bool excludeCoreNeighbors = true;
    [SerializeField] private GameObject exitRunnerPrefab; [SerializeField] private float runnerYOffset = 0.05f; [SerializeField] private bool autoSpawnRunnersOnAppend = false;

    // Servicios / repos / adapters
    private IChainRepository chain;
    private IExitRepository exits;
    private ISpatialOverlapChecker overlap;
    private IOrientationService orient;

    private IPrefabSelector prefabs;
    private ILayoutInstantiator instantiator;
    private IPlacementCalculator placement;
    private ICandidateProvider candidates;

    private ICoreService core;
    private ISpawnPointsService spawns;
    private IRunnerService runners;


    private System.Random rng = new System.Random();

    private void Awake()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        // 1) Components (si faltan, los busca en el mismo GO)
        if (tilePoolManager == null) tilePoolManager = GetComponent<TilePoolManager>();
        if (spatialPartitioner == null) spatialPartitioner = GetComponent<SpatialPartitioner>();
        if (orientationCalculator == null) orientationCalculator = GetComponent<TileOrientationCalculator>();

        // 2) Orientation + repos
        orient = new OrientationAdapter(orientationCalculator);
        chain = new ChainRepository();
        exits = new ExitRepository(orient);

        // 3) Spatial overlap (AABB) vía adapter a tu partitioner
        overlap = new SpatialOverlapAdapter(spatialPartitioner);

        // 4) Pooling (sin dependencia circular con GridGenerator)
        if (tilePoolManager != null)
        {
            tilePoolManager.InitializePool(grassPrefab, 64);
            tilePoolManager.InitializePool(pathBasicPrefab, 64);
            tilePoolManager.InitializePool(pathDamagePrefab, 32);
            tilePoolManager.InitializePool(pathSlowPrefab, 32);
            tilePoolManager.InitializePool(pathStunPrefab, 32);
        }

        // 5) Prefab selector + instanciador de layout
        prefabs = new PrefabSelector(grassPrefab, pathBasicPrefab, pathDamagePrefab, pathSlowPrefab, pathStunPrefab);
        instantiator = new LayoutInstantiator(prefabs, orient, tilePoolManager, grassCellOffset, pathCellOffset);

        // 6) Cálculo de placement y candidatos válidos
        placement = new PlacementCalculator(tileGap, extraTileOffset);
        candidates = new CandidateProvider(worldPlusZUsesYZeroEdge);

        // 7) Core + spawns + runners
        core = new CoreService(corePrefab, tilesRoot);

        // Elegimos un cellSize base (si tenés layouts heterogéneos, esto funciona bien igual)
        float baseCellSize = (initialLayout != null) ? initialLayout.cellSize : 1f;

        spawns = new SpawnPointsService(
            exits, chain, core, orient,
            baseCellSize,            // << acá está el cellSize que te faltaba
            minDistanceToCore,
            excludeCoreNeighbors
        );

        runners = new RunnerService(
            exits, chain, core, orient,
            exitRunnerPrefab,
            runnerYOffset
        );
    }

    

    private void Start()
    {
        if (generateOnStart) GenerateFirst();
    }


    private float GetGlobalCellSize() => (initialLayout ? initialLayout.cellSize : 1f);


    public void GenerateFirst()
    {
        if (!CheckPrefabs()) return;
        if (initialLayout == null)
        {
            Debug.LogError("[GridGenerator] Asigná initialLayout.");
            return;
        }

        // Limpieza respetando tu flag
        if (clearOnStartGenerate)
        {
            ClearRootChildren();     // <-- ya existe en tu clase
            exits.Clear();
            chain.Clear();
            overlap.Clear();         // ISpatialOverlapChecker.Clear()
            core?.ClearCore();       // ICoreService
        }

        tilesRoot = EnsureRoot(tilesRoot, rootName);

        // Grupo visual del primer tile (usa tu método)
        var group = CreateTileParent(tilesRoot, $"{tileGroupBaseName}{chain.Count}");

        // Orientación inicial
        int rot = 0;
        bool flip = false;
        Vector3 origin = initialLayout.origin;

        // Instanciación de celdas
        int count = instantiator.InstantiateLayout(initialLayout, origin, rot, flip, group);

        // Spawn del Core (usando CoreService) si corresponde
        if (corePrefab != null)
        {
            // Tomamos coreCell (si hasCore) o entry como fallback
            Vector2Int coreCell = initialLayout.hasCore ? initialLayout.GetCoreCell() : initialLayout.entry;
            if (initialLayout.IsInside(coreCell) && initialLayout.IsPath(coreCell))
            {
                Vector3 corePos = origin + orient.CellToWorldLocal(coreCell, initialLayout, rot, flip) + coreOffset;
                core.SpawnCore(corePos);
            }
        }

        // AABB del tile colocado
        var od = orient.GetOrientedData(initialLayout, rot, flip);
        placement.ComputeAABB(origin, od.w, od.h, initialLayout.cellSize, out var aMin, out var aMax);

        // Registrar en cadena + espacial
        var placed = new PlacedTile
        {
            layout = initialLayout,
            worldOrigin = origin,
            rotSteps = rot,
            flipped = flip,
            parent = group,
            aabbMin = aMin,
            aabbMax = aMax
        };
        chain.Add(placed);
        overlap.Add(placed, chain.Count - 1);

        // Registrar exits del primer tile
        AddTileExitsToPool(chain.Count - 1, excludeAssetCell: null);
        exits.Relabel();

        int abiertos = exits.IndicesDisponibles().Count();
        Debug.Log($"[GridGenerator] Primer tile instanciado ({count} celdas). Exits disponibles: {abiertos}");
    }



    public void AppendNextUsingSelectedExit()
        {
            int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
            if (global < 0) { Debug.LogWarning("[GridGenerator] No hay EXITS disponibles."); return; }
            var chosen = exits.Get(global);
            if (TryAppendAtExit(chosen)) { exits.MarkUsed(global); exits.Relabel(); if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
            else if (!HasAnyValidPlacementForExit(chosen)) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
        }

    public IEnumerator AppendNextAsync()
    {
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) yield break;
        var chosen = exits.Get(global); if (chosen.Used) yield break;
        bool success = false;
        for (int i = 0; i < connectionTries; i++) { if (TryAppendAtExit(chosen)) { success = true; break; } yield return null; }
        if (success) { exits.MarkUsed(global); exits.Relabel(); if (autoSpawnRunnersOnAppend) runners.SpawnRunnersAtAllOpenExits(); }
        else if (!HasAnyValidPlacementForExit(chosen)) { exits.MarkClosed(global); exits.Relabel(); selectedExitIndex = 0; }
    }

    // ── Queries para UI ──────────────────────────────────────
    public List<PlacementPreview> GetPlacementPreviews()
    {
        var previews = new List<PlacementPreview>();
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return previews;
        var chosen = exits.Get(global); if (chosen.Used) return previews;

        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return previews;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return previews;
        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        foreach (var (candidate, rot, flip) in candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient))
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            bool noOverlap = !overlap.OverlapsAny(nMin, nMax);
            var status = noOverlap ? PreviewStatus.Valid : PreviewStatus.Overlap;
            previews.Add(new PlacementPreview { origin = newOrigin, sizeXZ = new Vector2(o.w * candidate.cellSize, o.h * candidate.cellSize), valid = noOverlap, status = status, note = $"{candidate.name} | {rot * 90}° | flip={(flip ? 1 : 0)}", cellSize = candidate.cellSize });
        }
        return previews;
    }

    public bool TryGetSelectedExitWorld(out Vector3 pos, out Vector2Int dirOutWorld)
    {
        pos = Vector3.zero; dirOutWorld = Vector2Int.zero;
        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return false;
        var chosen = exits.Get(global); if (chosen.Used) return false;

        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;

        Vector2Int d = ClampToOrtho(orient.ApplyInverseToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        pos = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);
        dirOutWorld = d; return true;
    }

    public List<(string label, Vector3 worldPos)> GetAvailableExits()
    {
        return exits.GetAvailableWorld(chain.Get).ToList();
    }

    public List<string> GetAvailableExitLabels()
    {
        return exits.IndicesDisponibles().Select(i => exits.Get(i).label).ToList();
    }

    public void UI_SetExitIndex(int index) => selectedExitIndex = Mathf.Max(0, index);
    public void UI_SetExitByLabel(string label) { int idx = exits.AvailableIndexByLabel(label); if (idx >= 0) selectedExitIndex = idx; }
    public void UI_GenerateFirst() => GenerateFirst();
    public void UI_AppendNext() => AppendNextUsingSelectedExit();
    public void UI_AppendNextAsync() => StartCoroutine(AppendNextAsync());

    // ── Internals ────────────────────────────────────────────
    private bool TryAppendAtExit(ExitRecord chosen)
    {
        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;

        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        var shuffled = candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient).OrderBy(_ => rng.Next());
        int attempts = 0;
        foreach (var (candidate, rot, flip) in shuffled)
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            if (overlap.OverlapsAny(nMin, nMax)) { attempts++; if (attempts >= connectionTries) break; continue; }

            var parent = CreateChild(tilesRoot, $"{tileGroupBaseName}{chain.Count}");
            int count = instantiator.InstantiateLayout(candidate, newOrigin, rot, flip, parent);
            var placed = new PlacedTile { layout = candidate, worldOrigin = newOrigin, rotSteps = rot, flipped = flip, parent = parent, aabbMin = nMin, aabbMax = nMax };
            chain.Add(placed); overlap.Add(placed, chain.Count - 1);
            AddTileExitsToPool(chain.Count - 1, candidate.entry);
            Debug.Log($"[GridGenerator] Conectado via EXIT {chosen.label} → Nuevo tile #{chain.Count - 1} (rot={rot * 90}°, flip={flip}). Instancias: {count}");
            return true;
        }
        return false;
    }

    private bool HasAnyValidPlacementForExit(ExitRecord chosen)
    {
        var prevTile = chain.Get(chosen.tileIndex); var layoutPrev = prevTile.layout; var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;
        var nb = FindSinglePathNeighbor(layoutPrev, exitCell); if (!nb.HasValue) return false;
        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));
        Vector3 prevExitWorld = prevTile.worldOrigin + orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        foreach (var (candidate, rot, flip) in candidates.GetValidCandidates(candidateLayouts, dirOut, allowRotations, allowFlip, orient))
        {
            var o = orient.GetOrientedData(candidate, rot, flip);
            var newOrigin = placement.ComputeNewOrigin(candidate, rot, o.entryOriented, dirOut, prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);
            placement.ComputeAABB(newOrigin, o.w, o.h, candidate.cellSize, out var nMin, out var nMax);
            if (!overlap.OverlapsAny(nMin, nMax)) return true;
        }
        return false;
    }

    private void AddTileExitsToPool(int tileIndex, Vector2Int? excludeAssetCell)
    {
        var pt = chain.Get(tileIndex); var exitsList = pt.layout.exits ?? new List<Vector2Int>();
        foreach (var ex in exitsList)
        {
            if (!pt.layout.IsInside(ex) || !pt.layout.IsPath(ex)) continue;
            if (excludeAssetCell.HasValue && ex == excludeAssetCell.Value) continue;
            exits.Add(new ExitRecord { tileIndex = tileIndex, cell = ex, flags = 0, label = string.Empty });
        }
        exits.Relabel();
    }

    private void ClearAll()
    {
        // limpiar hijos (pools si existen)
        if (tilePoolManager) { for (int i = tilesRoot.childCount - 1; i >= 0; i--) Destroy(tilesRoot.GetChild(i).gameObject); }
        else { for (int i = tilesRoot.childCount - 1; i >= 0; i--) DestroyImmediate(tilesRoot.GetChild(i).gameObject); }

        chain.Clear(); exits.Clear(); overlap.Clear(); core.ClearCore();

    }

    private bool CheckPrefabs()
    {
        bool ok = grassPrefab && pathBasicPrefab && pathDamagePrefab && pathSlowPrefab && pathStunPrefab;
        if (!ok) Debug.LogError("[GridGenerator] Asigná todos los prefabs de path y grass.");
        return ok;
    }

    private static Transform EnsureRoot(Transform root, string rootName)
    {
        if (root) return root; var found = GameObject.Find(rootName); if (!found) found = new GameObject(rootName); return found.transform;
    }

    private static Transform CreateChild(Transform root, string name)
    { var go = new GameObject(name); go.transform.SetParent(root, false); return go.transform; }

    private static Vector2Int? FindSinglePathNeighbor(TileLayout layout, Vector2Int cell)
    {
        int count = 0; Vector2Int found = default;
        foreach (var d in TileLayout.OrthoDirs) { var n = cell + d; if (layout.IsPath(n)) { count++; found = n; } }
        return count == 1 ? found : (Vector2Int?)null;
    }

    private static Vector2Int ClampToOrtho(Vector2Int v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2Int(Mathf.Sign(v.x) >= 0 ? 1 : -1, 0);
        if (Mathf.Abs(v.y) > 0) return new Vector2Int(0, Mathf.Sign(v.y) >= 0 ? 1 : -1);
        return Vector2Int.up;
    }

    private static Transform CreateTileParent(Transform root, string name)
    {
        var go = new GameObject(name);
        if (root != null)
            go.transform.SetParent(root, false);
        return go.transform;
    }


    private void ClearRootChildren()
    {
        if (tilesRoot == null) return;

        if (tilePoolManager == null)
        {
            // sin pooling → Destroy
#if UNITY_EDITOR
            for (int i = tilesRoot.childCount - 1; i >= 0; i--)
                DestroyImmediate(tilesRoot.GetChild(i).gameObject);
#else
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            Destroy(tilesRoot.GetChild(i).gameObject);
#endif
            return;
        }

        // con pooling → devolver al pool cuando se pueda
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
        {
            var child = tilesRoot.GetChild(i).gameObject;

            // ¿Grass?
            if (child.CompareTag("Cell"))
            {
                tilePoolManager.ReturnToPool(grassPrefab, child);
                continue;
            }

            // ¿Path? (usa PathCellEffect para saber qué prefab es)
            var eff = child.GetComponent<PathCellEffect>();
            if (eff != null)
            {
                GameObject prefabRef;
                if (eff.stun > 0f) prefabRef = pathStunPrefab;
                else if (eff.slow > 0f) prefabRef = pathSlowPrefab;
                else if (eff.damagePerHit > 0) prefabRef = pathDamagePrefab;
                else prefabRef = pathBasicPrefab;

                tilePoolManager.ReturnToPool(prefabRef, child);
                continue;
            }

            // Fallback
#if UNITY_EDITOR
            DestroyImmediate(child);
#else
        Destroy(child);
#endif
        }
    }

    public TileLayout CurrentLayout
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).layout;
            return initialLayout;
        }
    }

    public List<TileLayout> GetRandomCandidateSet(int count)
    {
        // Copia solo válidos
        var pool = new List<TileLayout>();
        foreach (var c in candidateLayouts)
            if (c != null) pool.Add(c);

        // Fisher–Yates usando el rng ya declarado en tu GridGenerator
        for (int i = 0; i < pool.Count; i++)
        {
            int j = rng.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        if (pool.Count > count)
            pool.RemoveRange(count, pool.Count - count);

        return pool;
    }

    public Vector3 CurrentWorldOrigin
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).worldOrigin;
            return Vector3.zero;
        }
    }

    public int CurrentRotSteps
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).rotSteps;
            return 0;
        }
    }

    public bool CurrentFlipped
    {
        get
        {
            if (chain != null && chain.Count > 0)
                return chain.Get(chain.Count - 1).flipped;
            return false;
        }
    }


    public void UI_CleanupCaches()
    {
        exits.Clear();
        chain.Clear();
        overlap.Clear();
        core?.ClearCore();
        Debug.Log("[GridGenerator] Caches limpiados.");
    }

    // ──────────────────────────────────────────────────────────────
    // Compatibilidad con SpawnManager (API vieja del GridGenerator)
    // ──────────────────────────────────────────────────────────────

    // Devuelve todos los spawn points abiertos (exits no usados)
    public List<Vector3> GetSpawnPoints()
    {
        return spawns != null ? spawns.GetSpawnPoints() : new List<Vector3>();
    }

    // Round-robin entre los spawn points abiertos
    public Vector3 GetNextSpawnRoundRobin()
    {
        return spawns != null ? spawns.GetNextRoundRobin() : Vector3.zero;
    }

    // Ruta Exit -> Core usando el BFS interno
    public bool TryGetRouteExitToCore(Vector3 exitWorldPos, out List<Vector3> route)
    {
        route = null;
        return runners != null && runners.TryGetRouteExitToCore(exitWorldPos, out route);
    }

    // ¿Existe un core ya spawneado?
    public bool HasCore()
    {
        return core != null && core.HasCore;
    }

    // Posición actual del core (o Vector3.zero si no hay)
    public Vector3 GetCorePosition()
    {
        return core != null ? core.Position : Vector3.zero;
    }

    // Offset Y que usan los runners (para spawnear/routear flotando un poquito)
    public float GetRunnerYOffset()
    {
        return runnerYOffset;
    }


    public bool AppendNextUsingSelectedExitWithLayout(TileLayout forced)
    {
        if (forced == null) return false;

        int global = exits.GlobalIndexFromAvailable(selectedExitIndex);
        if (global < 0 || exits.Count == 0) return false;

        var chosen = exits.Get(global);
        if (chosen.Used || chosen.Closed) return false;

        // Tile previo y celda de salida
        var prevTile = chain.Get(chosen.tileIndex);
        var layoutPrev = prevTile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        // Dirección de salida en mundo (cardinal)
        Vector2Int dirOut = ClampToOrtho(orient.ApplyToDir(exitCell - nb.Value, prevTile.rotSteps, prevTile.flipped));

        // Posición mundo del exit previo
        Vector3 prevExitWorld = prevTile.worldOrigin +
                                orient.CellToWorldLocal(exitCell, layoutPrev, prevTile.rotSteps, prevTile.flipped);

        int rotMax = allowRotations ? 4 : 1;
        int flipMax = allowFlip ? 2 : 1;

        for (int rot = 0; rot < rotMax; rot++)
        {
            for (int f = 0; f < flipMax; f++)
            {
                bool flip = (allowFlip && f == 1);

                // Datos orientados del layout forzado
                var o = orient.GetOrientedData(forced, rot, flip);

                // Chequeo de borde válido para la 'entry' orientada
                bool okBorde = false;
                if (dirOut == Vector2Int.right) okBorde = (o.entryOriented.x == 0);
                else if (dirOut == Vector2Int.left) okBorde = (o.entryOriented.x == o.w - 1);
                else if (dirOut == Vector2Int.up) okBorde = (o.entryOriented.y == (worldPlusZUsesYZeroEdge ? 0 : o.h - 1));
                else okBorde = (o.entryOriented.y == (worldPlusZUsesYZeroEdge ? o.h - 1 : 0));

                if (!okBorde) continue;

                // Nuevo origen alineando 'entry' con el exit anterior + gap
                var newOrigin = placement.ComputeNewOrigin(
                                    forced, rot, o.entryOriented, dirOut,
                                    prevTile.aabbMin, prevTile.aabbMax, prevExitWorld);

                // AABB y chequeo de solape
                placement.ComputeAABB(newOrigin, o.w, o.h, forced.cellSize, out var nMin, out var nMax);
                if (overlap.OverlapsAny(nMin, nMax)) continue;

                // Instanciar y registrar
                var parent = CreateTileParent(tilesRoot, $"{tileGroupBaseName}{chain.Count}");
                int count = instantiator.InstantiateLayout(forced, newOrigin, rot, flip, parent);

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

                chain.Add(placed);
                overlap.Add(placed, chain.Count - 1);

                // Registrar exits del nuevo tile (excluyendo la entry)
                AddTileExitsToPool(chain.Count - 1, forced.entry);

                // Marcar el exit elegido como usado y relabel
                exits.MarkUsed(global);
                exits.Relabel();

                Debug.Log($"[GridGenerator] Conectado con layout forzado {forced.name} (rot={rot * 90}°, flip={flip}). Instancias: {count}");
                return true;
            }
        }

        // Si ninguna orientación es válida, cerramos el exit si no hay forma
        if (!HasAnyValidPlacementForExit(chosen))
        {
            exits.MarkClosed(global);
            exits.Relabel();
            selectedExitIndex = 0;
        }

        return false;
    }

}
