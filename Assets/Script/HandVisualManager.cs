using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HandVisualManager : MonoBehaviour
{
    [Header("Settings")]
    public float cardSpacing = 150f;
    public float arcHeight = 20f;
    public float rotationAmount = 5f;
    public float hoverScale = 1.5f;
    public float hoverYOffset = 50f;
    public float animSpeed = 10f;

    private List<RectTransform> cards = new List<RectTransform>();
    private int hoveredIndex = -1;

    // --- YENÝ: Sürüklenen Kart ---
    private RectTransform draggingCard = null;

    void Update()
    {
        UpdateCardList();
        UpdateCardPositions();
    }

    void UpdateCardList()
    {
        cards.Clear();
        foreach (Transform child in transform)
        {
            CanvasGroup cg = child.GetComponent<CanvasGroup>();
            // Sürüklenen kartý listeye EKLEME (Böylece pozisyonu hesaplanmaz)
            // Ama listeden çýkarýrsak diðer kartlarýn arasýndaki boþluk kapanýr.
            // Slay the Spire gibi boþluk kalsýn istiyorsan listeye ekle ama pozisyonunu deðiþtirme.
            // Biz basit yöntemle gidelim: Listeye ekleyelim ama UpdateCardPositions'da kontrol edelim.

            if (child.gameObject.activeSelf && (cg == null || cg.alpha > 0.1f))
            {
                cards.Add(child.GetComponent<RectTransform>());
            }
        }
    }

    void UpdateCardPositions()
    {
        int count = cards.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            RectTransform card = cards[i];

            // --- YENÝ: Eðer bu kart sürükleniyorsa pozisyonuna dokunma! ---
            if (card == draggingCard) continue;
            // --------------------------------------------------------------

            // 1. Hedef Pozisyon ve Açý
            float xPos = startX + (i * cardSpacing);
            float normalizedX = (count > 1) ? (float)i / (count - 1) * 2f - 1f : 0f;
            float yPos = Mathf.Abs(normalizedX) * -arcHeight;
            float targetRotation = -normalizedX * rotationAmount;
           

            float baseScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.cardInHandScale : 1f;
            float targetScale = baseScale;

            // 2. HOVER (Sürüklenen kart varsa Hover çalýþmasýn veya kýsýtlý çalýþsýn)
            if (hoveredIndex != -1 && draggingCard == null)
            {
                float dist = Mathf.Abs(i - hoveredIndex);
                if (i == hoveredIndex)
                {
                    targetScale = hoverScale;
                    yPos += hoverYOffset;
                    targetRotation = 0f;
                }
                else
                {
                    float pushDir = (i < hoveredIndex) ? -1f : 1f;
                    float pushAmount = (1f / (dist + 0.1f)) * 50f;
                    xPos += pushDir * pushAmount;
                }
            }

            // 3. Lerp
            Vector2 targetPos = new Vector2(xPos, yPos);
            card.anchoredPosition = Vector2.Lerp(card.anchoredPosition, targetPos, Time.deltaTime * animSpeed);

            // Eðer sürüklenen kart DEÐÝLSE scale ve rotasyonu deðiþtir
            card.localScale = Vector3.Lerp(card.localScale, Vector3.one * targetScale, Time.deltaTime * animSpeed);

            Quaternion targetRot = Quaternion.Euler(0, 0, targetRotation);
            card.localRotation = Quaternion.Lerp(card.localRotation, targetRot, Time.deltaTime * animSpeed);
        }
    }

    // --- YENÝ FONKSÝYONLAR ---
    public void StartDraggingCard(RectTransform card)
    {
        draggingCard = card;

        // Kartý en öne çizdir
        card.SetAsLastSibling();
        Canvas c = card.GetComponent<Canvas>();
        if (c == null) c = card.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 250; // Tooltip 300'dü, sürüklenen kart 250 olsun
    }

    public void StopDraggingCard()
    {
        if (draggingCard != null)
        {
            Canvas c = draggingCard.GetComponent<Canvas>();
            if (c != null)
            {
                c.overrideSorting = false;
                c.sortingOrder = 0;
            }
        }
        draggingCard = null;
        hoveredIndex = -1;
    }

    public void OnCardEnter(RectTransform card)
    {
        if (draggingCard != null) return; // Sürükleme varken hover'ý engelle

        hoveredIndex = cards.IndexOf(card);

        Canvas c = card.GetComponent<Canvas>();
        if (c == null) c = card.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 100;
        if (card.GetComponent<GraphicRaycaster>() == null) card.gameObject.AddComponent<GraphicRaycaster>();
    }

    public void OnCardExit(RectTransform card)
    {
        // YENÝ: Eðer fare üstünden çýksa bile bu kart SÜRÜKLENEN KART ise iþlemleri iptal et! (Arkaya düþmesin)
        if (draggingCard == card) return;

        hoveredIndex = -1;
        Canvas c = card.GetComponent<Canvas>();
        if (c != null)
        {
            c.overrideSorting = false;
            c.sortingOrder = 0;
        }
    }
}