using UnityEngine;

public class LegendaryNormalModifier : IGameModifier
{
    public string Name => "Modificador Legendario (Normal)";
    public string Description => "Ralentiza a todos los enemigos un 30% cada vez que cambias al mundo normal; en OtherWorld, los enemigos aumentan su velocidad un 25%.";
    public ModifierCategory Category => ModifierCategory.LegendaryNormal;

    private const float SlowAmount = 0.7f; // 30% más lento (multiplicador)
    private const float SpeedBuff = 1.25f; // 25% más rápido

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChanged;
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChanged;
    }

    private void OnWorldChanged(WorldState newWorld)
    {
        if (newWorld == WorldState.Normal)
        {
            foreach (var enemy in EnemyTracker.GetActiveEnemies())
                enemy.Movement.MultiplySpeed(SlowAmount);
            Debug.Log("[Legendario Normal] Todos los enemigos ralentizados 30%.");
        }
        else if (newWorld == WorldState.OtherWorld)
        {
            foreach (var enemy in EnemyTracker.GetActiveEnemies())
                enemy.Movement.MultiplySpeed(SpeedBuff);
            Debug.Log("[Legendario Normal] Todos los enemigos aumentan su velocidad 25% en OtherWorld.");
        }
    }

    public string GetStackDescription(int stacks) => "";
}
