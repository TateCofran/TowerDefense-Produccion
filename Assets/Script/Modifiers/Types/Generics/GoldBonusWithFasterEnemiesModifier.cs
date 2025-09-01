using UnityEngine;

public class GoldBonusWithFasterEnemiesModifier : IGameModifier
{
    public string Name => "Más oro y enemigos más rápidos";
    public string Description => "+10 oro por oleada, +10% velocidad enemigos";

    public ModifierCategory Category => ModifierCategory.ShiftWorld;

    public void Apply(GameModifiersManager manager)
    {
        manager.goldPerWaveMultiplier += 10;
        manager.enemySpeedMultiplier += 0.1f;
    }

    public string GetStackDescription(int stacks)
    {
        return $"{stacks * 10} oro / {stacks * 10}% velocidad";
    }
}
