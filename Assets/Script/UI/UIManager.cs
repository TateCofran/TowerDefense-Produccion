using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Wave UI")]
    private WaveUIController waveUIController;


    [Header("Tile Selection Panel")]
    public GameObject tileSelectionPanel;
    public Button[] tileButtons;

    private GameObject previewContainer;

    public TileExpansion selectedTile = null;
    //public GameObject tileVisualPrefab;
    private GameObject currentSelectableTile;

    public GameObject tilePreviewButtonPrefab;
    public Transform tilePreviewButtonParent; // donde aparecerá el botón
    private GameObject currentTileButton;
    private TileExpansion currentTileData;

    //public Vector3 previewWorldPosition = new Vector3(100, 0, 0);

    private TileExpansion _currentTileData;
    public TileExpansion CurrentTileData => _currentTileData;

    private List<string> tilesChosen = new List<string>();

    private void Awake()
    {
        Instance = this;
        tileSelectionPanel.SetActive(false);
    }
    void Start()
    {
        GameModifiersManager.Instance.InjectModifierPanelSelection(ModifierPanelSelection.Instance);
    }
    public void ShowTileSelection(List<TileExpansion> tiles)
    {
        tileSelectionPanel.SetActive(true);

        for (int i = 0; i < tileButtons.Length; i++)
        {
            if (i < tiles.Count)
            {
                tileButtons[i].gameObject.SetActive(true);
                tileButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = tiles[i].tileName;

                int index = i;
                tileButtons[i].onClick.RemoveAllListeners();
                tileButtons[i].onClick.AddListener(() => ToggleTileSelection(tiles[index]));
            }
            else
            {
                tileButtons[i].gameObject.SetActive(false);
            }
        }
        UpdateTileButtonStates(tiles);

    }

    private void ToggleTileSelection(TileExpansion tile)
    {
        // Deseleccionar si ya estaba seleccionado
        if (selectedTile == tile)
        {
            ClearPreview();
            selectedTile = null;
            return;
        }

        // Seleccionar nuevo tile
        selectedTile = tile;

        // En su lugar: generamos el botón que representa ese tile
        HandleTileSelection(tile);

        tileSelectionPanel.SetActive(false);


    }


    public void ClearPreview()
    {
        if (previewContainer != null)
        {
            Destroy(previewContainer);
            previewContainer = null;
        }
    }

    public void HandleTileSelection(TileExpansion tile)
    {
        currentTileData = tile;

        // Guarda el tile seleccionado
        tilesChosen.Add(tile.tileName);

        // Destruir botón anterior si existe
        if (currentTileButton != null)
        {
            Destroy(currentTileButton);
        }

        // Instanciar nuevo botón en el panel asignado
        currentTileButton = Instantiate(tilePreviewButtonPrefab, tilePreviewButtonParent);
        if (currentTileButton != null)
        {

            var textComponent = currentTileButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = tile.tileName;
            }
            var script = currentTileButton.GetComponent<TilePreviewButton>();
            if (script != null)
            {
                script.Setup(this, tile);
            }

        }


    }

    /*private Vector2Int GetPreviewBaseGrid()
    {
        float size = GridManager.Instance.cellSize;
        return new Vector2Int(
            Mathf.RoundToInt(previewWorldPosition.x / size),
            Mathf.RoundToInt(previewWorldPosition.z / size)
        );
    }*/

    private Vector3 GetWorldFromGrid(Vector2Int gridPos)
    {
        float size = GridManager.Instance.cellSize;
        return new Vector3(gridPos.x * size, 0.01f, gridPos.y * size);
    }
    public void UpdateTileButtonStates(List<TileExpansion> tiles)
    {
        List<string> disabledTiles = GridManager.Instance.GetDisabledTileNamesFromNextAdyacents();

        for (int i = 0; i < tileButtons.Length; i++)
        {
            if (i < tiles.Count)
            {
                string tileName = tiles[i].tileName;
                tileButtons[i].interactable = !disabledTiles.Contains(tileName);
            }
        }
    }

}