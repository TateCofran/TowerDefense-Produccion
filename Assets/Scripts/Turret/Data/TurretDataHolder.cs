using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretDataHolder : MonoBehaviour
{
    [Header("SO con los datos de la torreta")]
    public TurretDataSO turretDataSO;

    private TurretStats stats;

    void Awake()
    {
        stats = GetComponent<TurretStats>();

        if (turretDataSO != null)
            ApplyDataSO(turretDataSO);
        else
            Debug.LogWarning($"[TurretDataHolder] No se asignó un TurretDataSO en {gameObject.name}");
    }

    /// <summary>
    /// Inicializa la torreta con los valores del ScriptableObject.
    /// </summary>
    public void ApplyDataSO(TurretDataSO data)
    {
        if (data == null)
        {
            Debug.LogError("[TurretDataHolder] ApplyDataSO recibió un null.");
            return;
        }

        turretDataSO = data;
        stats.InitializeFromSO(data);
    }
}
