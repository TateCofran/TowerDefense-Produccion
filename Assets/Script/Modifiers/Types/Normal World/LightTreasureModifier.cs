using UnityEngine;

public class LightTreasureModifier : IGameModifier
{
    public string Name => "Tesoro de la Luz";
    public string Description => "Por cada enemigo destruido en el mundo Normal, ganás 1 de oro extra.";
    public ModifierCategory Category => ModifierCategory.NormalWorld;

    public void Apply(GameModifiersManager manager)
    {
        Enemy.OnAnyEnemyKilled += OnEnemyKilled;
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        if (WorldManager.Instance.CurrentWorld == WorldState.Normal)
        {
            GoldManager.Instance.AddGold(1);
            Debug.Log("[Tesoro de la Luz] +1 de oro extra (Normal World).");
        }
    }

    public string GetStackDescription(int stacks) => $"+{stacks} oro extra por kill";
}

