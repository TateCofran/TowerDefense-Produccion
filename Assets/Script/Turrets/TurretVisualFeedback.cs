using UnityEngine;

public class TurretVisualFeedback : MonoBehaviour
{
    [SerializeField] private Renderer turretRenderer;
    [SerializeField] private Color boostedColor = Color.cyan;
    [SerializeField] private Color normalColor = Color.black;

    private Material mat;

    void Start()
    {
        if (turretRenderer != null)
            mat = turretRenderer.material;

        UpdateVisual();
        WorldManager.OnWorldChanged += OnWorldChanged;
    }

    void OnDestroy()
    {
        WorldManager.OnWorldChanged -= OnWorldChanged;
    }

    private void OnWorldChanged(WorldState state)
    {
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (mat == null || TryGetComponent<TurretDataHolder>(out var holder) == false)
            return;

        var turretWorld = holder.turretData.GetAllowedWorld();
        var currentWorld = WorldManager.Instance.CurrentWorld;

        bool boosted = (turretWorld == AllowedWorld.Normal && currentWorld == WorldState.Normal) ||
                       (turretWorld == AllowedWorld.Other && currentWorld == WorldState.OtherWorld);

        Color emission = boosted ? new Color(0.2f, 1f, 1f) * 3f : Color.black;
        mat.SetColor("_EmissionColor", emission);
        
        DynamicGI.SetEmissive(turretRenderer, boosted ? boostedColor : normalColor);
    }
}
