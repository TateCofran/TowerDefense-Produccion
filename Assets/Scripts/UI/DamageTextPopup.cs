using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class DamageTextPopup : MonoBehaviour
{
    [Header("Anim")]
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseDistance = 1.0f;
    [SerializeField] private AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float fadeStart = 0.4f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private bool faceCamera = true;

    private float _t;
    private TextMeshProUGUI _tmp;
    private Color _base;
    private Vector3 _start;
    private Transform _cam;

    public void Play() { _t = 0f; } // trigger externo

    private void Awake()
    {
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (_tmp == null) _tmp = GetComponent<TextMeshProUGUI>();
        if (_tmp != null) _base = _tmp.color;
    }

    private void OnEnable()
    {
        _t = 0f;
        _start = transform.position;
        if (_tmp != null) _tmp.color = _base;
        if (Camera.main != null) _cam = Camera.main.transform;
    }

    private void Update()
    {
        if (_cam == null && Camera.main != null) _cam = Camera.main.transform;

        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / lifetime);

        // Billboard hacia la cámara
        if (faceCamera && _cam != null)
            transform.forward = _cam.forward;

        // Movimiento vertical suave
        float yOff = riseCurve.Evaluate(k) * riseDistance;
        transform.position = _start + Vector3.up * yOff;

        // Fade
        if (_tmp != null)
        {
            float fadeT = (k <= fadeStart) ? 0f : Mathf.InverseLerp(fadeStart, 1f, k);
            float a = Mathf.Clamp01(fadeCurve.Evaluate(fadeT));
            var c = _tmp.color; c.a = a; _tmp.color = c;
        }

        if (_t >= lifetime) Destroy(gameObject);
    }

    // helpers opcionales
    public void SetText(string value)
    {
        if (_tmp) _tmp.text = value;
    }

    public void SetStyle(Color color, float scale = 1f)
    {
        if (_tmp) _tmp.color = color;
        transform.localScale = Vector3.one * scale;
    }
}
