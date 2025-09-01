public class SolarEnergyModifier : IGameModifier
{
    public string Name => "Energía Solar";
    public string Description => "Mientras estés en el mundo Normal, todas las torretas disparan un 20% más rápido.";
    public ModifierCategory Category => ModifierCategory.NormalWorld;

    private const float FireRateBonus = 1.2f;

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
        ApplyModifierIfInNormal();
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
        // Si el modificador se quita estando en el mundo Normal, resetea el multiplicador
        if (WorldManager.Instance.CurrentWorld == WorldState.Normal)
        {
            GameModifiersManager.Instance.turretFireRateMultiplier /= FireRateBonus;
            RecalculateAllTurrets();
        }
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        ApplyModifierIfInNormal();
    }

    private void ApplyModifierIfInNormal()
    {
        if (WorldManager.Instance.CurrentWorld == WorldState.Normal)
        {
            GameModifiersManager.Instance.turretFireRateMultiplier *= FireRateBonus;
            RecalculateAllTurrets();
        }
        else
        {
            GameModifiersManager.Instance.turretFireRateMultiplier /= FireRateBonus;
            RecalculateAllTurrets();
        }
    }

    private void RecalculateAllTurrets()
    {
        foreach (var turret in TurretManager.Instance.GetAllTurrets())
            turret.Stats.RecalculateStats();
    }

    public string GetStackDescription(int stacks)
    {
        return $"+{stacks * 20}% fire rate";
    }
}
