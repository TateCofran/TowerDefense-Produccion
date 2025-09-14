using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DropdownManager : MonoBehaviour
{
    [Header("Tag del Dropdown a controlar")]
    [SerializeField] private string dropdownTag = "AttackModeDropdown";

    private TMP_Dropdown attackModeDropdown;

    private void Start()
    {
        GameObject dropdownObj = GameObject.FindGameObjectWithTag(dropdownTag);

        attackModeDropdown = dropdownObj.GetComponent<TMP_Dropdown>();

        attackModeDropdown.onValueChanged.AddListener(OnDropdownChanged); //si cambió el valor del dropdown
    }

    private void OnDestroy()
    {
        if (attackModeDropdown != null)
            attackModeDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        if (AttackModeManager.Instance != null)
        {
            AttackModeManager.Instance.SetAttackMode(index);
        }
        else
        {
            Debug.Log("nulo");
        }
    }
}

