using UnityEngine;
using UnityEngine.UI;

public class TilePreviewButton : MonoBehaviour
{
    private UIManager ui;
    private TileExpansion tile;

    public void Setup(UIManager manager, TileExpansion tileData)
    {
        ui = manager;
        tile = tileData;
        GetComponent<Button>().onClick.AddListener(ApplyTile);
    }

    private void ApplyTile()
    {
        Vector2Int lastPath = GridManager.Instance.GetLastPathGridPosition();

        // NO moverlo hacia adelante
        // El tile debe empezar exactamente donde terminó el camino anterior
        Vector3 spawnWorld = new Vector3(
            lastPath.x * GridManager.Instance.cellSize,
            0f,
            lastPath.y * GridManager.Instance.cellSize
        );

        GridManager.Instance.ApplyTileExpansionAtWorldPosition(tile, spawnWorld);
        //ui.ClearPreview();
        Destroy(gameObject);

        // Iniciar oleada 1 luego de seleccionar el primer tile
        if (WaveManager.Instance != null && WaveManager.Instance.GetCurrentWave() == 0)
        {
            WaveManager.Instance?.TryStartFirstWave();
        }
        if (WaveManager.Instance != null && WaveManager.Instance.GetCurrentWave() > 0 && !WaveManager.Instance.WaveInProgress)
        {
            WaveManager.Instance.StartNextWave();
        }

        //Debug.Log("Tile aplicado");
    }


    private Vector2Int GetSpawnBasePosition(TileExpansion tile)
    {
        Vector2Int lastGrid = GridManager.Instance.GetLastPathGridPosition();
        Vector2Int dir = GridManager.Instance.GetPathDirection();

        Vector2Int offset = dir * (tile.tileSize.y / 2); // mueve hacia adelante
        return lastGrid + offset;
    }


}
