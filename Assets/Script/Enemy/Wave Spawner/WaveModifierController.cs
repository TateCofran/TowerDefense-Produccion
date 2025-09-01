using System;
using UnityEngine;

public class WaveModifierController : MonoBehaviour
{
    private Action onModifierChosen;

    private void Start()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveEnded += CheckModifierPanel;
        }
    }

    private void CheckModifierPanel()
    {
        if (WaveManager.Instance.GetCurrentWave() % 3 == 0 && !WaveManager.Instance.IsLastWave())
        {
            ModifierPanelSelection panel = FindFirstObjectByType<ModifierPanelSelection>();
            if (panel != null)
            {
                panel.OpenPanel(OnModifierConfirmed);
            }
            else
            {
                Debug.LogWarning("[WaveModifierController] No se encontró el panel de modificadores.");
                OnModifierConfirmed();
            }
        }
        else
        {
            OnModifierConfirmed();
        }
    }

    private void OnModifierConfirmed()
    {
        Debug.Log("[WaveModifierController] Modificador elegido. Continuando...");
        UIManager.Instance.ShowTileSelection(GridManager.Instance.GetTileOptions());
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveEnded -= CheckModifierPanel;
        }
    }
}
