using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyEffects : MonoBehaviour, IPathAffectable
{
    [SerializeField] private EnemyHealth health;

    public bool IsStunned => _stunUntil > Time.time;
    public float CurrentSpeedMultiplier => IsStunned ? 0f : Mathf.Clamp01(1f - _currentSlow);

    private float _currentSlow, _slowUntil, _stunUntil;
    private Coroutine _dotCo;

    private void Awake()
    {
        if (!health) health = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (Time.time > _slowUntil) _currentSlow = 0f;
    }

    // ----- NUEVO -----
    public void ApplyInstantDamage(int amount)
    {
        if (amount <= 0 || health == null) return;
        health.TakeDamage(amount);
    }

    // ----- Legacy (podés no usarlos) -----
    public void ApplyDoT(float dps, float duration)
    {
        if (dps <= 0f || duration <= 0f || health == null) return;
        if (_dotCo != null) StopCoroutine(_dotCo);
        _dotCo = StartCoroutine(DotRoutine(dps, duration));
    }
    public void ApplySlow(float slowPercent, float duration)
    {
        if (slowPercent <= 0f || duration <= 0f) return;
        _currentSlow = Mathf.Max(_currentSlow, Mathf.Clamp01(slowPercent));
        _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
    }
    public void ApplyStun(float stunSeconds)
    {
        if (stunSeconds <= 0f) return;
        _stunUntil = Mathf.Max(_stunUntil, Time.time + stunSeconds);
    }

    private IEnumerator DotRoutine(float dps, float duration)
    {
        float end = Time.time + duration, last = Time.time;
        while (Time.time < end)
        {
            float now = Time.time, dt = now - last; last = now;
            int dmg = Mathf.CeilToInt(dps * dt);
            if (dmg > 0 && health != null) health.TakeDamage(dmg);
            yield return null;
        }
        _dotCo = null;
    }
}
