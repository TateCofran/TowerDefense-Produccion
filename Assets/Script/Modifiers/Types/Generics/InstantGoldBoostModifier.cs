using UnityEngine;

public class InstantGoldBoostModifier : IGameModifier
{
    public string Name => "Oro instantáneo";
    public string Description => "+50 de oro inmediato al elegir este modificador.";
    public ModifierCategory Category => ModifierCategory.ShiftWorld;

    public void Apply(GameModifiersManager manager)
    {
        GoldManager.Instance.AddGold(50);
        Debug.Log("Modificador aplicado: +50 de oro instantáneo.");
    }

    // Si permitís que este modificador se elija varias veces
    public string GetStackDescription(int stacks)
    {
        return $"+{stacks * 50} de oro total";
    }
}
