using TMPro;
using UnityEngine;

public class WaveUIController : MonoBehaviour
{
    public static WaveUIController Instance { get; private set; }

    [Header("References UI")]
    [SerializeField] private TMP_Text waveCounterText;
    [SerializeField] private TMP_Text enemiesRemainingText;

    [SerializeField] private TMP_Text normalEssenceText;
    [SerializeField] private TMP_Text otherWorldEssenceText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += UpdateWaveUI;
            WaveManager.Instance.OnWaveEnded += TryTriggerNextWave;
        }
    }
    private int GetTotalEnemiesForWave(int waveNumber)
    {
        var recipeList = WaveManager.Instance != null ? WaveManager.Instance.RecipeList : null;
        if (recipeList == null)
            return 0;

        var recipe = recipeList.waveRecipes.Find(r => r.waveNumber == waveNumber);
        if (recipe == null)
            return 0;

        float multiplier = WaveManager.Instance != null ? WaveManager.Instance.EnemyCountMultiplier : 1f;
        int total = 0;
        foreach (var step in recipe.steps)
            total += Mathf.CeilToInt(step.count * multiplier);

        return total;
    }


    public void UpdateWaveUI(int waveNumber, int _)
    {
        if (waveCounterText != null)
            waveCounterText.text = $"Wave: {waveNumber}";

        int totalEnemiesThisWave = GetTotalEnemiesForWave(waveNumber);
        UpdateEnemiesRemaining(totalEnemiesThisWave);
        UpdateExperienceUI();
    }

    public void UpdateExperienceUI()
    {
        int normal = PlayerExperienceManager.Instance.GetSessionEssence(WorldState.Normal);
        int other = PlayerExperienceManager.Instance.GetSessionEssence(WorldState.OtherWorld);

        if (normalEssenceText == null)
            Debug.LogWarning("[WaveUIController] El campo normalEssenceText no está asignado en el inspector.");
        else
            normalEssenceText.text = $"{normal}";

        if (otherWorldEssenceText == null)
            Debug.LogWarning("[WaveUIController] El campo otherWorldEssenceText no está asignado en el inspector.");
        else
            otherWorldEssenceText.text = $"{other}";
    }

    public void UpdateEnemiesRemaining(int remaining)
    {
        if (enemiesRemainingText == null) return;

        // Obtener el total solo de la receta con multiplicador
        int waveNumber = WaveManager.Instance.GetCurrentWave();
        int total = 0;
        var recipeList = WaveManager.Instance != null ? WaveManager.Instance.RecipeList : null;
        if (recipeList != null)
        {
            var recipe = recipeList.waveRecipes.Find(r => r.waveNumber == waveNumber);
            if (recipe != null)
            {
                float multiplier = WaveManager.Instance != null ? WaveManager.Instance.EnemyCountMultiplier : 1f;
                foreach (var step in recipe.steps)
                    total += Mathf.CeilToInt(step.count * multiplier);
            }
        }

        enemiesRemainingText.text = $"{remaining}/{total}";
    }



    private void TryTriggerNextWave()
    {
        // Verificamos si hay un nuevo tile seleccionado y lo aplicamos antes de la nueva oleada
        if (WaveManager.Instance.GetCurrentWave() > 0)
        {
            UIManager.Instance.ShowTileSelection(GridManager.Instance.GetTileOptions());
            //UpdateSpawnMultiplierUI();

        }
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= UpdateWaveUI;
            WaveManager.Instance.OnWaveEnded -= TryTriggerNextWave;
        }
    }
}
