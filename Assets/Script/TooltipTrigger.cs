using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea] public string overrideText; // Manuel yazı (Varsa bunu gösterir)

    private float hoverDelay = 0.7f;
    private Coroutine hoverCoroutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCoroutine = StartCoroutine(ShowAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverCoroutine != null) StopCoroutine(hoverCoroutine);
        if (TooltipUI.instance != null) TooltipUI.instance.HideTooltip();
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(hoverDelay);

        // 1. Önce manuel yazı var mı bak
        string textToShow = overrideText;

        // 2. Yoksa, bu objenin bir KART olup olmadığına bak ve veriyi çek
        if (string.IsNullOrEmpty(textToShow))
        {
            CardDisplay card = GetComponent<CardDisplay>();
            if (card != null && card.cardData != null)
            {
                textToShow = card.cardData.description;
            }
        }

        // 3. Hala boşsa, belki bu bir DÜŞMANDIR? (EnemyDisplay'den gelen veri overrideText'e yazılır)

        // Sonuçta bir yazı varsa göster
        if (!string.IsNullOrEmpty(textToShow) && TooltipUI.instance != null)
        {
            TooltipUI.instance.ShowTooltip(textToShow);
        }
    }

    // Dışarıdan yazı atamak için (Örn: Düşmanlar için)
    public void SetTooltipText(string text)
    {
        overrideText = text;
    }

    void OnDisable()
    {
        if (TooltipUI.instance != null) TooltipUI.instance.HideTooltip();
    }
}