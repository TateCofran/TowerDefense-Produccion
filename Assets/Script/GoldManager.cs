using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    public int currentGold = 30;
    public TMP_Text goldText;

    void Awake()
    {
        Instance = this;
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        CoreUI.Instance?.UpdateUI(); // actualizar estado del botón
        GoldChangeTextSpawner.Instance.ShowGoldChange(amount);
        UpdateGoldUI();

        // Al final de SpendGold o AddGold o SetGold, después de actualizar el valor:
        if (CellInteraction.hoveredCell != null)
            CellInteraction.hoveredCell.RefreshPreview();

    }

    public void SpendGold(int amount)
    {
        currentGold -= amount;
        CoreUI.Instance?.UpdateUI(); // actualizar estado del botón
        GoldChangeTextSpawner.Instance.ShowGoldChange(-amount);
        UpdateGoldUI();

        // Al final de SpendGold o AddGold o SetGold, después de actualizar el valor:
        if (CellInteraction.hoveredCell != null)
            CellInteraction.hoveredCell.RefreshPreview();

    }

    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    public void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Oro: {currentGold}";

    }

}
