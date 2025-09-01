using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TurretDataHolder))]
public class TurretDataHolderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TurretDataHolder holder = (TurretDataHolder)target;

        // Obtener todos los IDs del JSON
        string[] turretIds = TurretDatabase.Instance != null
            ? TurretDatabase.Instance.allTurrets.ConvertAll(t => t.id).ToArray()
            : new string[] { "No Database" };

        int selectedIndex = Mathf.Max(0, System.Array.IndexOf(turretIds, holder.turretId));
        int newIndex = EditorGUILayout.Popup("Turret ID", selectedIndex, turretIds);

        if (newIndex != selectedIndex)
        {
            Undo.RecordObject(holder, "Cambiar turret ID");

            string selectedId = turretIds[newIndex];
            holder.turretId = selectedId;

            TurretData data = TurretDatabase.Instance.GetTurretData(selectedId);
            if (data != null)
            {
                holder.ApplyData(data);

                // También actualizamos TurretStats para que se refleje
                var stats = holder.GetComponent<TurretStats>();
                if (stats != null)
                {
                    stats.InitializeFromData(data);
                    EditorUtility.SetDirty(stats);
                }

                EditorUtility.SetDirty(holder);
            }
        }

        // También mostramos los otros campos si querés que el usuario los vea
        DrawDefaultInspector();
    }
}
