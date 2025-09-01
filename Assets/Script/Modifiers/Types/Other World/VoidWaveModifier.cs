using System.Collections;
using UnityEngine;

public class VoidWaveModifier : IGameModifier
{
    public string Name => "Oleada del Vacío";
    public string Description => "Si pasás más de 20 segundos en el OtherWorld, todos los enemigos activos reciben +10% de velocidad, pero generan 5 de oro al morir.";
    public ModifierCategory Category => ModifierCategory.OtherWorld;

    private const float ThresholdTime = 20f;
    private const float SpeedBonus = 1.10f;
    private const int ExtraGold = 5;
    private bool buffApplied = false;
    private float timeInOtherWorld = 0f;

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
        Enemy.OnAnyEnemyKilled += OnEnemyKilledHandler;
        CoroutineRunner.Run(TickTimer());
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
        Enemy.OnAnyEnemyKilled -= OnEnemyKilledHandler;
        buffApplied = false;
        timeInOtherWorld = 0f;
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        if (newWorld != WorldState.OtherWorld)
        {
            buffApplied = false;
            timeInOtherWorld = 0f;
        }
    }

    private IEnumerator TickTimer()
    {
        while (true)
        {
            if (WorldManager.Instance.CurrentWorld == WorldState.OtherWorld && !buffApplied)
            {
                timeInOtherWorld += Time.deltaTime;
                if (timeInOtherWorld >= ThresholdTime)
                {
                    buffApplied = true;
                    // Buff a todos los enemigos activos
                    foreach (var enemy in EnemyTracker.GetActiveEnemies())
                        enemy.Movement.MultiplySpeed(SpeedBonus);

                    Debug.Log("[Oleada del Vacío] +10% velocidad a todos los enemigos activos en OtherWorld.");
                }
            }
            else if (WorldManager.Instance.CurrentWorld != WorldState.OtherWorld)
            {
                buffApplied = false;
                timeInOtherWorld = 0f;
            }
            yield return null;
        }
    }

    private void OnEnemyKilledHandler(Enemy enemy)
    {
        if (WorldManager.Instance.CurrentWorld == WorldState.OtherWorld && buffApplied)
        {
            GoldManager.Instance.AddGold(ExtraGold);
            Debug.Log("[Oleada del Vacío] +5 de oro por muerte en OtherWorld (Oleada del Vacío activo).");
        }
    }

    public string GetStackDescription(int stacks) => "";

}
