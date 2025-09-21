using UnityEngine;
using System;

[Serializable]
public struct PlacementPreview
{
    public Vector3 origin;
    public Vector2 sizeXZ;
    public bool valid;
    public GridGenerator.PreviewStatus status;
    public string note;
    public float cellSize;
}

[Serializable]
public struct PlacedTile
{
    public TileLayout layout;
    public Vector3 worldOrigin;
    public int rotSteps;
    public bool flipped;
    public Transform parent;
    public Vector3 aabbMin;
    public Vector3 aabbMax;
}

[Serializable]
public struct ExitRecord
{
    public int tileIndex;
    public Vector2Int cell;
    public byte flags; // bit 0: used, bit 1: closed
    public string label;

    public bool Used => (flags & 1) != 0;
    public bool Closed => (flags & 2) != 0;

    public void SetUsed(bool value) => flags = (byte)(value ? (flags | 1) : (flags & ~1));
    public void SetClosed(bool value) => flags = (byte)(value ? (flags | 2) : (flags & ~2));
}