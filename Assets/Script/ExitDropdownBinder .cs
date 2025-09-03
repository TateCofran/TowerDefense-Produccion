using System.Collections.Generic;
using UnityEngine;
using TMPro;           // <- importante para TMP_Dropdown
using UnityEngine.UI; // por si querés usar el Dropdown estándar

public class ExitDropdownBinder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridGenerator gridGenerator;
    [SerializeField] private TMP_Dropdown tmpDropdown;   // arrastrá tu Dropdown - TextMeshPro
    [SerializeField] private Dropdown uDropdown;         // opcional: si usás el UI Dropdown viejo

    void Awake()
    {
        // Suscribimos listeners (una sola vez)
        if (tmpDropdown != null)
            tmpDropdown.onValueChanged.AddListener(OnValueChangedTMP);
        if (uDropdown != null)
            uDropdown.onValueChanged.AddListener(OnValueChangedUGUI);
    }

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        if (gridGenerator == null) return;

        // 1) Obtener etiquetas disponibles (A, B, C, …)
        List<string> labels = gridGenerator.GetAvailableExitLabels();

        // 2) Rellenar el dropdown TMP
        if (tmpDropdown != null)
        {
            tmpDropdown.ClearOptions();
            // Si no hay exits, mostramos una opción gris para feedback
            if (labels.Count == 0)
            {
                tmpDropdown.options.Clear();
                tmpDropdown.options.Add(new TMP_Dropdown.OptionData("— sin EXITS —"));
                tmpDropdown.value = 0;
                tmpDropdown.RefreshShownValue();
                tmpDropdown.interactable = false;
            }
            else
            {
                tmpDropdown.AddOptions(labels);
                tmpDropdown.value = Mathf.Clamp(tmpDropdown.value, 0, labels.Count - 1);
                tmpDropdown.RefreshShownValue();
                tmpDropdown.interactable = true;
            }
        }

        // 3) (Opcional) UGUI Dropdown estándar
        if (uDropdown != null)
        {
            uDropdown.ClearOptions();
            if (labels.Count == 0)
            {
                uDropdown.options.Add(new Dropdown.OptionData("— sin EXITS —"));
                uDropdown.value = 0;
                uDropdown.RefreshShownValue();
                uDropdown.interactable = false;
            }
            else
            {
                uDropdown.AddOptions(labels);
                uDropdown.value = Mathf.Clamp(uDropdown.value, 0, labels.Count - 1);
                uDropdown.RefreshShownValue();
                uDropdown.interactable = true;
            }
        }
    }

    // Llamado por TMP_Dropdown al cambiar
    private void OnValueChangedTMP(int index)
    {
        if (gridGenerator == null) return;
        gridGenerator.UI_SetExitIndex(index);
    }
    // Llamado por UI Dropdown clásico
    private void OnValueChangedUGUI(int index)
    {
        if (gridGenerator == null) return;
        gridGenerator.UI_SetExitIndex(index);
    }
}
