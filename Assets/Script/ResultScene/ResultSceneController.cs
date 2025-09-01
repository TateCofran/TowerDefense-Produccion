using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ResultSceneController : MonoBehaviour
{
    public TMP_Text timeText;
    public TMP_Text wavesText;
    public TMP_Text sessionNormalEssenceText;
    public TMP_Text sessionOtherEssenceText;
    public TMP_Text totalNormalEssenceText;
    public TMP_Text totalOtherEssenceText;


    void Start()
    {
        var data = ResultData.GetData();

        timeText.text = $"Tiempo de juego: {FormatTime(data.timePlayed)}";
        wavesText.text = $"Oleadas completadas: {data.wavesCompleted}";

        // Mostramos esencias de la sesión
        sessionNormalEssenceText.text = $"Normal Essence Ganada: <color=#38C172>{data.sessionNormalEssence}</color>";
        sessionOtherEssenceText.text = $"OtherWorld Essence Ganada: <color=#6CB2EB>{data.sessionOtherWorldEssence}</color>";

        // Mostramos total acumulado
        totalNormalEssenceText.text = $"Normal Essence Total: <color=#38C172>{PlayerExperienceManager.Instance.GetTotalEssence(WorldState.Normal)}</color>";
        totalOtherEssenceText.text = $"OtherWorld Essence Total: <color=#6CB2EB>{PlayerExperienceManager.Instance.GetTotalEssence(WorldState.OtherWorld)}</color>";
    }


    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnPlayAgainButton()
    {
        SceneManager.LoadScene("GameScene");
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:D2}:{secs:D2}";
    }
}
