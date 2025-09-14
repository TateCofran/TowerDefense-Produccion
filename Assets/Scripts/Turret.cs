using System.Linq;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float range = 5f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float fireRate = 1f;

    private float fireCountdown = 0f;
    private Transform target;

    private void Update()
    {
        FindTarget();

        if (target != null)
        {
            if (fireCountdown <= 0f)
            {
                Shoot();
                fireCountdown = 1f / fireRate;
            }

            fireCountdown -= Time.deltaTime;
        }
    }

    private void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            target = null;
            return;
        }

        float halfAngle = 45f;
        var enemiesInCone = enemies.Where(e =>
        {
            Vector3 dirToEnemy = e.transform.position - transform.position;
            float distance = dirToEnemy.magnitude;

            if (distance > range)
                return false;

            //si está en el cono frontal
            float angleToEnemy = Vector3.Angle(transform.forward, dirToEnemy);
            return angleToEnemy <= halfAngle;
        }).ToList(); //lista de enemigos que estan dentro del rango

        if (enemiesInCone.Count == 0) //si no hay ningun enemigo en rango salgo
        {
            target = null;
            return;
        }

        //elijo el enemigo a atacar según el modo de ataque global
        switch (AttackModeManager.Instance.currentAttackMode)
        {
            case AttackMode.Farthest:
                target = enemiesInCone
                    .OrderByDescending(e => Vector3.Distance(transform.position, e.transform.position))
                    .First().transform;
                break;

            case AttackMode.Nearest:
                target = enemiesInCone
                    .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                    .First().transform;
                break;

            case AttackMode.LowestHealth:
                target = enemiesInCone
                    .OrderBy(e => e.GetComponent<Enemy>().currentHealth)
                    .First().transform;
                break;

            case AttackMode.HighestHealth:
                target = enemiesInCone
                    .OrderByDescending(e => e.GetComponent<Enemy>().currentHealth)
                    .First().transform;
                break;
        }

        if (target != null)
        {
            Debug.Log($" Objetivo: {target.name}, Vida: {target.GetComponent<Enemy>().currentHealth}, Modo: {AttackModeManager.Instance.currentAttackMode}");
        }
    }


    private void Shoot()
    {
        Enemy health = target.GetComponent<Enemy>(); //en el script del enemigo busco su vida
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }


    //opcional si quiero ver el rango de la torreta sino sacar
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        float angle = 90f; //ángulo del rango hacia adelante
        int segments = 20;

        Vector3 forward = transform.forward;
        Vector3 lastPoint = transform.position + Quaternion.Euler(0, -angle / 2, 0) * forward * range;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -angle / 2 + (angle * i / segments);
            Vector3 nextPoint = transform.position + Quaternion.Euler(0, currentAngle, 0) * forward * range;
            Gizmos.DrawLine(transform.position, nextPoint);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }
}
