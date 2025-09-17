using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShiftingWorldUI : MonoBehaviour
{
    public enum PanelMode { Normal, Otro }

    [Header("Refs")]
    [SerializeField] private GridGenerator grid;      // para pedir candidatos y forzar el layout
    [SerializeField] private GameObject panelRoot;    // panel contenedor (activar/desactivar)
    [SerializeField] private Button[] optionButtons;  // tamaño 3
    [SerializeField] private TMP_Text[] optionLabels; // opcional si usás TMP (mismo tamaño que botones)

    private Action onClosed;
    private PanelMode currentMode;

    // Cache de opciones actuales
    private List<TileLayout> currentTileOptions = new();  // para modo Normal
    private List<string> currentTurretOptions = new(); // para modo Otro (placeholder)

    void Awake()
    {
        Hide();
    }

    public void ShowNormalReached(Action closedCb = null)
    {
        currentMode = PanelMode.Normal;
        onClosed = closedCb;
        BuildNormalOptions();
        Show();
    }

    public void ShowOtherReached(Action closedCb = null)
    {
        currentMode = PanelMode.Otro;
        onClosed = closedCb;
        BuildOtherOptions();
        Show();
    }

    private void BuildNormalOptions()
    {
        currentTileOptions.Clear();

        if (grid == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] Grid no asignado.");
            SetButtonsDisabled("Grid no asignado");
            return;
        }

        var candidates = grid.GetRandomCandidateSet(3);
        // Asegurar exactamente 3 slots (si faltan, repetimos/llenamos con null)
        for (int i = 0; i < 3; i++)
        {
            var layout = (i < candidates.Count) ? candidates[i] : null;
            currentTileOptions.Add(layout);

            string label = layout != null ? layout.name : "N/D";
            BindButton(i, label, () => OnChooseTile(i));
        }
    }

    private void BuildOtherOptions()
    {
        currentTurretOptions.Clear();

        // Placeholder: tres torretas inventadas por ahora
        currentTurretOptions.Add("Torreta A");
        currentTurretOptions.Add("Torreta B");
        currentTurretOptions.Add("Torreta C");

        for (int i = 0; i < 3; i++)
        {
            string label = currentTurretOptions[i];
            BindButton(i, label, () => OnChooseTurret(i));
        }
    }

    private void BindButton(int index, string label, Action onClick)
    {
        if (index < 0 || index >= optionButtons.Length) return;

        var btn = optionButtons[index];
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick());

        if (optionLabels != null && index < optionLabels.Length && optionLabels[index] != null)
            optionLabels[index].text = label;
        else
        {
            // Si no usás TMP_Text, probá a buscar Text (UGUI) en el hijo
            var legacyText = btn.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = label;
        }

        btn.interactable = true;
        btn.gameObject.SetActive(true);
    }

    private void SetButtonsDisabled(string label)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var btn = optionButtons[i];
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();
            btn.interactable = false;

            if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                optionLabels[i].text = label;

            btn.gameObject.SetActive(true);
        }
    }

    private void OnChooseTile(int index)
    {
        var chosen = (index >= 0 && index < currentTileOptions.Count) ? currentTileOptions[index] : null;
        if (chosen == null)
        {
            Debug.LogWarning("[ShiftingWorldUI] opción de tile inválida.");
            Close();
            return;
        }

        bool ok = grid.AppendNextUsingSelectedExitWithLayout(chosen);
        if (!ok)
            Debug.LogWarning($"[ShiftingWorldUI] No se pudo colocar el layout elegido: {chosen.name}");

        Close();
    }

    private void OnChooseTurret(int index)
    {
        string label = (index >= 0 && index < currentTurretOptions.Count) ? currentTurretOptions[index] : "N/D";
        Debug.Log($"[ShiftingWorldUI] (Otro Mundo) Elegiste torreta: {label} (pendiente implementación)");
        // TODO: aquí enganchar con tu flujo de construcción/compra de torretas.
        Close();
    }

    public void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Close()
    {
        Hide();
        onClosed?.Invoke();
        onClosed = null;
        currentTileOptions.Clear();
        currentTurretOptions.Clear();
    }
}
