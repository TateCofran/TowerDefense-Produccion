using UnityEngine;
using UnityEngine.Events;

public class WorldSwitchPoints : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Arrastra el ShiftingWorldMechanic de la escena.")]
    [SerializeField] private ShiftingWorldMechanic mechanic;

    [Header("Esencias otorgadas al QUEDAR en cada mundo")]
    [Tooltip("Al cambiar y quedar en el mundo Normal esencias AZULES ganadas.")]
    [SerializeField] private int essencesOnNormal = 1;
    [Tooltip("Al cambiar y quedar en el Otro Mundo esencias ROJAS ganadas.")]
    [SerializeField] private int essencesOnOther = 1;

    [Header("Totales")]
    [SerializeField] private int totalBlueEssences = 0; // Normal
    [SerializeField] private int totalRedEssences = 0; // Other

    [System.Serializable] public class EssenceEvent : UnityEvent<int, int> { }
    public EssenceEvent OnBlueEssenceGained;
    public EssenceEvent OnRedEssenceGained;

    private ShiftingWorldMechanic.World _lastWorld;
    private bool _init;

    public int TotalBlue => totalBlueEssences;
    public int TotalRed => totalRedEssences;

    private void Reset()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void Awake()
    {
        if (mechanic == null) mechanic = FindFirstObjectByType<ShiftingWorldMechanic>();
    }

    private void Start()
    {
        if (mechanic == null)
        {
            Debug.LogWarning("[WorldSwitchEssences] No se encontro ShiftingWorldMechanic en la escena.");
            enabled = false;
            return;
        }

        _lastWorld = mechanic.GetCurrentWorld(); // necesita el getter del mundo actual
        _init = true;
    }

    private void Update()
    {
        if (!_init || mechanic == null) return;

        var current = mechanic.GetCurrentWorld();
        if (current != _lastWorld)
        {
            // Cambio detectado  sumar según mundo DESTINO
            if (current == ShiftingWorldMechanic.World.Normal)
            {
                totalBlueEssences += essencesOnNormal;
                OnBlueEssenceGained?.Invoke(essencesOnNormal, totalBlueEssences);
                // Debug.Log($"+{essencesOnNormal} esencia(s) AZUL. Total: {totalBlueEssences}");
            }
            else
            {
                totalRedEssences += essencesOnOther;
                OnRedEssenceGained?.Invoke(essencesOnOther, totalRedEssences);
                // Debug.Log($"+{essencesOnOther} esencia(s) ROJA. Total: {totalRedEssences}");
            }

            _lastWorld = current;
        }
    }

    // APIs públicas por si querés sumar esencias desde otros sistemas:
    public void AddBlueEssence(int amount)
    {
        if (amount <= 0) return;
        totalBlueEssences += amount;
        OnBlueEssenceGained?.Invoke(amount, totalBlueEssences);
    }

    public void AddRedEssence(int amount)
    {
        if (amount <= 0) return;
        totalRedEssences += amount;
        OnRedEssenceGained?.Invoke(amount, totalRedEssences);
    }
}
