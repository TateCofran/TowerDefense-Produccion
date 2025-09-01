using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CoreUI : MonoBehaviour
{
    public static CoreUI Instance;

    [Header("UI")]
    public TMP_Text healthText;
    public TMP_Text coreLevelText;
    public TMP_Text upgradeCostText;
    public Button upgradeButton;

    [Header("Health Bar")]
    public Image greenFillImage; // instant fill (vida actual)
    public Image redFillImage;   // delayed fill (delay del daño)
    public float delaySpeed = 1.5f; // velocidad de "catch up" del delay

    private float targetFill = 1f;   // el valor real de vida
    private float delayedFill = 1f;  // el valor del delay (barra roja)
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        upgradeButton.onClick.AddListener(() => Core.Instance.UpgradeCore());
        UpdateUI();
    }
    void Update()
    {
        // La verde siempre sigue el valor real al instante
        greenFillImage.fillAmount = targetFill;

        // La roja va siguiendo suavemente a la verde
        if (redFillImage.fillAmount > targetFill)
        {
            redFillImage.fillAmount = Mathf.MoveTowards(redFillImage.fillAmount, targetFill, delaySpeed * Time.deltaTime);
        }
        else
        {
            redFillImage.fillAmount = targetFill; // Nunca puede ser menor
        }
    }
    public void UpdateUI()
    {
        if (Core.Instance == null) return;

        int current = Core.Instance.GetCurrentHealth();
        int max = Core.Instance.GetMaxHealth();
        int level = Core.Instance.GetCoreLevel();

        healthText.text = $" Health: {current} / {max}";
        coreLevelText.text = $"Level: {level}";

        targetFill = Mathf.Clamp01((float)current / max);

        if (Core.Instance.IsMaxLevel())
        {
            upgradeCostText.text = "Máximo nivel";
            upgradeButton.interactable = false;
        }
        else
        {
            int cost = Core.Instance.GetUpgradeCost();
            upgradeCostText.text = $"({cost} oro)";
            upgradeButton.interactable = GoldManager.Instance.HasEnoughGold(cost);
        }
    }

    public void OnCoreDamaged()
    {
        UpdateUI();
    }
}
