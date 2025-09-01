public class ApparentPeaceModifier : IGameModifier
{
    public string Name => "Paz Aparente";
    public string Description => "En el mundo Normal, los enemigos aparecen un 15% más lentos pero reciben 10% menos daño.";

    private const float SpeedBonus = 0.85f; // 15% más lentos
    private const float DamageReduction = 0.9f; // 10% menos daño
    public ModifierCategory Category => ModifierCategory.NormalWorld;

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
        ApplyModifierIfInNormal();
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
        // Volver a valores normales por si el modificador se quita en mundo Normal
        var modManager = GameModifiersManager.Instance;
        if (modManager == null) return;
        modManager.enemySpeedMultiplier /= SpeedBonus;
        modManager.enemyDamageTakenMultiplier /= DamageReduction;
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        ApplyModifierIfInNormal();
    }

    private void ApplyModifierIfInNormal()
    {
        var modManager = GameModifiersManager.Instance;

        if (modManager == null) return;

        if (WorldManager.Instance.CurrentWorld == WorldState.Normal)
        {
            modManager.enemySpeedMultiplier *= SpeedBonus;

            // Si no tenés esta variable, agregala a GameModifiersManager:
            // public float enemyDamageTakenMultiplier = 1f;
            modManager.enemyDamageTakenMultiplier *= DamageReduction;
        }
        else
        {
            modManager.enemySpeedMultiplier /= SpeedBonus;
            modManager.enemyDamageTakenMultiplier /= DamageReduction;
        }
    }

    public string GetStackDescription(int stacks)
    {
        return $"-{stacks * 15}% velocidad enemigos, -{stacks * 10}% daño recibido";
    }
}
