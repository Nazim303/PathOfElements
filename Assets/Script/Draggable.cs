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

    // --- YENİ: Akıcı takip ve fare offset'i için değişkenler ---
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
        TurnManager tm = TurnManager.instance != null ? TurnManager.instance : FindFirstObjectByType<TurnManager>();
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

        // Başlangıç hedef noktasını ayarla
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
            targetDragPosition = globalMousePos; // Anlık fare konumunu hedef olarak kaydet
        }

        // --- YANA YATMA (TILT) EFEKTİ ---
        float targetZRotation = eventData.delta.x * -0.25f;
        targetZRotation = Mathf.Clamp(targetZRotation, -15f, 15f);

        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float newZ = Mathf.SmoothDamp(currentZ, targetZRotation, ref smoothVelocity, 0.05f);
        transform.localEulerAngles = new Vector3(0, 0, newZ);
    }

    // --- YENİ: YUMUŞAK TAKİP VE FAREDEN KAÇMA (OFFSET) MANTIĞI ---
    void Update()
    {
        if (isDragging)
        {
            // Kartı farenin bir miktar yukarısında tut (kart yüksekliğinin %40'ı kadar)
            // Bu sayede oyuncunun parmağı/faresi, kartın açıklamalarını veya düşmanı kapatmaz.
            float yOffsetAmount = (rectTransform.rect.height * 0.4f) * transform.lossyScale.y;
            Vector3 offset = transform.up * yOffsetAmount;

            // Farenin anlık yerine + offset'e doğru yaylanarak (Lerp) git
            transform.position = Vector3.Lerp(transform.position, targetDragPosition + offset, Time.deltaTime * 20f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Kartı bıraktığımızda açısını anında sıfırla (dikleşsin)
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