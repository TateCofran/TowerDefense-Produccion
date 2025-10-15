using System.Collections;
using UnityEditor.SearchService;
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
    //private int wavesCompleted = 0;

    [Header("Escenas comunes")]
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private string gameplayScene = "GameScene";

    public float timePlayed { get; private set; }
    public bool PlayerHasWon { get; private set; }
    public int wavesCompleted { get; private set; }
    public int totalEnemiesKilled { get; private set; }

    private bool subscribedToWave = false;

    /*[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            var go = new GameObject("[GameManager]");
            go.AddComponent<GameManager>();
        }
    }*/

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
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveEnded -= NotifyWaveCompleted;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameplayScene)
            ResetRunState(false);
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Esperar hasta que exista el WaveManager en la escena
        while (WaveManager.Instance == null)
            yield return null;

        // Suscribirse al evento una sola vez
        if (!subscribedToWave)
        {
            WaveManager.Instance.OnWaveEnded -= NotifyWaveCompleted;
            WaveManager.Instance.OnWaveEnded += NotifyWaveCompleted;
            subscribedToWave = true;
            Debug.Log("[GameManager] Suscrito correctamente a WaveManager.OnWaveEnded.");
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
        if (WaveManager.Instance != null)
            totalEnemiesKilled = WaveManager.Instance.GetTotalEnemiesKilled();

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

