using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
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

    public struct PlacementPreview
    {
        public Vector3 origin;
        public Vector2 sizeXZ;
        public bool valid;
        public PreviewStatus status;
        public string note;
        public float cellSize;
    }
    private struct PlacedTile
    {
        public TileLayout layout;    
        public Vector3 worldOrigin;  
        public int rotSteps;          
        public bool flipped;
        public Transform parent;
        public Vector3 aabbMin;      
        public Vector3 aabbMax;       
    }
    private readonly List<PlacedTile> _chain = new List<PlacedTile>();

    private class ExitRecord
    {
        public int tileIndex;
        public Vector2Int cell;
        public bool used;
        public bool closed;     // <-- NUEVO
        public string label;
    }
    private readonly List<ExitRecord> _exits = new List<ExitRecord>();
    [SerializeField] private int selectedExitIndex = 0;

    private System.Random _rng = new System.Random();

    public TileLayout CurrentLayout => _chain.Count > 0 ? _chain[_chain.Count - 1].layout : initialLayout;
    public Vector3 CurrentWorldOrigin => _chain.Count > 0 ? _chain[_chain.Count - 1].worldOrigin : Vector3.zero;
    public int CurrentRotSteps => _chain.Count > 0 ? _chain[_chain.Count - 1].rotSteps : 0;
    public bool CurrentFlipped => _chain.Count > 0 && _chain[_chain.Count - 1].flipped;

    // En GetAvailableExits() y GetAvailableExitLabels() filtrá también closed:
    public List<(string label, Vector3 worldPos)> GetAvailableExits()
    {
        var list = new List<(string, Vector3)>();
        for (int i = 0; i < _exits.Count; i++)
        {
            var ex = _exits[i];
            if (ex.used || ex.closed) continue;   // <-- filtra cerrados
            var pt = _chain[ex.tileIndex];
            Vector3 wpos = pt.worldOrigin + CellToWorldLocal(ex.cell, pt.layout, pt.rotSteps, pt.flipped);
            list.Add((ex.label, wpos));
        }
        return list;
    }

    public List<string> GetAvailableExitLabels()
    {
        var labels = new List<string>();
        foreach (var ex in _exits)
            if (!ex.used && !ex.closed)           // <-- filtra cerrados
                labels.Add(ex.label);
        return labels;
    }

    public void UI_SetExitIndex(int index) => selectedExitIndex = Mathf.Max(0, index);
    public void UI_SetExitByLabel(string label)
    {
        int idx = GetAvailableExitIndexByLabel(label);
        if (idx >= 0) selectedExitIndex = idx;
    }

    public void UI_GenerateFirst() => GenerateFirst();
    public void UI_AppendNext() => AppendNextUsingSelectedExit();
    public void UI_SetTileGap(float g) => tileGap = g;
    public void UI_SetExtraOffsetX(float x) => extraTileOffset.x = x;
    public void UI_SetExtraOffsetY(float y) => extraTileOffset.y = y;
    public void UI_SetExtraOffsetZ(float z) => extraTileOffset.z = z;

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
            _chain.Clear();
            _exits.Clear();
            selectedExitIndex = 0;
        }

        int rot = 0; bool flip = false;
        Vector3 worldOrigin = initialLayout.origin;

        var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chain.Count);
        int count = InstantiateLayout(initialLayout, worldOrigin, rot, flip, parent);

        OrientedSize(initialLayout, rot, out int w, out int h);
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
        _chain.Add(PushCopy(placed)); 

        AddTileExitsToPool(_chain.Count - 1, excludeAssetCell: null);

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

        var chosen = _exits[globalIdx];
        if (chosen.used)
        {
            Debug.LogWarning("[GridGenerator] El EXIT elegido ya está usado.");
            return;
        }

        int tileIdx = chosen.tileIndex;
        var tile = _chain[tileIdx];
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell))
        {
            Debug.LogError("[GridGenerator] EXIT elegido no válido.");
            return;
        }

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue)
        {
            Debug.LogError("[GridGenerator] EXIT sin dirección (no grado 1).");
            return;
        }

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;

        Vector3 prevExitWorld = tile.worldOrigin + CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

        for (int attempt = 0; attempt < Mathf.Max(1, connectionTries); attempt++)
        {
            TileLayout candidate = ChooseRandomCandidate();
            if (candidate == null)
            {
                Debug.LogError("[GridGenerator] No hay candidateLayouts.");
                return;
            }

            int rot = allowRotations ? _rng.Next(0, 4) : 0;
            bool flip = allowFlip ? (_rng.NextDouble() < 0.5) : false;

            Vector2Int entry = candidate.entry;
            if (!candidate.IsInside(entry) || !candidate.IsPath(entry))
                continue;

            OrientedSize(candidate, rot, out int newW, out int newH);
            float newWidth = newW * candidate.cellSize;
            float newHeight = newH * candidate.cellSize;

            Vector2Int entryOriented = ApplyOrientationToCell(entry, candidate, rot, flip);

            bool okBorde = false;
            if (dirOut == Vector2Int.right) okBorde = (entryOriented.x == 0);
            if (dirOut == Vector2Int.left) okBorde = (entryOriented.x == newW - 1);
            if (dirOut == Vector2Int.up) okBorde = (entryOriented.y == 0);
            if (dirOut == Vector2Int.down) okBorde = (entryOriented.y == newH - 1);
            if (!okBorde) continue;

            Vector3 newOrigin = ComputeNewOrigin(
                candidate, rot, entryOriented, dirOut,
                prevMin, prevMax, prevExitWorld);

            Vector3 nMin = newOrigin;
            Vector3 nMax = newOrigin + new Vector3(newWidth, 0f, newHeight);


            if (OverlapsAny(nMin, nMax))
                continue;

            var parent = CreateTileParent(tilesRoot, tileGroupBaseName + _chain.Count);
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
            _chain.Add(PushCopy(placed));

            chosen.used = true;
            RelabelAvailableExits();

            AddTileExitsToPool(_chain.Count - 1, excludeAssetCell: candidate.entry);

            Debug.Log($"[GridGenerator] Conectado via EXIT {chosen.label} (tile #{tileIdx}) → Nuevo tile #{_chain.Count - 1} (rot={rot * 90}°, flip={flip}). Instancias: {count}");
            return;
        }

        // Al final de AppendNextUsingSelectedExit(), justo antes del Debug.LogWarning(...)
        Debug.LogWarning("[GridGenerator] No se encontró orientación válida (o todas solapan).");

        if (!HasAnyValidPlacementForExit(chosen))
        {
            chosen.closed = true;
            chosen.label = "EXIT CERRADO";
            RelabelAvailableExits();
            selectedExitIndex = 0;
        }
    }

    public List<PlacementPreview> GetPlacementPreviews()
    {
        var previews = new List<PlacementPreview>();

        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exits.Count == 0) return previews;

        var chosen = _exits[globalIdx];
        if (chosen.used) return previews;

        var tile = _chain[chosen.tileIndex];
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;
        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return previews;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return previews;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

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

                    OrientedSize(candidate, rot, out int w, out int h);
                    float width = w * candidate.cellSize;
                    float height = h * candidate.cellSize;

                    Vector2Int entryOriented = ApplyOrientationToCell(entry, candidate, rot, flip);

                    bool okBorde = false;
                    if (dirOut == Vector2Int.right) okBorde = (entryOriented.x == 0);
                    if (dirOut == Vector2Int.left) okBorde = (entryOriented.x == w - 1);
                    if (dirOut == Vector2Int.up) okBorde = (entryOriented.y == 0);
                    if (dirOut == Vector2Int.down) okBorde = (entryOriented.y == h - 1);

                    Vector3 newOrigin = ComputeNewOrigin(candidate, rot, entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
                    Vector3 nMin = newOrigin;
                    Vector3 nMax = newOrigin + new Vector3(width, 0f, height);
                    bool noOverlap = !OverlapsAny(nMin, nMax);

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
        pos = Vector3.zero; dirOutWorld = Vector2Int.zero;

        int globalIdx = GetGlobalExitIndexFromAvailable(selectedExitIndex);
        if (globalIdx < 0 || _exits.Count == 0) return false;

        var chosen = _exits[globalIdx];
        if (chosen.used) return false;

        var tile = _chain[chosen.tileIndex];
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int d = (exitCell - nb.Value);
        d = ApplyInverseOrientationToDir(d, tile.rotSteps, tile.flipped);
        d = ClampToOrtho(d);

        pos = tile.worldOrigin + CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);
        dirOutWorld = d;
        return true;
    }

    // ======= Gestión de EXITS =======
    private void AddTileExitsToPool(int tileIndex, Vector2Int? excludeAssetCell)
    {
        var pt = _chain[tileIndex];
        var exits = pt.layout.exits ?? new List<Vector2Int>();
        foreach (var ex in exits)
        {
            if (!pt.layout.IsInside(ex) || !pt.layout.IsPath(ex))
                continue;
            if (excludeAssetCell.HasValue && ex == excludeAssetCell.Value)
                continue;

            _exits.Add(new ExitRecord
            {
                tileIndex = tileIndex,
                cell = ex,
                used = false,
                label = ""
            });
        }
        RelabelAvailableExits();
    }

    private void RelabelAvailableExits()
    {
        int n = 0;
        foreach (var ex in _exits)
        {
            if (ex.used || ex.closed) continue;   // <-- skip cerrados
            ex.label = IndexToLetters(n++);
        }
    }

    private int GetGlobalExitIndexFromAvailable(int availableIdx)
    {
        int count = 0;
        for (int i = 0; i < _exits.Count; i++)
        {
            if (_exits[i].used || _exits[i].closed) continue; // 👈
            if (count == availableIdx) return i;
            count++;
        }
        return -1;
    }

    private int GetAvailableExitIndexByLabel(string label)
    {
        int count = 0;
        foreach (var ex in _exits)
        {
            if (ex.used || ex.closed) continue; // 👈
            if (ex.label == label) return count;
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

    // ======= Helpers de AABB / Colisiones / Cálculo de origin =======
    private bool OverlapsAny(Vector3 nMin, Vector3 nMax)
    {
        for (int i = 0; i < _chain.Count; i++)
        {
            var t = _chain[i];
            if (AabbOverlap(nMin, nMax, t.aabbMin, t.aabbMax))
                return true;
        }
        return false;
    }

    private static bool AabbOverlap(Vector3 aMin, Vector3 aMax, Vector3 bMin, Vector3 bMax)
    {
        // Separating Axis (solo X/Z)
        if (aMax.x <= bMin.x || aMin.x >= bMax.x) return false;
        if (aMax.z <= bMin.z || aMin.z >= bMax.z) return false;
        return true;
    }

    private Vector3 ComputeNewOrigin(
        TileLayout candidate,
        int rotSteps,
        Vector2Int entryOriented,
        Vector2Int dirOut,
        Vector3 prevMin, Vector3 prevMax,
        Vector3 prevExitWorld)
    {
        OrientedSize(candidate, rotSteps, out int w, out int h);
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

    // ======= Helpers de conexión e instanciación =======
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

    public static void OrientedSize(TileLayout layout, int rotSteps, out int w, out int h)
    {
        bool swap = (rotSteps % 2) != 0;
        w = swap ? layout.gridHeight : layout.gridWidth;
        h = swap ? layout.gridWidth : layout.gridHeight;
    }

    private static Vector2Int ApplyInverseOrientationToDir(Vector2Int d, int rotSteps, bool flip)
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

    private static Vector2Int ApplyOrientationToCell(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
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

    private static Vector3 CellToWorldLocal(Vector2Int c, TileLayout layout, int rotSteps, bool flip)
    {
        Vector2Int oc = ApplyOrientationToCell(c, layout, rotSteps, flip);
        return new Vector3(oc.x * layout.cellSize, 0f, oc.y * layout.cellSize);
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

                Vector3 basePos = worldOrigin + CellToWorldLocal(cell, layout, rotSteps, flip);
                Vector3 offset = (type == TileLayout.TileType.Path) ? pathCellOffset : grassCellOffset;

                var go = Instantiate(prefab, basePos + offset, Quaternion.Euler(0f, rotSteps * 90f, 0f), parent);
                go.name = $"{prefab.name}_({x},{y})";
                count++;
            }
        }
        return count;
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
#if UNITY_EDITOR
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(tilesRoot.GetChild(i).gameObject);
#else
        for (int i = tilesRoot.childCount - 1; i >= 0; i--)
            Destroy(tilesRoot.GetChild(i).gameObject);
#endif
    }

    private Transform CreateTileParent(Transform root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
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

    private TileLayout ChooseRandomCandidate()
    {
        if (candidateLayouts == null || candidateLayouts.Count == 0) return null;
        return candidateLayouts[_rng.Next(candidateLayouts.Count)];
    }

    private PlacedTile PushCopy(PlacedTile p)
    {
        return p; 
    }
    // Devuelve true si existe AL MENOS un placement válido para este EXIT (no solapa y borde OK)
    private bool HasAnyValidPlacementForExit(ExitRecord chosen)
    {
        var tile = _chain[chosen.tileIndex];
        var layoutPrev = tile.layout;
        var exitCell = chosen.cell;

        if (!layoutPrev.IsInside(exitCell) || !layoutPrev.IsPath(exitCell)) return false;

        var nb = FindSinglePathNeighbor(layoutPrev, exitCell);
        if (!nb.HasValue) return false;

        Vector2Int dirOut = (exitCell - nb.Value);
        dirOut = ApplyInverseOrientationToDir(dirOut, tile.rotSteps, tile.flipped);
        dirOut = ClampToOrtho(dirOut);

        Vector3 prevMin = tile.aabbMin;
        Vector3 prevMax = tile.aabbMax;
        Vector3 prevExitWorld = tile.worldOrigin + CellToWorldLocal(exitCell, layoutPrev, tile.rotSteps, tile.flipped);

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

                    OrientedSize(candidate, rot, out int w, out int h);
                    float width = w * candidate.cellSize;
                    float height = h * candidate.cellSize;

                    Vector2Int entryOriented = ApplyOrientationToCell(entry, candidate, rot, flip);

                    bool okBorde = false;
                    if (dirOut == Vector2Int.right) okBorde = (entryOriented.x == 0);
                    if (dirOut == Vector2Int.left) okBorde = (entryOriented.x == w - 1);
                    if (dirOut == Vector2Int.up) okBorde = (entryOriented.y == 0);
                    if (dirOut == Vector2Int.down) okBorde = (entryOriented.y == h - 1);
                    if (!okBorde) continue;

                    Vector3 newOrigin = ComputeNewOrigin(candidate, rot, entryOriented, dirOut, prevMin, prevMax, prevExitWorld);
                    Vector3 nMin = newOrigin;
                    Vector3 nMax = newOrigin + new Vector3(width, 0f, height);

                    if (!OverlapsAny(nMin, nMax))
                        return true; // encontramos al menos una colocación válida
                }
            }
        }
        return false;
    }

}

