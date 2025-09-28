using UnityEngine;
using System;

[DisallowMultipleComponent]
public class PathEffectManager : MonoBehaviour
{
    public static PathEffectManager Instance { get; private set; }

    [Header("Master")]
    public bool enableAll = true;

    // ===== Da�o fijo =====
    [Header("Da�o fijo (golpe)")]
    public bool enableDamage = true;
    [Min(0)] public int damagePerHit = 1;   // escala global del da�o por golpe
    [Min(0.05f)] public float damageCooldown = 10f;  // cooldown global POR ENEMIGO para el da�o

    // ===== Slow =====
    [Header("Slow (reducci�n de velocidad)")]
    public bool enableSlow = true;
    [Tooltip("Multiplica la fuerza de slow definida por el tile (0..1). 1 = igual al tile.")]
    [Range(0, 1)] public float slowMultiplier = 1f;    // controla CU�NTO se reduce la velocidad
    [Range(0, 1)] public float slowMax = 0.95f;        // no m�s de 95% de reducci�n
    [Tooltip("Intervalo opcional de refresco para slow (si 0 usa el interval del PathCellEffect).")]
    [Min(0)] public float globalApplyIntervalOverride = 0f; // NO es cooldown; solo refresco de estado

    // ===== Stun =====
    [Header("Stun (aturdimiento)")]
    public bool enableStun = true;
    [Min(0)] public float stunMultiplier = 1f;        // escala global de duraci�n de stun
    [Min(0)] public float stunMax = 10f;              // tope de duraci�n
    [Min(0.05f)] public float stunCooldown = 0.5f;    // cooldown global POR ENEMIGO para re-aplicar stun

    public event Action OnValuesChanged;

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

#if UNITY_EDITOR
    private void OnValidate() { OnValuesChanged?.Invoke(); }
#endif

    /// <summary>
    /// Aplica switches, multiplicadores y caps.
    /// Devuelve adem�s los cooldowns espec�ficos de da�o y stun.
    /// </summary>
    public void GetEffective(
        int inDamagePerHit, float inSlow, float inStun,
        out int outDamagePerHit, out float outSlow, out float outStun,
        out float outDamageCd, out float outStunCd)
    {
        if (!enableAll)
        {
            outDamagePerHit = 0;
            outSlow = 0;
            outStun = 0;
            outDamageCd = Mathf.Max(0.05f, damageCooldown);
            outStunCd = Mathf.Max(0.05f, stunCooldown);
            return;
        }

        // Da�o fijo
        if (enableDamage)
            outDamagePerHit = inDamagePerHit;
        else
            outDamagePerHit = 0;

        // Slow fijo
        if (enableSlow)
            outSlow = inSlow;
        else
            outSlow = 0;

        // Stun fijo
        if (enableStun)
            outStun = inStun;
        else
            outStun = 0;

        // Cooldowns fijos por efecto
        outDamageCd = Mathf.Max(0.05f, damageCooldown);
        outStunCd = Mathf.Max(0.05f, stunCooldown);
    }

    public float GetApplyIntervalOverride() => globalApplyIntervalOverride;

    // Helpers para UI (opcionales)
    public void ToggleAll(bool on) { enableAll = on; OnValuesChanged?.Invoke(); }
    public void ToggleDamage(bool on) { enableDamage = on; OnValuesChanged?.Invoke(); }
    public void ToggleSlow(bool on) { enableSlow = on; OnValuesChanged?.Invoke(); }
    public void ToggleStun(bool on) { enableStun = on; OnValuesChanged?.Invoke(); }
}
