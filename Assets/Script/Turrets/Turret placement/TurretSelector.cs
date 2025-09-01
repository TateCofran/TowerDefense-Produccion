using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TurretSelector : MonoBehaviour
{
    public static TurretSelector Instance;
    private Turret selectedTurret;

    void Awake()
    {
        Instance = this;
    }

    public void SelectTurret(Turret turret)
    {
        // Si ya estaba seleccionada, deselecciona y oculta el panel
        if (selectedTurret == turret)
        {
            TurretInfoUI.Instance.Hide();
            selectedTurret = null;
            turret.HideRange();
            return;
        }

        // Deselect previous
        if (selectedTurret != null)
        {
            selectedTurret.HideRange();
        }

        selectedTurret = turret;

        // Mostrar info
        TurretInfoUI.Instance.Initialize(turret);
        TurretInfoUI.Instance.Show();
        turret.ShowRange();
    }



    public bool IsSelected(Turret turret)
    {
        return selectedTurret == turret;
    }

    public void Deselect()
    {
        if (selectedTurret != null)
        {
            selectedTurret.GetComponent<TurretRangeVisualizer>()?.Hide(); 
            TurretInfoUI.Instance.Hide();
            selectedTurret = null;
        }
    }
}
