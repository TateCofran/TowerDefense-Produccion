using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpgradeUIItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    public Image iconImage; // la imagen principal
    public GameObject tooltipPanel;
    public TMP_Text tooltipNameText;
    public TMP_Text tooltipDescriptionText;
    public TMP_Text tooltipCostText;
    public Image circleSlider; // tipo radial (Image con fill radial)

    [Header("Config")]
    public float unlockHoldTime = 1.5f;

    [Header("Tooltip Offset")]
    //public Vector2 tooltipOffset = new Vector2(240f, 0f); // 120 px a la derecha, ajustá a gusto

    private float holdTimer = 0f;
    private bool isHolding = false;
    private bool canUnlock = true;

    private UpgradeData currentUpgrade;
    private System.Action<string> unlockCallback;

    public void Setup(UpgradeData upgrade, bool unlocked, System.Action<string> onUnlock)
    {
        currentUpgrade = upgrade;
        unlockCallback = onUnlock;

        // Actualizá el icono (si tenés un campo Sprite)
        iconImage.sprite = upgrade.icon;

        canUnlock = !unlocked;
        // Visual feedback:
        iconImage.color = unlocked ? new Color(1, 1, 1, 0.4f) : Color.white;

        // Ocultá tooltip y reseteá círculo de progreso
        tooltipPanel.SetActive(false);
        circleSlider.fillAmount = 0f;
    }

    void Update()
    {
        if (isHolding && canUnlock)
        {
            holdTimer += Time.deltaTime;
            circleSlider.fillAmount = Mathf.Clamp01(holdTimer / unlockHoldTime);

            if (holdTimer >= unlockHoldTime)
            {
                UnlockUpgrade();
                isHolding = false;
                circleSlider.fillAmount = 0;
            }
        }
    }
    public void ShowTooltipPanel()
    {
        tooltipPanel.SetActive(true);

        RectTransform myRect = GetComponent<RectTransform>();
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

        // Usar Pivot (0,0.5) en TooltipPanel
        float offsetX = myRect.rect.width / 2f + 20; // 20 px a la derecha del item
        tooltipRect.anchoredPosition = new Vector2(offsetX, 0);

        ClampTooltipToCanvas(tooltipRect);
    }


    void ClampTooltipToCanvas(RectTransform tooltipRect)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Vector2 anchoredPos = tooltipRect.anchoredPosition;
        Vector2 size = tooltipRect.sizeDelta;
        Vector2 canvasSize = canvasRect.sizeDelta;

        // Chequeá los bordes
        anchoredPos.x = Mathf.Clamp(anchoredPos.x, 0, canvasSize.x - size.x);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, -canvasSize.y / 2f + size.y / 2f, canvasSize.y / 2f - size.y / 2f);

        tooltipRect.anchoredPosition = anchoredPos;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltipPanel();

        // Posicionar el tooltip a la derecha del icono
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        tooltipRect.anchoredPosition = new Vector2(300f, 0f); // 100px a la derecha, ajustá a gusto

        // Rellená textos
        tooltipNameText.text = currentUpgrade.upgradeName;
        tooltipDescriptionText.text = currentUpgrade.description;
        bool isUnlocked = UpgradeManager.Instance != null && UpgradeManager.Instance.IsUnlocked(currentUpgrade.upgradeId);

        if (isUnlocked)
        {
            tooltipCostText.text = "Unlocked";
        }
        else
        {
            string costText;
            switch (currentUpgrade.category)
            {
                case UpgradeCategory.NormalWorld:
                    costText = $"Essence Normal World: {currentUpgrade.xpCost}";
                    break;
                case UpgradeCategory.OtherWorld:
                    costText = $"Essence Other World: {currentUpgrade.xpCost}";
                    break;
                case UpgradeCategory.General:
                    int mitad = currentUpgrade.xpCost / 2;
                    // Si el costo es impar, sumá 1 a Normal World

                    int normal = mitad + (currentUpgrade.xpCost % 2);
                    int other = mitad;

                    costText = $"Essence Normal World: {normal}\nEssence Other World: {other}";
                    break;
                default:
                    costText = $"Essence: {currentUpgrade.xpCost}";
                    break;
            }
            tooltipCostText.text = costText;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipPanel.SetActive(false);
        isHolding = false;
        holdTimer = 0;
        circleSlider.fillAmount = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Validar esencia suficiente antes de permitir el hold
        if (!HasEnoughEssenceForCurrentUpgrade())
        {
            // (Opcional) Feedback: podés mostrar un mensaje, cambiar color, etc.
            Debug.Log("No tenés la esencia suficiente para esta mejora.");
            return;
        }

        if (canUnlock)
        {
            isHolding = true;
            holdTimer = 0;
        }
    }



    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        holdTimer = 0;
        circleSlider.fillAmount = 0;
    }

    private void UnlockUpgrade()
    {
        Debug.Log("Intentando desbloquear: " + currentUpgrade.upgradeId);

        if (!canUnlock) return;

        unlockCallback?.Invoke(currentUpgrade.upgradeId);

        canUnlock = false;
        iconImage.color = new Color(1, 1, 1, 0.4f);
        tooltipPanel.SetActive(false);
    }
    private bool HasEnoughEssenceForCurrentUpgrade()
    {
        if (currentUpgrade == null) return false;

        switch (currentUpgrade.category)
        {
            case UpgradeCategory.NormalWorld:
                return PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal) >= currentUpgrade.xpCost;

            case UpgradeCategory.OtherWorld:
                return PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld) >= currentUpgrade.xpCost;

            case UpgradeCategory.General:
                int half = Mathf.CeilToInt(currentUpgrade.xpCost / 2f);
                return PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal) >= half
                    && PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld) >= half;

            default:
                return false;
        }
    }

}
