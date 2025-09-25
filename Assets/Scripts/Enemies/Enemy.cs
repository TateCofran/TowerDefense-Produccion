using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour, IEnemyDeathHandler // <- implementamos el handler
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;
    public EnemyType Type => data != null ? data.type : EnemyType.Minion;

    private bool hasHitCore = false;

    public static event System.Action<Enemy> OnAnyEnemyKilled;

    [Header("Settings")]
    [SerializeField] private float waypointTolerance = 0.03f;
    [SerializeField] private bool faceDirection = true;

    private readonly List<Vector3> _route = new List<Vector3>();
    private int _idx;

    public EnemyHealth Health { get; private set; }
    public EnemyHealthBar HealthBar { get; private set; }

    // Evita notificar muerte más de una vez (pooling / Destroy / llegada al core)
    private bool _removedFromWave; // renombrado para contemplar ambas causas

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        HealthBar = GetComponent<EnemyHealthBar>();

        if (data == null)
            Debug.LogError($"[Enemy] {name} no tiene asignado EnemyData");
    }
    public void Init(IList<Vector3> worldRoute)
    {
        _route.Clear();
        if (worldRoute != null) _route.AddRange(worldRoute);
        _idx = 0;
        if (_route.Count > 0) transform.position = _route[0];

        _removedFromWave = false;
        hasHitCore = false;

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
    private void OnTriggerEnter(Collider other)
    {
        if (hasHitCore) return;

        if (other.TryGetComponent<Core>(out var core))
        {
            hasHitCore = true;
            core.TakeDamage(data.damageToCore);
            ReturnToPoolOrDisable();
        }
    }

    private void OnEnable()
    {
        EnemyTracker.RegisterEnemy(this);
        _removedFromWave = false;
        hasHitCore = false;
    }

    private void OnDisable()
    {
        EnemyTracker.UnregisterEnemy(this);
    }

    private void OnArrived()
    {
        if (_removedFromWave) return;
        hasHitCore = true;

        // TODO: aplicar daño al núcleo si corresponde:
        // Core.Instance?.TakeDamage(data != null ? data.coreDamage : 1);

        RemoveFromWaveOnce();   // notifica WaveManager una sola vez
        ReturnToPoolOrDisable();// NO destruir
    }

    public void ResetEnemy()
    {
        // Asegurar dependencias
        var health = GetComponent<EnemyHealth>();
        if (health == null) health = gameObject.AddComponent<EnemyHealth>();
        var healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<EnemyHealthBar>();

        // Reset de stats
        if (Data != null)
        {
            health.SetMaxHealth(Data.maxHealth);
            health.SetDefense(Data.defense);
            health.SetCurrentHealth(Data.maxHealth);
        }
        else
        {
            if (health.GetMaxHealth() <= 0f) health.SetMaxHealth(10f);
            health.SetCurrentHealth(health.GetMaxHealth());
        }

        // Inicializar barra
        healthBar.Initialize(this.transform, health.GetMaxHealth());
        healthBar.UpdateHealthBar(health.GetCurrentHealth(), health.GetMaxHealth());

        // Reset de flags/movimiento
        hasHitCore = false;
        _removedFromWave = false;
        _idx = 0;
    }

    public void OnEnemyDeath(Enemy e)
    {
        // Por contrato del handler, 'e' debería ser this, pero lo defensivizamos
        if (e != this) return;
        NotifyDeath();
    }

    public void NotifyDeath()
    {
        if (_removedFromWave) return;

        OnAnyEnemyKilled?.Invoke(this);
        RemoveFromWaveOnce();
        ReturnToPoolOrDisable(); // NO Destroy
    }

    private void RemoveFromWaveOnce()
    {
        if (_removedFromWave) return;
        _removedFromWave = true;
        WaveManager.Instance?.NotifyEnemyKilled();
    }

    private void ReturnToPoolOrDisable()
    {
        // Devolver al pool si existe; si no, como mínimo desactivar para no romper refs
        if (EnemyPool.Instance != null)
            EnemyPool.Instance.ReturnEnemy(gameObject);
        else
            gameObject.SetActive(false);
    }

}
