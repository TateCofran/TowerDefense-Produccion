using UnityEngine;

public class TurretCellLink : MonoBehaviour
{
    private CellInteraction parentCell;

    public void SetCell(CellInteraction cell)
    {
        parentCell = cell;
    }

    public void ReleaseCell(string turretId)
    {

        if (parentCell != null)
        {
            parentCell.RemoveTurret(turretId);
            parentCell = null;
        }

    }

    public CellInteraction GetCell()
    {
        return parentCell;
    }
}
