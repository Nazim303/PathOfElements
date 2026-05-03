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

        // 1. Lock buttons to prevent spam clicks
        foreach (Transform child in cardsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = false;
        }
        if (skipButton != null) skipButton.interactable = false;

        // 2. Create the "Real Card" dummy for flying
        GameObject prefabToUse = (selectedCard.cardType == CardType.Support && supportCardPrefab != null) ? supportCardPrefab : standardCardPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("[HATA] Uçuþ için Card Prefab atanmamýþ! Lütfen Inspector'dan kontrol et.");
            yield break;
        }

        // --- CRITICAL FIX: Find the Canvas and make it the parent ---
        Canvas mainCanvas = GetComponentInParent<Canvas>();
        if (mainCanvas == null)
        {
            // Fallback if the script isn't under a Canvas for some reason
            mainCanvas = FindObjectOfType<Canvas>();
        }

        // Instantiate under the Canvas, NOT 'this.transform'
        GameObject flyingCard = Instantiate(prefabToUse, mainCanvas.transform);

        // Match the starting position and scale of the clicked button
        flyingCard.transform.position = startTransform.position;

        // We use localScale here, but because it's under a Canvas now, it won't explode.
        // We match it to the prefab's default scale just to be safe.
        flyingCard.transform.localScale = prefabToUse.transform.localScale;

        flyingCard.transform.SetAsLastSibling(); // Bring to the very front

        // --- Remove Draggable ---
        if (flyingCard.GetComponent<Draggable>() != null) Destroy(flyingCard.GetComponent<Draggable>());

        // Disable Raycasts so it doesn't block the mouse
        CanvasGroup flyCG = flyingCard.GetComponent<CanvasGroup>();
        if (flyCG == null) flyCG = flyingCard.AddComponent<CanvasGroup>();
        flyCG.blocksRaycasts = false;

        // Load visual data into the dummy card
        CardDisplay display = flyingCard.GetComponent<CardDisplay>();
        if (display != null) display.LoadCard(selectedCard);

        // Make the original clicked button transparent
        CanvasGroup originalCG = startTransform.GetComponent<CanvasGroup>();
        if (originalCG == null) originalCG = startTransform.gameObject.AddComponent<CanvasGroup>();
        originalCG.alpha = 0f;

        // 3. The Flying Animation (Position and Scale)
        float t = 0;
        Vector3 startPos = flyingCard.transform.position;
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