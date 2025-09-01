using System.Collections.Generic;
using UnityEngine;

public class TurretDatabase : MonoBehaviour
{
    public static TurretDatabase Instance;

    [Header("Turret Json")]
    public TextAsset turretJson;

    [Header("All Turrets")]
    public List<TurretData> allTurrets = new();

    private Dictionary<string, TurretData> turretDict = new();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    public static void ForceInitInEditor()
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<TurretDatabase>();
            if (Instance != null)
            {
                Instance.LoadData();
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadData(); // MUY IMPORTANTE
        }
    }


#if UNITY_EDITOR
    void OnValidate()
    {
        if (turretJson != null && !Application.isPlaying)
        {
            LoadData();
            UnityEditor.EditorUtility.SetDirty(this); // Forza que Unity lo registre en el editor
        }
    }
#endif

    public void LoadData()
    {
        if (turretJson == null)
        {
            Debug.LogError("Turret JSON no asignado.");
            return;
        }

        var list = JsonUtility.FromJson<TurretDataList>(turretJson.text);

        if (list == null || list.turrets == null)
        {
            Debug.LogError("Error al parsear el archivo JSON.");
            return;
        }

        allTurrets = new List<TurretData>(list.turrets);

        turretDict.Clear();
        foreach (var data in allTurrets)
        {
            if (!string.IsNullOrEmpty(data.id))
            {
                turretDict[data.id] = data;
            }
            else
            {
                Debug.LogWarning("Una torreta no tiene ID definido.");
            }
        }
    }


    public TurretData GetTurretData(string id)
    {
        if (turretDict.TryGetValue(id, out TurretData data))
        {
            return data;
        }

        Debug.LogWarning($"[TurretDatabase] No se encontró la torreta con ID: {id}");
        return null;
    }

}
