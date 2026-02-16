using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Vector3 originalPosition; // Yerel pozisyonu hatýrlamak için

    private HandVisualManager visualManager;

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
            // Sýra oyuncuda deðilse sürüklemeyi baþlatma
            eventData.pointerDrag = null;
            return;
        }

        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Visual Manager'ý bul ve bu kartý "Sürükleniyor" olarak iþaretle
        visualManager = GetComponentInParent<HandVisualManager>();
        if (visualManager != null)
        {
            visualManager.StartDraggingCard(rectTransform);
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;

        // Sürüklerken diðer UI'larýn üstünde görünmesi için geçici olarak en dýþa alabilirsin
        // transform.SetParent(transform.root); // Bu satýr opsiyonel, UI yapýna göre
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (visualManager != null)
        {
            visualManager.StopDraggingCard();
        }

        // --- KRÝTÝK KONTROL ---
        // Eðer kartýn ebeveyni hala originalParent ise (yani yeni bir yere býrakýlmadýysa)
        // VEYA ebeveyni deðiþmiþ olsa bile geçerli bir slot deðilse (boþluða býrakýldýysa)

        // Basit Mantýk: OnDrop (Slot scriptleri) kartýn parent'ýný deðiþtirir.
        // Eðer parent deðiþmediyse, kart bir slota girmemiþ demektir. Geri dönmeli.

        if (transform.parent == originalParent || transform.parent == transform.root)
        {
            // Eski yerine dön
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;

            // Eðer HandVisualManager varsa o zaten pozisyonu düzeltecektir, sorun yok.
        }
        else
        {
            // Kartýn parent'ý deðiþmiþ (yani bir slota girmiþ).
            // KURAL: Slota giren kart ele dönemez.
            // Ama zaten OnDrop sadece slotlarda var, HandArea'da yok.
            // Yani oyuncu kartý slottan alýp HandArea'ya sürüklerse, HandArea'da OnDrop olmadýðý için
            // parent deðiþmeyecek ve yukarýdaki "if" bloðuna girip slota geri dönecek.
            // BU ÝSTEDÝÐÝN DAVRANIÞI OTOMATÝK SAÐLAR.
        }
    }
}