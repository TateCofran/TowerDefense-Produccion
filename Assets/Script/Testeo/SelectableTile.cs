using UnityEngine;

public class SelectableTile : MonoBehaviour
{
    private TileExpansion tileData;
    private bool isSelected = false;

    public void Initialize(TileExpansion tile)
    {
        tileData = tile;
    }

    private void OnMouseDown()
    {
        if (!isSelected)
        {
            //UIManager.Instance.ShowTilePreview(tileData);
            isSelected = true;
        }
        else
        {
            //UIManager.Instance.ClearPreview();
            isSelected = false;
        }
    }
}