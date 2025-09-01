using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public UpgradesPanelUI upgradesPanel;

    [SerializeField] private string levelScene = "GameScene";
    private void Start()
    {
        // Al entrar al menu, refresca la UI de upgrades y XP
        if (upgradesPanel != null)
        {
            upgradesPanel.RefreshUI();
        }

    }
    public void OnPlayButtonPressed()
    {
        SceneManager.LoadScene(levelScene);
    }
    public void OnExitButtonPressed()
    {
        Application.Quit();
    }
}
