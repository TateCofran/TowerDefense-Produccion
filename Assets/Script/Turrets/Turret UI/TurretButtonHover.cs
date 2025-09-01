using UnityEngine;
using UnityEngine.EventSystems;

public class TurretButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string turretId;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TurretTooltipUI.Instance.Show(turretId);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TurretTooltipUI.Instance.Hide();
    }
}
