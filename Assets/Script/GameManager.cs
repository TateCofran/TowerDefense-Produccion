using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Analytics;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;

    public float timePlayed { get; private set; } // p�blico para que lo use ResultScene

    private void Update()
    {
        // No sumar tiempo si est� pausado
        if (!isPaused)
        {
            timePlayed += Time.deltaTime;

        }
        // Pausa y reanuda con ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        AudioListener.pause = true;
    }
    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        AudioListener.pause = false;
    }
    public void GameOver()
    {
        int wave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 0;

        // Guardar ambas esencias de la sesi�n para la ResultScene
        int normalEssenceGain = PlayerExperienceManager.Instance.GetSessionEssence(WorldState.Normal);
        int otherEssenceGain = PlayerExperienceManager.Instance.GetSessionEssence(WorldState.OtherWorld);
        ResultData.SetData(timePlayed, wave, normalEssenceGain, otherEssenceGain);

        // Sumar ambas esencias de la sesi�n al total acumulado SOLO en GameOver
        PlayerExperienceManager.Instance.AddEssenceSessionToTotal();

        SceneManager.LoadScene("ResultScene");
    }
    //Eliminar Funcion de ResultSceneController m�s adelante
    public void OnMainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }

    //Eliminar Funcion de ResultSceneController m�s adelante
    public void OnPlayAgainButton()
    {
        SceneManager.LoadScene("GameScene");
    }
}
