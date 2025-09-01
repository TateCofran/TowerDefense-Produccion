using UnityEngine;

public class ModifierTestKey : MonoBehaviour
{
    private ModifierPanelSelection panelSelection;

    void Start()
    {
        panelSelection = FindFirstObjectByType<ModifierPanelSelection>();
        if (panelSelection == null)
            Debug.LogWarning("[ModifierTestKey] No se encontró ModifierPanelSelection en la escena.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (panelSelection != null)
            {
                panelSelection.OpenPanel(() =>
                {
                    Debug.Log("[ModifierTestKey] Modificador elegido por test.");
                });
            }
            else
            {
                Debug.LogWarning("[ModifierTestKey] No se puede abrir el panel porque no existe.");
            }
        }
    }
}
