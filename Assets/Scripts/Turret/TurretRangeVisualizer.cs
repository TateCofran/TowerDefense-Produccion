using UnityEngine;

public class TurretRangeVisualizer : MonoBehaviour, IRangeDisplay
{
    [Header("Refs")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Calidad del círculo")]
    [Tooltip("Longitud aproximada de cada cuerda del polígono. Radios grandes ⇒ más segmentos automáticamente.")]
    [SerializeField] private float targetChordLength = 0.15f; // menor = más segmentos
    [SerializeField] private int minSegments = 24;
    [SerializeField] private int maxSegments = 256;

    [Header("Altura del círculo")]
    [SerializeField] private float yOffset = 0.02f;

    [Header("Actualización automática")]
    [Tooltip("Si está activo, volverá a dibujar si SetRadius recibe el mismo valor (por ejemplo, tras reactivar).")]
    [SerializeField] private bool alwaysRedrawOnShow = true;

    private float _currentRadius = -1f;
    private int _currentSegments = -1;

    // NUEVO: para seguir al centro aunque no cambie el radio
    private Vector3 _lastCenter;
    private bool _visible;

    private void Awake()
    {
        if (!lineRenderer)
        {
            Debug.LogError($"[{nameof(TurretRangeVisualizer)}] LineRenderer no asignado en {name}");
            enabled = false;
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
    }

    private void LateUpdate()
    {
        if (!_visible || !lineRenderer) return;

        Vector3 center = transform.position;
        if ((center - _lastCenter).sqrMagnitude > 0.0000001f)
        {
            // si se movió el objeto, redibujo con el mismo radio/segmentos
            RedrawCircleWorldSpace();
        }
    }

    // ========== IRangeDisplay ==========
    public void Show(float radius)
    {
        if (!lineRenderer) return;
        _visible = true;
        SetRadius(radius, forceRedraw: alwaysRedrawOnShow);
        lineRenderer.enabled = true;
    }

    public void Hide()
    {
        _visible = false;
        if (lineRenderer) lineRenderer.enabled = false;
    }

    public bool IsVisible()
    {
        return lineRenderer && lineRenderer.enabled;
    }

    /// <summary> Llamá esto cuando cambie el rango de la torreta. </summary>
    public void SetRadius(float radius, bool forceRedraw = false)
    {
        radius = Mathf.Max(0f, radius);

        int desiredSegments = ComputeSegments(radius);
        bool radiusChanged = !Mathf.Approximately(_currentRadius, radius);
        bool segsChanged = desiredSegments != _currentSegments;

        if (!forceRedraw && !radiusChanged && !segsChanged) return;

        _currentRadius = radius;
        _currentSegments = desiredSegments;

        RedrawCircleWorldSpace();
    }

    private int ComputeSegments(float radius)
    {
        float circumference = Mathf.PI * 2f * Mathf.Max(0.01f, radius);
        int segments = Mathf.CeilToInt(circumference / Mathf.Max(0.01f, targetChordLength));
        return Mathf.Clamp(segments, minSegments, maxSegments);
    }

    private void RedrawCircleWorldSpace()
    {
        if (!lineRenderer || _currentSegments < 3) return;

        lineRenderer.positionCount = _currentSegments + 1;

        Vector3 center = transform.position;
        _lastCenter = center; // <- NUEVO: actualizamos el cache del centro

        float angleStep = 360f / _currentSegments;
        for (int i = 0; i <= _currentSegments; i++)
        {
            float angleRad = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleRad) * _currentRadius;
            float z = Mathf.Sin(angleRad) * _currentRadius;
            lineRenderer.SetPosition(i, new Vector3(center.x + x, center.y + yOffset, center.z + z));
        }
    }

    // Helpers opcionales
    public void SetColor(Color color)
    {
        if (!lineRenderer) return;
        if (lineRenderer.colorGradient != null)
        {
            var g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
            );
            lineRenderer.colorGradient = g;
        }
        else
        {
            lineRenderer.startColor = lineRenderer.endColor = color;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minSegments < 3) minSegments = 3;
        if (maxSegments < minSegments) maxSegments = minSegments;
        if (targetChordLength <= 0f) targetChordLength = 0.01f;

        if (_currentRadius > 0f && lineRenderer)
        {
            SetRadius(_currentRadius, forceRedraw: true);
        }
    }
#endif
}
