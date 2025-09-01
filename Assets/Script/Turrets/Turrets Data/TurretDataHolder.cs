using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TurretStats))]
public class TurretDataHolder : MonoBehaviour
{
    [HideInInspector]
    public TurretData turretData;

    [Header("ID de torreta (se busca en el JSON)")]
    public string turretId;

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !string.IsNullOrEmpty(turretId))
        {
            TurretDatabase db = TurretDatabase.Instance;

            if (db == null)
            {
                db = UnityEditor.AssetDatabase
                    .FindAssets("t:TurretDatabase")
                    .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<TurretDatabase>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                    .FirstOrDefault();

                if (db != null)
                    TurretDatabase.Instance = db;
            }

            var data = db?.GetTurretData(turretId);
            if (data != null)
            {
                ApplyData(data);
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            else
            {
               // Debug.LogWarning($"Turret ID '{turretId}' no encontrado en la base de datos.");
            }
        }
#endif
    }




    public void ApplyData(TurretData data)
    {
        turretData = data;

        var stats = GetComponent<TurretStats>();
        stats?.InitializeFromData(data);
    }
}
