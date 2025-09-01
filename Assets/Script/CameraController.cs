using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 40f;

    private Camera cam;
    [SerializeField] CinemachineCamera cam1;
    [SerializeField] CinemachineCamera cam2;

    void Awake()
    {
        if (cam1 != null)
            CameraManager.Register(cam1);

        if (cam2 != null)
            CameraManager.Register(cam2);
        CameraManager.SwitchCamera(cam2);

    }

    void Start()
    {

        cam = Camera.main;

        StartCoroutine(DelayedSwitchCamera(cam1));
    }

    IEnumerator DelayedSwitchCamera(CinemachineCamera cam)
    {
        yield return null;
        CameraManager.SwitchCamera(cam);
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A(-1), D(1)
        float vertical = Input.GetAxis("Vertical"); // S(-1), W(1)

        Vector3 direction = new Vector3(horizontal, 0, vertical);
        Vector3 movement = direction * moveSpeed * Time.deltaTime;

        transform.Translate(movement, Space.World);
    }

    void HandleZoom()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            cam.fieldOfView -= zoomSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.R))
        {
            cam.fieldOfView += zoomSpeed * Time.deltaTime;
        }

        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, minZoom, maxZoom);
    }
}
