using UnityEngine;

public class ChaosAddictionModifier : IGameModifier
{
    public string Name => "Adicción al caos";
    public string Description => "Cada vez que cambiás de mundo, la velocidad de los enemigos aumenta un 5% durante esa oleada.";

    public ModifierCategory Category => ModifierCategory.ShiftWorld;

    private const float SpeedBuff = 1.05f;

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
        GameModifiersManager.Instance.enemySpeedMultiplier *= SpeedBuff;
        Debug.Log("[Adicción al caos] Velocidad de enemigos +5% por el resto de la oleada.");
    }

    public string GetStackDescription(int stacks) => "";

}
