using System;
using UnityEngine;

public enum WorldState { Normal, OtherWorld }

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    public Material cellMaterialNormal;
    public Material cellMaterialOtherWorld;
    public Material pathMaterialNormal;
    public Material pathMaterialOtherWorld;

    public WorldState CurrentWorld { get; private set; } = WorldState.Normal;
    public static event Action<WorldState> OnWorldChanged;

    [SerializeField] private float shiftCooldown = 10f;
    private float lastShiftTime = -999f;


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) // o la tecla que elijas
        {
            TryShiftWorld();
        }
    }

    public void TryShiftWorld()
    {
        if (!CanShiftWorld())
        {
            Debug.Log($"[WorldManager] No se puede cambiar de mundo aún. Cooldown restante: {Mathf.Ceil(shiftCooldown - (Time.time - lastShiftTime))}s");
            return;
        }

        ShiftWorld();
    }

    private bool CanShiftWorld()
    {
        return WaveManager.Instance != null &&
               WaveManager.Instance.WaveInProgress &&
               Time.time - lastShiftTime >= shiftCooldown;
    }


    private void ShiftWorld()
    {
        CurrentWorld = (CurrentWorld == WorldState.Normal) ? WorldState.OtherWorld : WorldState.Normal;
        lastShiftTime = Time.time;

        Debug.Log($"[WorldManager] Mundo cambiado a: {CurrentWorld}");
        OnWorldChanged?.Invoke(CurrentWorld);

        CorruptionManager.Instance.ReduceCorruption(); // si querés reducir corrupción al cambiar
    }
    public void IncreaseShiftCooldown(float amount)
    {
        shiftCooldown += amount;
        Debug.Log($"[WorldManager] Nuevo shiftCooldown: {shiftCooldown} segundos.");
    }

}
