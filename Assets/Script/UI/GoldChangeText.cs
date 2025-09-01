using UnityEngine;
using TMPro;

public class GoldChangeText : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 40f;
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] private TextMeshProUGUI goldText;

    private Color startColor;
    private float timer = 0f;
    private Vector3 startPos;

    public void Setup(int amount)
    {
        // Setea el texto y color
        goldText.text = (amount > 0 ? "+" : "") + amount;
        goldText.color = amount < 0 ? Color.red : Color.green; // <--- Rojo si resta, verde si suma
        startColor = goldText.color;
        timer = 0f;
        startPos = transform.localPosition;
        gameObject.SetActive(true);
    }

    void Update()
    {
        transform.localPosition = startPos + Vector3.up * floatSpeed * (timer / fadeDuration);
        timer += Time.deltaTime;

        float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
        goldText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

        if (timer >= fadeDuration)
            Destroy(gameObject); // O devolvelo al pool si usás pooling
    }
}
