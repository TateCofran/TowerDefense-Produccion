using UnityEngine;
using UnityEngine.InputSystem;

public class TurretController : MonoBehaviour
{
    [Header("Turrets")]
    [SerializeField] private GameObject[] turretPrefabs;
    private GameObject selectedTurretPrefab;

    [Header("Turret Stats")]
    [SerializeField] private float attackSpeed = 10;
    [SerializeField] private float damage = 20;
    [SerializeField] private float range = 5;

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
                Debug.Log("Hiciste click en un tile de pasto: " + clickedCollider.name); //aca deberia instanciar a una torreta VER
                //instanciar torreta justo arriba de la posicion del tile de pasto traerme posicion de donde clickee

                Vector3 spawnPos = hit.point;
                float turretHeight = selectedTurretPrefab.transform.localScale.y;
                spawnPos.y = clickedCollider.bounds.max.y + turretHeight/2;

                //limito x y z para q no se salga del tile
                Vector3 halfSize = selectedTurretPrefab.transform.localScale/2f;

                spawnPos.x = Mathf.Clamp(spawnPos.x,
                                         clickedCollider.bounds.min.x + halfSize.x,
                                         clickedCollider.bounds.max.x - halfSize.x);

                spawnPos.z = Mathf.Clamp(spawnPos.z,
                                         clickedCollider.bounds.min.z + halfSize.z,
                                         clickedCollider.bounds.max.z - halfSize.z);

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

