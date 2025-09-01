using UnityEngine;

public class PathVisual : MonoBehaviour
{
    private Renderer rend;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        WorldManager.OnWorldChanged += UpdateColor;
        UpdateColor(WorldManager.Instance.CurrentWorld);
    }

    private void OnDestroy()
    {
        WorldManager.OnWorldChanged -= UpdateColor;
    }

    private void UpdateColor(WorldState state)
    {
        rend.material = (state == WorldState.OtherWorld) ?
            WorldManager.Instance.pathMaterialOtherWorld :
            WorldManager.Instance.pathMaterialNormal;
    }
}
