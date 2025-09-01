using UnityEngine;

public class UIPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelUI;

    private void Awake()
    {
        if (panelUI == null)
        {
            Debug.LogError($"[UIPanelController] No asignaste el panelUI en {gameObject.name}.");
        }
    }

    public void Show()
    {
        if (panelUI != null && !panelUI.activeSelf)
            panelUI.SetActive(true);
    }

    public void Hide()
    {
        if (panelUI != null && panelUI.activeSelf)
            panelUI.SetActive(false);
    }

    public void Toggle()
    {
        if (panelUI != null)
            panelUI.SetActive(!panelUI.activeSelf);
    }
}
