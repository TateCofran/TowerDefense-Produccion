using UnityEngine;
using TMPro;

public class EventUIController : MonoBehaviour
{
    public TMP_Text currentEventText;
    public TMP_Text nextEventText;

    private void Start()
    {
        // Suscribirse a los eventos de WaveManager
        WaveManager.Instance.OnWaveStarted += OnWaveChanged;
        UpdateEventUI(WaveManager.Instance.GetCurrentWave());
    }

    private void OnDestroy()
    {
        // Limpieza de eventos
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveStarted -= OnWaveChanged;
    }

    private void OnWaveChanged(int waveNumber, int totalEnemies)
    {
        UpdateEventUI(waveNumber);
    }

    private void UpdateEventUI(int currentWave)
    {
        string currentEvent = GetEventNameForWave(currentWave);
        int nextEventWave = GetNextEventWave(currentWave + 1); // el siguiente evento despu�s de la oleada actual
        string nextEvent = GetEventNameForWave(nextEventWave);

        // Mostr� el evento actual
        currentEventText.text = $"{currentEvent}";

        // Mostr� el pr�ximo evento
        int roundsLeft = nextEventWave - currentWave;
        nextEventText.text = $"In {roundsLeft} waves {nextEvent}";
    }

    private string GetEventNameForWave(int wave)
    {
        if (wave <= 0) return "";
        if (wave % 3 == 0) return "Modifier Selection";
        if (wave % 5 == 0) return "Mini Boss";
        if (wave % 15 == 0) return "Boss";
        return "Ninguno";
    }

    private int GetNextEventWave(int fromWave)
    {
        int nextMod = ((fromWave - 1) / 3 + 1) * 3;
        int nextMini = ((fromWave - 1) / 5 + 1) * 5;
        int nextBoss = ((fromWave - 1) / 15 + 1) * 15;

        // Buscar el evento m�s pr�ximo
        int nextEventWave = Mathf.Min(nextMod, nextMini, nextBoss);
        return nextEventWave;
    }
}
