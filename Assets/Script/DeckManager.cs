using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    [Header("Deck Config")]
    public List<CardData> allCardsInDeck = new List<CardData>();
    public int maxHandSize = 7;

    [Header("Battle State")]
    public List<CardData> drawPile = new List<CardData>();
    public List<CardData> discardPile = new List<CardData>();

    [Header("References")]
    public Transform handArea;
    public GameObject cardPrefab;
    public GameObject supportCardPrefab;
    [Header("Visuals")]
    public Transform deckDrawPoint;
    public Transform discardPoint; // --- YENÝ: Ýskarta destesinin ekrandaki konumu ---

    private Canvas mainCanvas;

    public static DeckManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (handArea != null)
        {
            mainCanvas = handArea.GetComponentInParent<Canvas>();
        }
    }

    public void InitializeBattle()
    {
        drawPile.Clear();
        discardPile.Clear();
        // DiscardHand(); // Bunu kapalý tutuyoruz (El koruma için)

        List<CardData> tempMasterDeck = new List<CardData>(allCardsInDeck);
        List<CardData> cardsInHand = new List<CardData>();

        // Eldeki kartlarý koru
        foreach (Transform child in handArea)
        {
            CardDisplay display = child.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null) cardsInHand.Add(display.cardData);
        }

        // Eldekileri desteden düþ
        foreach (CardData handCard in cardsInHand)
        {
            if (tempMasterDeck.Contains(handCard)) tempMasterDeck.Remove(handCard);
        }

        drawPile = tempMasterDeck;

        // 1. Desteyi Karýþtýr (Normal prosedür)
        ShuffleDrawPile();

        // 2. --- YENÝ: YOLDAÞ KARTI KONTROLÜ ---
        // Karýþtýrma bitti ama Yoldaþ kartý her zaman en üstte (ilk çekilecek) olmalý.
        // Desteyi tara, Companion bulursan en baþa taþý.
        CardData companionCard = null;

        // Tersten veya düzden arayabiliriz, ilk bulduðumuzu alalým
        foreach (CardData c in drawPile)
        {
            if (c.cardType == CardType.Companion)
            {
                companionCard = c;
                break;
            }
        }

        if (companionCard != null)
        {
            drawPile.Remove(companionCard); // Aradan çýkar
            drawPile.Insert(0, companionCard); // En tepeye koy
            Debug.Log($"Savaþ Baþladý: {companionCard.cardName} (Yoldaþ) destenin en tepesine taþýndý!");
        }
        // -------------------------------------

        // Önizlemeyi güncelle (Artýk en baþta Yoldaþ görünecek)
       

        if (allCardsInDeck.Count == 0) Debug.LogError("HATA: Ana Deste Boþ!");
    }

    public void ShuffleDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardData temp = drawPile[i];
            int randomIndex = Random.Range(i, drawPile.Count);
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    public void DrawCard(int amount, GameObject ignoreObject = null)
    {
        StartCoroutine(DrawCardRoutine(amount, ignoreObject));
    }

    IEnumerator DrawCardRoutine(int amount, GameObject ignoreObject)
    {
        yield return new WaitForEndOfFrame();

        int currentHandSize = handArea.childCount;
        if (ignoreObject != null) currentHandSize--;

        int spaceInHand = maxHandSize - currentHandSize;
        int cardsToDraw = Mathf.Min(amount, spaceInHand);

        if (cardsToDraw <= 0) yield break;

        for (int i = 0; i < cardsToDraw; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count > 0) ReshuffleDiscard();
                else break;
            }

            CardData cardToDraw = drawPile[0];
            drawPile.RemoveAt(0);

            GameObject prefabToUse = cardPrefab; // Varsayýlan normal kart

            if (cardToDraw.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab; // Destek kartýysa özel prefabý kullan
            }


            if (prefabToUse != null)
            {
                // 1. GERÇEK KART (HandArea içinde)
                GameObject realCard = Instantiate(prefabToUse, handArea);

                // --- DÜZELTME: Prefabýn orijinal scale'ini koru ---
                // Layout Group bazen scale'i bozabilir, emin olmak için prefab scale'i tekrar atayabilirsin ama
                // genelde Instantiate sonrasý dokunmazsak doðru kalýr.
                // realCard.transform.localScale = cardPrefab.transform.localScale; 

                CardDisplay realDisplay = realCard.GetComponent<CardDisplay>();
                if (realDisplay != null) realDisplay.LoadCard(cardToDraw);

                CanvasGroup realCG = realCard.GetComponent<CanvasGroup>();
                if (realCG == null) realCG = realCard.AddComponent<CanvasGroup>();
                realCG.alpha = 0f;

              
                yield return new WaitForEndOfFrame();

                // 2. UÇAN KART
                GameObject flyingCard = Instantiate(prefabToUse, mainCanvas.transform);

                RectTransform flyRect = flyingCard.GetComponent<RectTransform>();
                flyRect.anchorMin = new Vector2(0.5f, 0.5f);
                flyRect.anchorMax = new Vector2(0.5f, 0.5f);
                flyRect.pivot = new Vector2(0.5f, 0.5f);

                if (deckDrawPoint != null) flyingCard.transform.position = deckDrawPoint.position;
                CanvasGroup flyCG = flyingCard.GetComponent<CanvasGroup>();
                if (flyCG == null) flyCG = flyingCard.AddComponent<CanvasGroup>();
                flyCG.blocksRaycasts = false;

                // Küçük baþla
                flyingCard.transform.localScale = Vector3.zero;
                flyingCard.transform.rotation = Quaternion.Euler(0, 0, 90f);

                Destroy(flyingCard.GetComponent<Draggable>());

                flyingCard.GetComponent<CardDisplay>().LoadCard(cardToDraw);
                flyingCard.transform.SetAsLastSibling();

                StartCoroutine(AnimateFlyToHand(flyingCard, realCard, realCG));
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator AnimateFlyToHand(GameObject flyingCard, GameObject targetCard, CanvasGroup targetCG)
    {
        float duration = 0.5f;
        float t = 0;

        Vector3 startPos = flyingCard.transform.position;
        Quaternion startRot = flyingCard.transform.rotation;

        Vector3 startScale = Vector3.zero; // Baþlangýç (Yoktan var olma)

        // --- DÜZELTME BURADA ---
        // Hedef boyut (1,1,1) deðil, gerçek kartýn o anki boyutu olmalý!
        // targetCard handArea içinde olduðu için doðru boyuttadýr.
        Vector3 targetScale = targetCard.transform.localScale;

        while (t < 1)
        {
            t += Time.deltaTime / duration;

            if (targetCard == null) break;

            Vector3 targetPos = targetCard.transform.position;
            Quaternion targetRot = Quaternion.identity;

            float smoothT = t * t * (3f - 2f * t);

            flyingCard.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            flyingCard.transform.rotation = Quaternion.Lerp(startRot, targetRot, smoothT);

            // Hedef boyuta (targetScale) doðru büyü
            flyingCard.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            yield return null;
        }

        if (targetCard != null)
        {
            targetCG.alpha = 1f;
            targetCG.blocksRaycasts = true; // YENÝ: Uçuþ bitti, artýk mouse ile tutulabilir!
        }

        Destroy(flyingCard);
    }

    void ReshuffleDiscard()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDrawPile();
    }

    public void AddToDiscard(CardData card)
    {
        if (card.cardType != CardType.Companion) discardPile.Add(card);
    }

    // --- YENÝ FONKSÝYON ---
    // Companion Event'ten veya baþka bir yerden kart kazanýldýðýnda çaðrýlýr
    public void AddCardFromEvent(CardData newCard)
    {
        if (newCard.cardType == CardType.Companion)
        {
            // >>> SENARYO 1: COMPANION KART <<<
            // Kural: Destede en fazla 1 tane olabilir. Eskisini sil.
            for (int i = allCardsInDeck.Count - 1; i >= 0; i--)
            {
                if (allCardsInDeck[i].cardType == CardType.Companion)
                    allCardsInDeck.RemoveAt(i);
            }

            // Ana desteye ekle (Kayýt için)
            allCardsInDeck.Add(newCard);

            // Ve hemen kullanýlabilmesi için Çekme Destesinin EN TEPESÝNE koy!
            drawPile.Insert(0, newCard);

            Debug.Log($"Yoldaþ {newCard.cardName} alýndý ve destenin en tepesine koyuldu!");

          
        }
        else
        {
            // >>> SENARYO 2: NORMAL KART (Unlock) <<<
            // Kural: Bu kart artýk destenin kalýcý bir üyesi olur.
            // Sýnýrsýz sayýda eklenebilir (Ayný karttan 2-3 tane olabilir).

            allCardsInDeck.Add(newCard);

            // Savaþýn ortasýnda olduðumuz için bu kartý hemen çekme destesine de ekleyelim mi?
            // Genelde "Shuffle" edilerek araya karýþýr.
            drawPile.Add(newCard);
            ShuffleDrawPile(); // Karýþtýr ki rastgele bir yere gitsin

            Debug.Log($"Kart {newCard.cardName} kilidi açýldý ve desteye eklendi!");
        }
    }
    public void RemoveCardFromDeck(CardData cardToRemove)
    {
        if (allCardsInDeck.Contains(cardToRemove)) allCardsInDeck.Remove(cardToRemove);
        for (int i = allCardsInDeck.Count - 1; i >= 0; i--)
        {
            if (allCardsInDeck[i].cardName == cardToRemove.cardName) allCardsInDeck.RemoveAt(i);
        }
        if (drawPile.Contains(cardToRemove)) drawPile.Remove(cardToRemove);
        if (discardPile.Contains(cardToRemove)) discardPile.Remove(cardToRemove);
    }

    public void DiscardHand()
    {
        foreach (Transform child in handArea) Destroy(child.gameObject);
    }

    public void DiscardHandAndDrawNew(int drawAmount, GameObject ignoreCard = null)
    {
        foreach (Transform child in handArea)
        {
            if (ignoreCard != null && child.gameObject == ignoreCard) continue;

            CardDisplay display = child.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null)
            {
                if (display.cardData.cardType != CardType.Companion)
                {
                    discardPile.Add(display.cardData);
                }
            }
            Destroy(child.gameObject);
        }
        DrawCard(drawAmount);
    }

    // --- YENÝ: Kartý iskartaya uçuran ortak fonksiyon ---
    public IEnumerator AnimateCardToDiscard(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        // Kartýn UI'da her þeyin üstünde görünmesi için Canvas'a taþý
        if (mainCanvas != null) cardObj.transform.SetParent(mainCanvas.transform);
        cardObj.transform.SetAsLastSibling();

        Vector3 startPos = cardObj.transform.position;
        Quaternion startRot = cardObj.transform.rotation;
        Vector3 targetPos = discardPoint != null ? discardPoint.position : Vector3.zero;

        float t = 0;
        float duration = 0.4f;

        while (t < 1)
        {
            if (cardObj == null) yield break;
            t += Time.deltaTime / duration;

            // Hareketi yumuþat
            float smoothT = t * t * (3f - 2f * t);

            // Pozisyonu, boyutu ve açýyý ayný anda deðiþtir
            cardObj.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            cardObj.transform.localScale = Vector3.Lerp(new Vector3(1.5f, 1.5f, 1.5f ), Vector3.zero, smoothT);
            cardObj.transform.rotation = Quaternion.Lerp(startRot, Quaternion.Euler(0, 0, 180), smoothT); // Uçarken döner

            yield return null;
        }

        Destroy(cardObj);
    }
}