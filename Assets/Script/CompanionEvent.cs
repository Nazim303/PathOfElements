using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CompanionEvent : MonoBehaviour
{
    public static CompanionEvent instance;

    [Header("Settings")]
    public List<CardData> allCompanionCards;
    public Transform cardsContainer;
    public GameObject choiceButtonPrefab;        // Normal kartlar için Prefab
    public GameObject choiceSupportButtonPrefab; // --- YENÝ: Destek (Support) kartlarý için özel Prefab ---

    [Header("UI Elements")]
    public Button skipButton;

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
        // Önce eski butonlarý temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);

        List<CardData> choices = new List<CardData>();
        List<CardData> pool = new List<CardData>(allCompanionCards);

        // Havuzda kaç kart kaldýysa o kadarýný çek (En fazla 3)
        int cardsToPull = Mathf.Min(3, pool.Count);

        for (int i = 0; i < cardsToPull; i++)
        {
            int randIndex = Random.Range(0, pool.Count);
            choices.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }

        // Butonlarý oluþtur
        foreach (CardData card in choices)
        {
            // --- YENÝ MANTIK: Kartýn türüne göre Prefab seç ---
            GameObject prefabToUse = choiceButtonPrefab; // Varsayýlan normal

            // Eðer kart Destek (Support) türündeyse ve özel prefab atandýysa onu kullan
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

    public void OnCompanionSelected(CardData selectedCard)
    {
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
                Debug.Log($"{selectedCard.cardName} kalýcý bir kart olduðu için havuzdan çýkarýldý.");
            }
        }
        else
        {
            Debug.Log($"{selectedCard.cardName} tek kullanýmlýk bir yoldaþ olduðu için Tavernada kalmaya devam edecek.");
        }

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