using UnityEngine;

public class GoldChangeTextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldChangeTextPrefab; // Prefab con GoldChangeText
    [SerializeField] private RectTransform parentTransform;   // Por lo general, el panel donde aparece el oro

    public static GoldChangeTextSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }
    public void ShowGoldChange(int amount)
    {
        GameObject textObj = Instantiate(goldChangeTextPrefab, parentTransform);
        textObj.transform.localPosition = Vector3.zero; // posición fija, junto al icono
        GoldChangeText goldChangeText = textObj.GetComponent<GoldChangeText>();
        goldChangeText.Setup(amount);
    }
}
