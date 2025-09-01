using UnityEngine;

public class LegendaryOtherModifier : IGameModifier
{
    public string Name => "Modificador Legendario (OtherWorld)";
    public string Description => "Haces 3% de daño a todos los enemigos cada vez que cambias al OtherWorld; aumenta la vida de los enemigos en el mundo normal un 5%.";
    public ModifierCategory Category => ModifierCategory.LegendaryOther;

    private const float DamagePercent = 0.03f;
    private const float HealthBuff = 1.05f;

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
        if (newWorld == WorldState.OtherWorld)
        {
            foreach (var enemy in EnemyTracker.GetActiveEnemies())
            {
                float damage = enemy.Health.GetMaxHealth() * DamagePercent;
                enemy.Health.TakeDamage(damage);
            }
            Debug.Log("[Legendario OtherWorld] 3% de daño a todos los enemigos.");
        }
        else if (newWorld == WorldState.Normal)
        {
            foreach (var enemy in EnemyTracker.GetActiveEnemies())
            {
                enemy.Health.MultiplyMaxHealth(HealthBuff);
            }
            Debug.Log("[Legendario OtherWorld] +5% vida máxima a enemigos en NormalWorld.");
        }
    }

    public string GetStackDescription(int stacks) => "";
}
