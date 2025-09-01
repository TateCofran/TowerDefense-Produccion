using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardSelectionHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float verticalMovementAmount = 30f;
    [SerializeField] private float moveTime = 0.1f;
    [Range(0f, 2f), SerializeField] private float scaleAmount = 1.1f;

    private Vector3 startAnchoredPos;
    private Vector3 startScale;
    private RectTransform rectTransform;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        startAnchoredPos = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;
    }

    private IEnumerator AnimateScale(bool enlarge)
    {
        Vector3 targetScale = enlarge ? startScale * scaleAmount : startScale;
        Vector3 initialScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / moveTime);
            transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            yield return null;
        }
        transform.localScale = targetScale;
    }


    private void Animate(bool enlarge)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateScale(enlarge));
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        Animate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Animate(false);
    }

    public void OnSelect(BaseEventData eventData)
    {
        Animate(true);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Animate(false);
    }
}
