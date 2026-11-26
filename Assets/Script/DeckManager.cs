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
    public Transform discardPoint; // --- YENİ: İskarta destesinin ekrandaki konumu ---

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
        // DiscardHand(); // Bunu kapalı tutuyoruz (El koruma için)

        List<CardData> tempMasterDeck = new List<CardData>(allCardsInDeck);
        List<CardData> cardsInHand = new List<CardData>();

        // Eldeki kartları koru
        foreach (Transform child in handArea)
        {
            CardDisplay display = child.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null) cardsInHand.Add(display.cardData);
        }

        // Eldekileri desteden düş
        foreach (CardData handCard in cardsInHand)
        {
            if (tempMasterDeck.Contains(handCard)) tempMasterDeck.Remove(handCard);
        }

        drawPile = tempMasterDeck;

        // 1. Desteyi Karıştır (Normal prosedür)
        ShuffleDrawPile();

        // 2. --- YENİ: YOLDAŞ KARTI KONTROLÜ ---
        // Karıştırma bitti ama Yoldaş kartı her zaman en üstte (ilk çekilecek) olmalı.
        // Desteyi tara, Companion bulursan en başa taşı.
        CardData companionCard = null;

        // Tersten veya düzden arayabiliriz, ilk bulduğumuzu alalım
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
            drawPile.Remove(companionCard); // Aradan çıkar
            drawPile.Insert(0, companionCard); // En tepeye koy
            Debug.Log($"Savaş Başladı: {companionCard.cardName} (Yoldaş) destenin en tepesine taşındı!");
        }
        // -------------------------------------

        // Önizlemeyi güncelle (Artık en başta Yoldaş görünecek)
       

        if (allCardsInDeck.Count == 0) Debug.LogError("HATA: Ana Deste Boş!");
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

            GameObject prefabToUse = cardPrefab; // Varsayılan normal kart

            if (cardToDraw.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab; // Destek kartıysa özel prefabı kullan
            }


            if (prefabToUse != null)
            {
                // 1. GERÇEK KART (HandArea içinde)
                GameObject realCard = Instantiate(prefabToUse, handArea);

                // --- DÜZELTME: Prefabın orijinal scale'ini koru ---
                // Layout Group bazen scale'i bozabilir, emin olmak için prefab scale'i tekrar atayabilirsin ama
                // genelde Instantiate sonrası dokunmazsak doğru kalır.
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

                // Küçük başla
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
        if (AudioManager.instance != null) AudioManager.instance.PlayCardPaper();
        float duration = 0.5f;
        float t = 0;

        Vector3 startPos = flyingCard.transform.position;
        Quaternion startRot = flyingCard.transform.rotation;

        Vector3 startScale = Vector3.zero; // Başlangıç (Yoktan var olma)

        // --- DÜZELTME BURADA ---
        // Hedef boyut (1,1,1) değil, gerçek kartın o anki boyutu olmalı!
        // targetCard handArea içinde olduğu için doğru boyuttadır.
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

            // Hedef boyuta (targetScale) doğru büyü
            flyingCard.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

            yield return null;
        }

        if (targetCard != null)
        {
            targetCG.alpha = 1f;
            targetCG.blocksRaycasts = true; // YENİ: Uçuş bitti, artık mouse ile tutulabilir!
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

    // --- YENİ FONKSİYON ---
    // Companion Event'ten veya başka bir yerden kart kazanıldığında çağrılır
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

            // Ana desteye ekle (Kayıt için)
            allCardsInDeck.Add(newCard);

            // Ve hemen kullanılabilmesi için Çekme Destesinin EN TEPESİNE koy!
            drawPile.Insert(0, newCard);

            Debug.Log($"Yoldaş {newCard.cardName} alındı ve destenin en tepesine koyuldu!");

          
        }
        else
        {
            // >>> SENARYO 2: NORMAL KART (Unlock) <<<
            // Kural: Bu kart artık destenin kalıcı bir üyesi olur.
            // Sınırsız sayıda eklenebilir (Aynı karttan 2-3 tane olabilir).

            allCardsInDeck.Add(newCard);

            // Savaşın ortasında olduğumuz için bu kartı hemen çekme destesine de ekleyelim mi?
            // Genelde "Shuffle" edilerek araya karışır.
            drawPile.Add(newCard);
            ShuffleDrawPile(); // Karıştır ki rastgele bir yere gitsin

            Debug.Log($"Kart {newCard.cardName} kilidi açıldı ve desteye eklendi!");
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

    // --- YENİ: Kartı iskartaya uçuran ortak fonksiyon ---
    public IEnumerator AnimateCardToDiscard(GameObject cardObj)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayCardPaper();
        if (cardObj == null) yield break;

        // Kartın UI'da her şeyin üstünde görünmesi için Canvas'a taşı
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

            // Hareketi yumuşat
            float smoothT = t * t * (3f - 2f * t);

            // Pozisyonu, boyutu ve açıyı aynı anda değiştir
            cardObj.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            cardObj.transform.localScale = Vector3.Lerp(new Vector3(1.5f, 1.5f, 1.5f ), Vector3.zero, smoothT);
            cardObj.transform.rotation = Quaternion.Lerp(startRot, Quaternion.Euler(0, 0, 180), smoothT); // Uçarken döner

            yield return null;
        }

        Destroy(cardObj);
    }
}