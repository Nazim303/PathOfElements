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

    [Header("Flying Animation Prefabs")]
    public GameObject standardCardPrefab;
    public GameObject supportCardPrefab;

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
        Debug.Log($"[DEBUG] Süzülme Animasyonu Baþlýyor: {selectedCard.cardName}");

        // 1. Lock AND Hide the unselected buttons to prevent spam clicks and clean the UI
        foreach (Transform child in cardsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }

            // YENÝ: Seçilmeyen diðer kartlarý anýnda görünmez yap
            if (child != startTransform)
            {
                child.gameObject.SetActive(false);
            }
        }
        if (skipButton != null) skipButton.interactable = false;

        // 2. Create the "Real Card" dummy for flying
        GameObject prefabToUse = (selectedCard.cardType == CardType.Support && supportCardPrefab != null) ? supportCardPrefab : standardCardPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("[HATA] Uçuþ için Card Prefab atanmamýþ! Lütfen Inspector'dan kontrol et.");
            yield break;
        }

        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            mainCanvas = FindObjectOfType<Canvas>();
        }

        GameObject flyingCard = Instantiate(prefabToUse, mainCanvas.transform);

        // Match the starting position and scale of the clicked button
        flyingCard.transform.position = startTransform.position;
        flyingCard.transform.localScale = prefabToUse.transform.localScale;
        // Start rotation flat
        Quaternion startRot = flyingCard.transform.rotation;

        flyingCard.transform.SetAsLastSibling();

        if (flyingCard.GetComponent<Draggable>() != null) Destroy(flyingCard.GetComponent<Draggable>());

        CanvasGroup flyCG = flyingCard.GetComponent<CanvasGroup>();
        if (flyCG == null) flyCG = flyingCard.AddComponent<CanvasGroup>();
        flyCG.blocksRaycasts = false;

        CardDisplay display = flyingCard.GetComponent<CardDisplay>();
        if (display != null) display.LoadCard(selectedCard);

        CanvasGroup originalCG = startTransform.GetComponent<CanvasGroup>();
        if (originalCG == null) originalCG = startTransform.gameObject.AddComponent<CanvasGroup>();
        originalCG.alpha = 0f;

        // 3. The Flying Animation (Position, Scale, AND Rotation)
        float t = 0;
        Vector3 startPos = flyingCard.transform.position;
        Vector3 targetPos = deckIconPoint != null ? deckIconPoint.position : startPos;

        // Slightly enlarge at the start, then shrink to zero
        Vector3 startScale = new Vector3(1.5f, 1.5f, 1.5f);
        Vector3 targetScale = Vector3.zero;

        // The rotation goal (flipping the card)
        Quaternion targetRot = Quaternion.Euler(0, 0, 180);

        while (t < 1)
        {
            t += Time.deltaTime / flyDuration;

            // A slightly more dynamic curve: Fast start, slow finish
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            if (flyingCard != null)
            {
                // Lerp Position
                flyingCard.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);

                // Lerp Scale
                flyingCard.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

                // Lerp Rotation (Spins as it flies)
                flyingCard.transform.rotation = Quaternion.Lerp(startRot, targetRot, smoothT);
            }
            yield return null;
        }

        if (flyingCard != null) Destroy(flyingCard);

        // 4. Animation finished! Execute the actual logic
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
                Debug.Log($"{selectedCard.cardName} kalýcý bir kart, havuzdan çýkarýldý.");
            }
        }
        else
        {
            Debug.Log($"{selectedCard.cardName} tek kullanýmlýk bir yoldaþ, Tavernada kalmaya devam edecek.");
        }

        if (skipButton != null) skipButton.interactable = true;

        if (PathManager.instance != null)
        {
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