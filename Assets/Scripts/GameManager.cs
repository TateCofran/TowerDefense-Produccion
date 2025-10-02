using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Pausa")]
    [SerializeField] private GameObject pausePanel;
    private bool isPaused = false;

    [Header("Victoria")]
    [SerializeField] private string resultScene = "ResultScene";
    [SerializeField] private int wavesToWin = 10;
    private int wavesCompleted = 0;

    [Header("Escenas comunes")]
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private string gameplayScene = "GameScene";

    public float timePlayed { get; private set; }
    public bool PlayerHasWon { get; private set; } 

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isPaused)
            timePlayed += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    #region Pausa
    public void PauseGame()
    {
        if (isPaused) return;
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        AudioListener.pause = false;
    }
    #endregion

    #region Flujo de partida
    public void NotifyWaveCompleted()
    {
        wavesCompleted++;
        if (wavesCompleted >= wavesToWin)
            WinGame();
    }

    public void GameOver()
    {
        PlayerHasWon = false;  // derrota
        EndRun();
    }

    private void WinGame()
    {
        PlayerHasWon = true;   // victoria
        EndRun();
    }

    private void EndRun()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(resultScene);
    }
    #endregion

    #region Botones ResultScene
    public void OnMainMenuButton()
    {
        ResetRunState(false);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnPlayAgainButton()
    {
        ResetRunState(false);
        SceneManager.LoadScene(gameplayScene);
    }
    #endregion

    private void ResetRunState(bool keepTime)
    {
        wavesCompleted = 0;
        if (!keepTime) timePlayed = 0f;
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
