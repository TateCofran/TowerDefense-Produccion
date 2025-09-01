using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour, IDamageDealer
{
    [SerializeField] private EnemyData data;
    public EnemyData Data => data;
    public EnemyType Type => data != null ? data.type : EnemyType.Minion;

    public EnemyHealth Health { get; private set; }
    public EnemyMovement Movement { get; private set; }
    public EnemyWorldLogic WorldLogic { get; private set; }
    public EnemyHealthBar HealthBar { get; private set; }
    public int GetDamage() => data.damageToCore;

    private bool hasHitCore = false;

    public static event System.Action<Enemy> OnAnyEnemyKilled;

    private void Awake()
    {
        Health = GetComponent<EnemyHealth>();
        Movement = GetComponent<EnemyMovement>();
        WorldLogic = GetComponent<EnemyWorldLogic>();
        HealthBar = GetComponent<EnemyHealthBar>();

        if (data == null)
            Debug.LogError($"[Enemy] {name} no tiene asignado EnemyData");
    }
    private void Update()
    {
        if (Health.IsDead()) return;

        Movement?.Move();
    }

    public void InitializePath(Vector3[] path, GameObject coreObject, GridManager manager)
    {
        if (data == null)
            return;

        Health.Initialize(data.maxHealth, data.defense);
        Movement.Initialize(path.Reverse().ToArray(), data.moveSpeed);

        SetOriginWorld(data.originWorld);

        HealthBar?.Initialize(transform, Health.GetMaxHealth());

        //WaveManager.Instance?.RegisterEnemyManually();

    }

    public void SetOriginWorld(WorldState state)
    {
        WorldLogic?.SetOriginWorld(state);
    }

    public void OnSuccessfulHit()
    {
        if (hasHitCore) return;
        hasHitCore = true;
        Health.Die();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Core")) return;

        int damage = data != null ? data.damageToCore : 1;
        Core.Instance.TakeDamage(damage);
        Health.Die();
    }

    private void OnEnable()
    {
        EnemyTracker.RegisterEnemy(this);
    }

    private void OnDisable()
    {
        EnemyTracker.UnregisterEnemy(this);
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