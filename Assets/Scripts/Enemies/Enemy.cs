using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;
    public EnemyType Type => data != null ? data.type : EnemyType.Minion;

    private bool hasHitCore = false;

    public static event System.Action<Enemy> OnAnyEnemyKilled;

    [Header("Settings")]
    //public float speed = 2f;
    //public float arrivalThreshold = 0.3f;
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

        if (data == null)
            Debug.LogError($"[Enemy] {name} no tiene asignado EnemyData");
    }

    public void Init(IList<Vector3> worldRoute)
    {
        //data.moveSpeed = Mathf.Max(0.01f, moveSpeed);
        _route.Clear();
        if (worldRoute != null) _route.AddRange(worldRoute);
        _idx = 0;
        if (_route.Count > 0) transform.position = _route[0];

        // Inicializar vida primero (con valores reales)
        if (Health != null && data != null)
            Health.Initialize(data.maxHealth, data.defense);

        HealthBar?.Initialize(transform, Health != null ? Health.GetMaxHealth() : 1f);
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
        transform.position = Vector3.MoveTowards(current, target, data.moveSpeed * Time.deltaTime);

        if (faceDirection && dir.sqrMagnitude > 0.0001f)
        {
            var look = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.25f);
        }
    }
    private void OnEnable()
    {
        EnemyTracker.RegisterEnemy(this);
    }
    private void OnDisable()
    {
        EnemyTracker.UnregisterEnemy(this);
    }
    private void OnArrived()
    {
        // TODO: VFX, daño al Core, etc.
        Destroy(gameObject);
    }

    public void ResetEnemy()
    {
        hasHitCore = false;
        Health?.Initialize(data.maxHealth, data.defense);

        // Reiniciar la barra de vida al máximo
        HealthBar?.Initialize(transform, Health.GetMaxHealth());
        HealthBar?.UpdateHealthBar(Health.GetMaxHealth(), Health.GetMaxHealth());

        // Cualquier otra lógica que tu enemigo requiera...
    }

    public void NotifyDeath()
    {
        OnAnyEnemyKilled?.Invoke(this);
    }
}
