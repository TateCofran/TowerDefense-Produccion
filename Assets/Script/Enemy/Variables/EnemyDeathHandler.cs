using UnityEngine;

public class EnemyDeathHandler : MonoBehaviour, IEnemyDeathHandler
{
    public void OnEnemyDeath(Enemy enemy)
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.NotifyEnemyKilled();

        if (EnemyPool.Instance != null)
            EnemyPool.Instance.ReturnEnemy(enemy.Type, enemy.gameObject);

        // Sumar essence según tipo y mundo del enemigo
        EnemyTracker.NotifyEnemyKilledGlobal(enemy);

        enemy.NotifyDeath();
    }

}
