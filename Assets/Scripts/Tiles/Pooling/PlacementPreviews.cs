using UnityEngine;

public enum PreviewStatus
{
    Valid,
    Overlap,
    Invalid
}

public struct PlacementPreview
{
    public Vector3 origin;          // posici�n mundial donde se ubicar�a el tile
    public Vector2 sizeXZ;          // tama�o visual del tile (w x h)
    public bool valid;              // true si no se superpone con otro tile
    public PreviewStatus status;    // estado (v�lido, solapado, inv�lido)
    public string note;             // texto descriptivo (ej: nombre, rotaci�n)
    public float cellSize;          // tama�o de celda usado (solo informativo)
}
