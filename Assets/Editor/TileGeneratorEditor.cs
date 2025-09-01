#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TileTestGenerator : EditorWindow
{
    public GameObject cellPrefab;
    public GameObject pathPrefab;
    public GameObject corePrefab;
    public int tileGridSize = 5;
    public float cellSize = 1f;
    public float tileSpacing = 1.5f; // espacio entre tiles

    [MenuItem("Herramientas/Generar Todas las Variantes de Tile (GridManager logic)")]
    public static void ShowWindow()
    {
        GetWindow<TileTestGenerator>("Tile Variants Generator");
    }

    void OnGUI()
    {
        SerializedObject so = new SerializedObject(this);
        so.Update();

        EditorGUILayout.PropertyField(so.FindProperty("cellPrefab"));
        EditorGUILayout.PropertyField(so.FindProperty("pathPrefab"));
        EditorGUILayout.PropertyField(so.FindProperty("corePrefab"));
        tileGridSize = EditorGUILayout.IntField("Tile Grid Size", tileGridSize);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        tileSpacing = EditorGUILayout.FloatField("Tile Spacing", tileSpacing);

        if (GUILayout.Button("Generar todas las variantes de tile"))
        {
            GenerateAllTiles();
        }

        so.ApplyModifiedProperties();
    }

    void GenerateAllTiles()
    {
        // Limpia previa generación
        var prev = GameObject.Find("TileVariantsPreviewRoot");
        if (prev) DestroyImmediate(prev);

        GameObject root = new GameObject("TileVariantsPreviewRoot");

        // Define todas las variantes igual que en tu GridManager
        List<TileExpansion> tileVariants = new()
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

        // Instanciar cada variante en la escena (en fila)
        for (int t = 0; t < tileVariants.Count; t++)
        {
            var tile = tileVariants[t];
            Vector3 tileOrigin = new Vector3((tileGridSize + tileSpacing) * t, 0, 0);

            GameObject tileRoot = new GameObject($"Tile_{tile.tileName}");
            tileRoot.transform.parent = root.transform;
            tileRoot.transform.position = tileOrigin;

            // Instanciar celdas normales (5x5)
            for (int x = 0; x < tileGridSize; x++)
            {
                for (int y = 0; y < tileGridSize; y++)
                {
                    Vector3 worldPos = tileOrigin + new Vector3(x * cellSize, 0, y * cellSize);
                    var cell = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab, tileRoot.transform);
                    cell.transform.position = worldPos;
                }
            }

            // Instanciar core en (centro abajo)
            Vector2Int corePos = new Vector2Int(tileGridSize / 2, 0);
            Vector3 coreWorldPos = tileOrigin + new Vector3(corePos.x * cellSize, 0, corePos.y * cellSize);
            var core = (GameObject)PrefabUtility.InstantiatePrefab(corePrefab, tileRoot.transform);
            core.transform.position = coreWorldPos;

            // Instanciar camino
            foreach (var offset in tile.pathOffsets)
            {
                Vector2Int pathGrid = corePos + offset;
                Vector3 pathWorldPos = tileOrigin + new Vector3(pathGrid.x * cellSize, 0, pathGrid.y * cellSize);

                // Buscar y destruir celda normal si está en ese lugar (para no solapar)
                foreach (Transform child in tileRoot.transform)
                {
                    if (Vector3.Distance(child.position, pathWorldPos) < 0.01f && child.gameObject != core)
                    {
                        DestroyImmediate(child.gameObject);
                        break;
                    }
                }

                var path = (GameObject)PrefabUtility.InstantiatePrefab(pathPrefab, tileRoot.transform);
                path.transform.position = pathWorldPos;
            }
        }

        Debug.Log("¡Todas las variantes de tile generadas!");
    }
}

#endif