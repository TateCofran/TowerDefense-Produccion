using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("Turrets")]
    [SerializeField] private GameObject[] turretPrefabs;
    private GameObject selectedTurretPrefab;

    [Header("Other configurations")]
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private Camera mainCamera;
    
    [Header("UI")]
    [SerializeField] private GameObject selectionPanel;

    private void OnEnable()
    {
        clickAction.action.performed += OnClickPerformed;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClickPerformed;
    }

    public void OpenSelectionPanel()
    {
        selectionPanel.SetActive(true);
    }

    public void SelectTurret(int index)
    {
        if (index >= 0 && index < turretPrefabs.Length)
        {
            selectedTurretPrefab = turretPrefabs[index];
            Debug.Log("Torreta seleccionada: " + selectedTurretPrefab.name);
        }

        selectionPanel.SetActive(false);
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        if (selectedTurretPrefab == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Collider clickedCollider = hit.collider;

            if (clickedCollider.CompareTag("Grass"))
            {
                Debug.Log("Hiciste click en un tile de pasto: " + clickedCollider.name);

                BoxCollider turretCollider = selectedTurretPrefab.GetComponent<BoxCollider>();

                //posición centrada sobre el tile
                Vector3 spawnPos = clickedCollider.bounds.center;
                spawnPos.y = clickedCollider.bounds.max.y + turretCollider.bounds.extents.y;

                //instanciar torreta solo si no hay otra
                if (clickedCollider.transform.childCount == 0)
                {
                    GameObject turret = Instantiate(selectedTurretPrefab, spawnPos, Quaternion.identity);
                    turret.transform.SetParent(clickedCollider.transform);
                    Debug.Log("Torreta colocada en: " + clickedCollider.name);
                }
                else
                {
                    Debug.Log("Ya hay una torreta en este tile: " + clickedCollider.name);
                }
            }
        }
    }
}

