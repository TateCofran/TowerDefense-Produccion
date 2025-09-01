using UnityEngine;

public class EnergeticChaosModifier : IGameModifier
{
    public string Name => "Caos Energético";
    public string Description => "En el OtherWorld, las torretas infligen 30% más daño, pero tienen 20% menos precisión (probabilidad de fallar).";
    public ModifierCategory Category => ModifierCategory.OtherWorld;

    private const float DamageBonus = 1.3f;
    private const float MissChance = 0.2f; // 20% de fallar

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
        ApplyModifierIfInOtherWorld();
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
        // Si se remueve estando en OtherWorld, quitá el buff.
        if (WorldManager.Instance.CurrentWorld == WorldState.OtherWorld)
        {
            GameModifiersManager.Instance.turretDamageMultiplier /= DamageBonus;
        }
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        ApplyModifierIfInOtherWorld();
    }

    private void ApplyModifierIfInOtherWorld()
    {
        if (WorldManager.Instance.CurrentWorld == WorldState.OtherWorld)
        {
            GameModifiersManager.Instance.turretDamageMultiplier *= DamageBonus;
            TurretManager.Instance.SetGlobalTurretMissChance(MissChance);
            Debug.Log("[Caos Energético] +30% daño, pero 20% de disparos pueden fallar.");
        }
        else
        {
            GameModifiersManager.Instance.turretDamageMultiplier /= DamageBonus;
            TurretManager.Instance.SetGlobalTurretMissChance(0f);
        }
    }

    public string GetStackDescription(int stacks) => "";

}
