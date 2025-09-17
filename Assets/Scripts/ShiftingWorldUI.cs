using System;
using UnityEngine;
using UnityEngine.UI;

public class ShiftingWorldUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject normalWorldPanel;
    [SerializeField] private GameObject otherWorldPanel;

    // Botones de cerrar (opcional)
    [SerializeField] private Button normalCloseButton;
    [SerializeField] private Button otherCloseButton;

    private Action onNormalClosed;
    private Action onOtherClosed;

    void Awake()
    {
        if (normalCloseButton != null)
            normalCloseButton.onClick.AddListener(CloseNormal);

        if (otherCloseButton != null)
            otherCloseButton.onClick.AddListener(CloseOther);

        HideAll();
    }

    public void ShowNormalReached(Action onClosed = null)
    {
        onNormalClosed = onClosed;
        if (normalWorldPanel != null) normalWorldPanel.SetActive(true);
    }

    public void ShowOtherReached(Action onClosed = null)
    {
        onOtherClosed = onClosed;
        if (otherWorldPanel != null) otherWorldPanel.SetActive(true);
    }

    public void CloseNormal()
    {
        if (normalWorldPanel != null) normalWorldPanel.SetActive(false);
        onNormalClosed?.Invoke();
        onNormalClosed = null;
    }

    public void CloseOther()
    {
        if (otherWorldPanel != null) otherWorldPanel.SetActive(false);
        onOtherClosed?.Invoke();
        onOtherClosed = null;
    }

    public void HideAll()
    {
        if (normalWorldPanel != null) normalWorldPanel.SetActive(false);
        if (otherWorldPanel != null) otherWorldPanel.SetActive(false);
    }
}
