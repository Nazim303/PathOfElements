using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using System.Collections;       

public class DeckSelectionCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData myCard;
    private StartingDeckManager manager;
    private bool isSelected = false;

    private Coroutine hoverCoroutine;
    private Vector3 originalScale = Vector3.one;
    
    private RectTransform rectTransform;
    private Vector2 originalPos;
    private bool hasOriginalPos = false;
    
    private Canvas overrideCanvas; 

    public void Setup(CardData card, StartingDeckManager mgr, bool selected)
    {
        myCard = card;
        manager = mgr;
        isSelected = selected;

        rectTransform = GetComponent<RectTransform>();

        CardDisplay display = GetComponent<CardDisplay>();
        if (display != null) display.LoadCard(card);

        Draggable drag = GetComponent<Draggable>();
        if (drag != null) drag.enabled = false;

        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);

        overrideCanvas = GetComponent<Canvas>();
        if (overrideCanvas == null) overrideCanvas = gameObject.AddComponent<Canvas>();
        overrideCanvas.overrideSorting = true;
        overrideCanvas.sortingOrder = 1; 

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
    }

    void OnClick()
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayClick(); 

        if (isSelected) manager.RemoveFromDeck(this);
        else manager.AddToDeck(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {

        // Orijinal pozisyonu sadece ilk fare giriþinde kaydet
        if (!hasOriginalPos) 
        { 
            originalPos = rectTransform.anchoredPosition; 
            hasOriginalPos = true; 
        }

        if (overrideCanvas != null) overrideCanvas.sortingOrder = 100; 

        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        // Hem %35 büyüt, hem de 40 birim yukarý kaldýr!
        hoverCoroutine = StartCoroutine(HoverRoutine(new Vector3(1.35f, 1.35f, 1.35f), originalPos + new Vector2(0, 40f))); 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (overrideCanvas != null) overrideCanvas.sortingOrder = 1; 

        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        // Orijinal boyut ve pozisyona dön
        hoverCoroutine = StartCoroutine(HoverRoutine(originalScale, originalPos)); 
    }

    private IEnumerator HoverRoutine(Vector3 targetScale, Vector2 targetPos)
    {
        float speed = 15f;
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f || Vector2.Distance(rectTransform.anchoredPosition, targetPos) > 0.1f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPos, Time.deltaTime * speed);
            yield return null;
        }
        transform.localScale = targetScale;
        rectTransform.anchoredPosition = targetPos;
    }
}