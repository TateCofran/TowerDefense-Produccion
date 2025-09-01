using UnityEngine;

public class TurretRangeVisualizer : MonoBehaviour, IRangeDisplay
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int segments = 60;

    private float currentRadius;

    public void Show(float radius)
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer no asignado en " + gameObject.name);
            return;
        }

        currentRadius = radius;
        UpdateCircle();
        lineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    public bool IsVisible()
    {
        return lineRenderer != null && lineRenderer.enabled;
    }

    private void UpdateCircle()
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = segments + 1;
        lineRenderer.loop = true;

        float angleStep = 360f / segments;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angle) * currentRadius;
            float z = Mathf.Sin(angle) * currentRadius;
            lineRenderer.SetPosition(i, new Vector3(x, 0.01f, z));
        }
    }
}

