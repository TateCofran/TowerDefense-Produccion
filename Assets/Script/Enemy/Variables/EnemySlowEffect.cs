using System.Collections;
using UnityEngine;

public class EnemySlowEffect : MonoBehaviour, ISlowable
{
    private EnemyMovement movement;
    private Coroutine slowCoroutine;
    private float originalSpeed;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
    }

    public void ApplySlow(float amount, float duration)
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowRoutine(amount, duration));
    }

    private IEnumerator SlowRoutine(float amount, float duration)
    {
        originalSpeed = movement.GetSpeed();
        float slowedSpeed = originalSpeed * (1f - amount);
        movement.SetSpeed(slowedSpeed);

        yield return new WaitForSeconds(duration);

        movement.SetSpeed(originalSpeed);
        slowCoroutine = null;
    }
}
