using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileExpansion", menuName = "TD/Tile Expansion")]
public class TileExpansionSO : ScriptableObject
{
    public string tileName;
    public Vector2Int tileSize = new Vector2Int(5, 5);

    [Header("Posiciones de Camino dentro del tile (relative al 5x5)")]
    public List<Vector2Int> pathPositions = new List<Vector2Int>();
}
