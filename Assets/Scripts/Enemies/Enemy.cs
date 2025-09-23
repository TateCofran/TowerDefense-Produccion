using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{

    [Header("Settings")]
    public float speed = 2f;
    public float arrivalThreshold = 0.3f;
    [SerializeField] private float waypointTolerance = 0.03f;
    [SerializeField] private bool faceDirection = true;

    private readonly List<Vector3> _route = new List<Vector3>();
    private int _idx;

    public EnemyHealth Health { get; private set; }
    public EnemyHealthBar HealthBar { get; private set; }

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        HealthBar = GetComponent<EnemyHealthBar>();
    }

    public void Init(IList<Vector3> worldRoute, float moveSpeed)
    {
        speed = Mathf.Max(0.01f, moveSpeed);
        _route.Clear();
        if (worldRoute != null) _route.AddRange(worldRoute);
        _idx = 0;
        if (_route.Count > 0) transform.position = _route[0];
    }
    private void Update()
    {
        if (_route.Count == 0 || _idx >= _route.Count) return;

        var current = transform.position;
        var target = _route[_idx];
        var to = target - current;

        if (to.sqrMagnitude <= waypointTolerance * waypointTolerance)
        {
            _idx++;
            if (_idx >= _route.Count)
            {
                OnArrived();
                return;
            }
            target = _route[_idx];
            to = target - current;
        }

        var dir = to.normalized;
        transform.position = Vector3.MoveTowards(current, target, speed * Time.deltaTime);

        if (faceDirection && dir.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.25f);
        }
    }

    private void OnArrived()
    {
        // TODO: VFX, daño al Core, etc.
        Destroy(gameObject);
    }
}
