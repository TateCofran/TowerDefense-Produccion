using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveEnemyGeneratorNormal : WaveEnemyGeneratorBase
{
    public static WaveEnemyGeneratorNormal Instance;
    private float EnemyCountMultiplier => WaveManager.Instance != null ? WaveManager.Instance.EnemyCountMultiplier : 1f;

    protected override WorldState TargetWorld => WorldState.Normal;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public new void Spawn(int waveNumber, int _)
    {
        Debug.Log($"[{GetType().Name}] Spawn llamado - Wave: {waveNumber} - Total a spawnear: {_}");

        if (WorldManager.Instance.CurrentWorld != TargetWorld) return;
        StartCoroutine(SpawnEnemies(waveNumber));
    }

    protected IEnumerator SpawnEnemies(int waveNumber)
    {
        Vector3[] path = gridManager.GetPathPositions();
        if (path == null || path.Length == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] El path está vacío.");
            yield break;
        }

        // Obtener la receta desde WaveManager
        var recipeList = WaveManager.Instance != null ? WaveManager.Instance.RecipeList : null;
        if (recipeList == null)
        {
            Debug.LogWarning("No se encontró WaveRecipeList en WaveManager.");
            yield break;
        }
        WaveRecipe recipe = recipeList.waveRecipes.Find(r => r.waveNumber == waveNumber);
        if (recipe == null)
        {
            Debug.LogWarning($"No hay receta configurada para la ronda {waveNumber}");
            yield break;
        }

        int totalSpawned = 0;

        foreach (var step in recipe.steps)
        {
            int realCount = Mathf.CeilToInt(step.count * EnemyCountMultiplier);

            for (int i = 0; i < realCount; i++)
            {
                Enemy enemy = enemySpawner.SpawnEnemy(path[0], path, step.enemyType);
                if (enemy != null)
                    enemy.SetOriginWorld(TargetWorld);

                totalSpawned++;
                if (i < realCount - 1)
                    yield return new WaitForSeconds(step.interval);
            }
            if (step.waitAfterStep > 0)
                yield return new WaitForSeconds(step.waitAfterStep);
        }

        Debug.Log($"[{GetType().Name}] Oleada {waveNumber} - {TargetWorld} - Generados: {totalSpawned} enemigos.");
    }

}
