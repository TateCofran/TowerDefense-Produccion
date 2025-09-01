using Unity.VisualScripting;
using UnityEngine;

public class EnergySynchronyModifier : IGameModifier
{
    public string Name => "Sincronía de energía";
    public string Description => "El primer cambio de mundo en cada oleada otorga +5% de daño a las torretas durante 5 segundos.";
    public ModifierCategory Category => ModifierCategory.ShiftWorld;

    private const float DamageBuff = 1.05f;
    private const float BuffDuration = 5f;
    private bool appliedThisWave = false;

    public void Apply(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged += OnWorldChangedHandler;
        WaveManager.Instance.OnWaveStarted += OnWaveStartedHandler;
    }

    public void Remove(GameModifiersManager manager)
    {
        WorldManager.OnWorldChanged -= OnWorldChangedHandler;
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= OnWaveStartedHandler;
    }

    private void OnWaveStartedHandler(int waveNumber, int totalEnemies)
    {
        appliedThisWave = false;
    }

    private void OnWorldChangedHandler(WorldState newWorld)
    {
        if (!appliedThisWave)
        {
            appliedThisWave = true;
            GameModifiersManager.Instance.turretDamageMultiplier *= DamageBuff;
            Debug.Log("[Sincronía de energía] +5% de daño a torretas por 5 segundos.");

            // Iniciar corrutina para resetear el buff
            CoroutineRunner.Run(ResetBuffAfterDelay());
        }
    }

    private System.Collections.IEnumerator ResetBuffAfterDelay()
    {
        yield return new WaitForSeconds(BuffDuration);
        GameModifiersManager.Instance.turretDamageMultiplier /= DamageBuff;
        Debug.Log("[Sincronía de energía] Buff de daño finalizado.");
    }

    public string GetStackDescription(int stacks) => "";

}
