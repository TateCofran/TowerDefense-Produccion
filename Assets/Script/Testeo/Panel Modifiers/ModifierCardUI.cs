using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Si usás TextMeshPro

public class ModifierCardUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text currentModifierText;
    public Button selectButton;

    private IGameModifier modifier;

    public void Setup(IGameModifier modifier, Action onSelect, int currentStacks)
    {
        this.modifier = modifier;
        nameText.text = modifier.Name;
        descriptionText.text = modifier.Description; 
        currentModifierText.text = currentStacks > 0 ? $"+{modifier.GetStackDescription(currentStacks)}" : "";
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onSelect?.Invoke());
    }
}
