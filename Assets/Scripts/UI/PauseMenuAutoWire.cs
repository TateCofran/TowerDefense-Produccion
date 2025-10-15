using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenuAutoWire : MonoBehaviour
{
    [Header("Botones del men� de pausa")]
    [SerializeField] private Button resumeButton;   // Contin�a
    [SerializeField] private Button restartButton;  // Reiniciar partida
    [SerializeField] private Button menuButton;     // Volver al men�

    private void OnEnable()
    {
        // Puede activarse tras recargar escena o al pausar
        WireButtons();
    }

    private void Start()
    {
        // Por si el panel ya estaba activo al entrar a la escena
        WireButtons();
    }

    private void WireButtons()
    {
        var gm = GameManager.Instance;
        if (gm == null) return; // A�n no est� listo

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(gm.ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            // Usa la API del GameManager para reiniciar la escena de juego
            restartButton.onClick.AddListener(gm.OnPlayAgainButton);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(gm.OnMainMenuButton);
        }
    }
}
