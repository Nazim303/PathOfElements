using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition;

    private HandVisualManager visualManager;
    private float smoothVelocity = 0f;

    // --- YENÝ: Akýcý takip ve fare offset'i için deðiþkenler ---
    private bool isDragging = false;
    private Vector3 targetDragPosition;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null && tm.state != BattleState.PLAYERTURN)
        {
            eventData.pointerDrag = null;
            return;
        }

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        visualManager = GetComponentInParent<HandVisualManager>();
        if (visualManager != null)
        {
            visualManager.StartDraggingCard(rectTransform);
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;

        // Baþlangýç hedef noktasýný ayarla
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out targetDragPosition);

        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos))
        {
            targetDragPosition = globalMousePos; // Anlýk fare konumunu hedef olarak kaydet
        }

        // --- YANA YATMA (TILT) EFEKTÝ ---
        float targetZRotation = eventData.delta.x * -0.25f;
        targetZRotation = Mathf.Clamp(targetZRotation, -15f, 15f);

        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float newZ = Mathf.SmoothDamp(currentZ, targetZRotation, ref smoothVelocity, 0.05f);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    // --- YENÝ: YUMUÞAK TAKÝP VE FAREDEN KAÇMA (OFFSET) MANTIÐI ---
    void Update()
    {
        if (isDragging)
        {
            // Kartý farenin bir miktar yukarýsýnda tut (kart yüksekliðinin %40'ý kadar)
            // Bu sayede oyuncunun parmaðý/faresi, kartýn açýklamalarýný veya düþmaný kapatmaz.
            float yOffsetAmount = (rectTransform.rect.height * 0.4f) * transform.lossyScale.y;
            Vector3 offset = transform.up * yOffsetAmount;

            // Farenin anlýk yerine + offset'e doðru yaylanarak (Lerp) git
            transform.position = Vector3.Lerp(transform.position, targetDragPosition + offset, Time.deltaTime * 20f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Kartý býraktýðýmýzda açýsýný anýnda sýfýrla (dikleþsin)
        transform.localEulerAngles = Vector3.zero;

        if (visualManager != null)
        {
            visualManager.StopDraggingCard();
        }

        if (transform.parent == originalParent || transform.parent == transform.root)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}