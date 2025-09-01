using UnityEngine;

public class DimensionalExplorerModifier : IGameModifier
{
    public string Name => "Explorador dimensional";
    public string Description => "Cada vez que cambiás de mundo, ganás 10 de oro y el cooldown del cambio aumenta en 5 segundos.";
    public ModifierCategory Category => ModifierCategory.ShiftWorld;

    private const int GoldBonus = 10;
    private const float CooldownIncrease = 5f;

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        GoldManager.Instance.AddGold(GoldBonus);

        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.IncreaseShiftCooldown(CooldownIncrease);
            Debug.Log("[Explorador dimensional] +10 de oro y cooldown aumentado +5s.");
        }
    }

    public string GetStackDescription(int stacks) => "";

}
