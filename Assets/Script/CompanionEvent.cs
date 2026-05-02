using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections; // YENÝ: Coroutine için eklendi

public class CompanionEvent : MonoBehaviour
{
    public static CompanionEvent instance;

    [Header("Settings")]
    public List<CardData> allCompanionCards;
    public Transform cardsContainer;
    public GameObject choiceButtonPrefab;
    public GameObject choiceSupportButtonPrefab;

    [Header("UI Elements")]
    public Button skipButton;

    // YENÝ: Animasyon hedefleri
    public Transform deckIconPoint; // Inspector'dan destenin durduðu UI objesini buraya ata
    public float flyDuration = 0.6f; // Süzülme süresi

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipEvent);
        }
    }

    public void OpenEvent()
    {
        GenerateChoices();
    }

    void GenerateChoices()
    {
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);

        List<CardData> choices = new List<CardData>();
        List<CardData> pool = new List<CardData>(allCompanionCards);

        int cardsToPull = Mathf.Min(3, pool.Count);

        for (int i = 0; i < cardsToPull; i++)
        {
            int randIndex = Random.Range(0, pool.Count);
            choices.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }

        foreach (CardData card in choices)
        {
            GameObject prefabToUse = choiceButtonPrefab;

            if (card.cardType == CardType.Support && choiceSupportButtonPrefab != null)
            {
                prefabToUse = choiceSupportButtonPrefab;
            }

            GameObject btnObj = Instantiate(prefabToUse, cardsContainer);

            CompanionSelectUI uiScript = btnObj.GetComponent<CompanionSelectUI>();
            if (uiScript != null)
            {
                uiScript.Setup(card, this);
            }
        }
    }

    // YENÝ: Artýk startTransform parametresi alýyor
    public void OnCompanionSelected(CardData selectedCard, Transform startTransform)
    {
        // Anýnda kapatmak yerine animasyonu baþlatýyoruz
        StartCoroutine(AnimateAndSelectRoutine(selectedCard, startTransform));
    }

    IEnumerator AnimateAndSelectRoutine(CardData selectedCard, Transform startTransform)
    {
        Debug.Log($"[DEBUG 1] Animasyon Coroutine'i basladi! Secilen kart: {selectedCard.cardName}");

        // 1. Oyuncu arka arkaya basmasýn diye tüm butonlarý kilitliyoruz
        foreach (Transform child in cardsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
        if (skipButton != null) skipButton.interactable = false;

        // 2. Havada süzülecek "sahte" bir kopya yarat
        GameObject prefabToUse = (selectedCard.cardType == CardType.Support && choiceSupportButtonPrefab != null) ? choiceSupportButtonPrefab : choiceButtonPrefab;

        GameObject flyingCard = Instantiate(prefabToUse, this.transform);
        Debug.Log($"[DEBUG 2] Ucan kopya obje basariyla yaratildi: {flyingCard.name}");

        flyingCard.transform.position = startTransform.position;
        flyingCard.transform.localScale = startTransform.localScale;
        flyingCard.transform.SetAsLastSibling(); // En öne al

        Button flyingBtn = flyingCard.GetComponent<Button>();
        if (flyingBtn != null) flyingBtn.interactable = false; // Kopyaya tiklanmasin

        CompanionSelectUI uiScript = flyingCard.GetComponent<CompanionSelectUI>();
        if (uiScript != null) uiScript.Setup(selectedCard, this);

        // Orijinal kartý saydamlaþtýr
        CanvasGroup originalCG = startTransform.GetComponent<CanvasGroup>();
        if (originalCG == null) originalCG = startTransform.gameObject.AddComponent<CanvasGroup>();
        originalCG.alpha = 0f;

        // 3. Süzülme Animasyonu (Pozisyon ve Küçülme)
        float t = 0;
        Vector3 startPos = flyingCard.transform.position;

        // --- KRÝTÝK HATA KONTROLÜ ---
        if (deckIconPoint == null)
        {
            Debug.LogError("[HATA] Inspector'da 'Deck Icon Point' yuvasi BOS! Kart ucamayacak, sadece oldugu yerde kuculecek!");
        }
        else
        {
            Debug.Log($"[DEBUG 3] Hedef nokta bulundu. Ucus basliyor... Sure: {flyDuration} saniye");
        }

        Vector3 targetPos = deckIconPoint != null ? deckIconPoint.position : startPos;
        Vector3 startScale = flyingCard.transform.localScale;
        Vector3 targetScale = Vector3.zero;

        while (t < 1)
        {
            t += Time.deltaTime / flyDuration;
            float smoothT = t * t * (3f - 2f * t);

            if (flyingCard != null)
            {
                flyingCard.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                flyingCard.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            }
            yield return null;
        }

        Debug.Log("[DEBUG 4] Ucus dongusu (while) bitti. Ucan kopya siliniyor.");
        if (flyingCard != null) Destroy(flyingCard);

        // 4. Animasyon bitti! Orijinal iþlemleri gerçekleþtir
        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm != null)
        {
            dm.AddCardFromEvent(selectedCard);
        }

        if (selectedCard.cardType != CardType.Companion)
        {
            if (allCompanionCards.Contains(selectedCard))
            {
                allCompanionCards.Remove(selectedCard);
                Debug.Log($"[DEBUG 5] {selectedCard.cardName} kalici bir kart, havuzdan cikarildi.");
            }
        }

        if (skipButton != null) skipButton.interactable = true;

        if (PathManager.instance != null)
        {
            Debug.Log("[DEBUG 6] PathManager'a haritaya donus emri veriliyor.");
            PathManager.instance.CompleteCurrentNode();
        }
    }

    public void SkipEvent()
    {
        Debug.Log("Tavernadan hiçbir þey almadan, desteni temiz tutarak ayrýldýn.");

        if (PathManager.instance != null)
        {
            PathManager.instance.CompleteCurrentNode();
        }
    }
}