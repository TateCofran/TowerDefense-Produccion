using System.Collections;
using UnityEngine;

public class BulletHellModifier : IGameModifier
{
    public string Name => "Bullet Hell";
    public string Description => "Al matar enemigos en el OtherWorld, hay un 10% de probabilidad de aumentar la velocidad de ataque por 3 segundos.";
    public ModifierCategory Category => ModifierCategory.OtherWorld;

    private const float BonusChance = 0.10f;
    private const float FireRateBonus = 1.5f;
    private const float Duration = 3f;
    private bool buffActive = false;

    public void Apply(GameModifiersManager manager)
    {
        Enemy.OnAnyEnemyKilled += OnEnemyKilledHandler;
    }

    public void Remove(GameModifiersManager manager)
    {
        Enemy.OnAnyEnemyKilled -= OnEnemyKilledHandler;
    }

    private void OnEnemyKilledHandler(Enemy enemy)
    {
        if (WorldManager.Instance.CurrentWorld == WorldState.OtherWorld && Random.value < BonusChance)
        {
            if (!buffActive)
            {
                buffActive = true;
                GameModifiersManager.Instance.turretFireRateMultiplier *= FireRateBonus;
                Debug.Log("[Bullet Hell] ¡Velocidad de ataque aumentada x1.5 por 3 segundos!");
                CoroutineRunner.Run(ResetBuffAfterDelay());
            }
        }
    }

    private IEnumerator ResetBuffAfterDelay()
    {
        yield return new WaitForSeconds(Duration);
        GameModifiersManager.Instance.turretFireRateMultiplier /= FireRateBonus;
        buffActive = false;
        Debug.Log("[Bullet Hell] Buff de velocidad de ataque finalizado.");
    }

    public string GetStackDescription(int stacks) => "";

}
