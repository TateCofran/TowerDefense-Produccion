using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CorruptionUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Image corruptionFillImage;
    [SerializeField] private TMP_Text corruptionLevelText;
    [SerializeField] private TMP_Text penaltyDescriptionText;

    void Start()
    {
        if (CorruptionManager.Instance != null)
        {
            CorruptionManager.Instance.OnCorruptionChanged += UpdateFillAmount;
            CorruptionManager.Instance.OnCorruptionLevelChanged += UpdateLevelText;

            // Inicializar valores
            UpdateFillAmount(CorruptionManager.Instance.GetCorruptionPercent());
            UpdateLevelText(CorruptionManager.Instance.CurrentLevel);
            penaltyDescriptionText.text = CorruptionManager.Instance.GetPenaltyDescription();
        }
    }

    private void OnDestroy()
    {
        if (CorruptionManager.Instance != null)
        {
            CorruptionManager.Instance.OnCorruptionChanged -= UpdateFillAmount;
            CorruptionManager.Instance.OnCorruptionLevelChanged -= UpdateLevelText;
        }
    }

    private void UpdateFillAmount(float totalPercent)
    {
        if (corruptionFillImage == null) return;

        // Dividimos la barra en 3 segmentos visuales (una vuelta completa en cada nivel)
        float visualFill;

        if (totalPercent < 1f / 3f)
        {
            visualFill = totalPercent * 3f;
        }
        else if (totalPercent < 2f / 3f)
        {
            visualFill = (totalPercent - 1f / 3f) * 3f;
        }
        else
        {
            visualFill = (totalPercent - 2f / 3f) * 3f;
        }

        corruptionFillImage.fillAmount = Mathf.Clamp01(visualFill);
    }


    private void UpdateLevelText(CorruptionManager.CorruptionLevel level)
    {
        if (corruptionLevelText == null) return;

        int levelNumber = level switch
        {
            CorruptionManager.CorruptionLevel.None => 0,
            CorruptionManager.CorruptionLevel.Level1 => 1,
            CorruptionManager.CorruptionLevel.Level2 => 2,
            CorruptionManager.CorruptionLevel.Level3 => 3,
            _ => 0
        };

        corruptionLevelText.text = levelNumber.ToString();

        //actualizar color de la barra
        UpdateLevelVisuals(level);

        penaltyDescriptionText.text = CorruptionManager.Instance.GetPenaltyDescription();
    }

    private void UpdateLevelVisuals(CorruptionManager.CorruptionLevel level)
    {
        switch (level)
        {
            case CorruptionManager.CorruptionLevel.None:
                corruptionFillImage.color = Color.white;
                break;
            case CorruptionManager.CorruptionLevel.Level1:
                corruptionFillImage.color = Color.yellow;
                break;
            case CorruptionManager.CorruptionLevel.Level2:
                corruptionFillImage.color = new Color(1f, 0.5f, 0f); // naranja
                break;
            case CorruptionManager.CorruptionLevel.Level3:
                corruptionFillImage.color = Color.red;
                break;
        }
    }

}
