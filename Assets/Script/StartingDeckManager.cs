using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StartingDeckManager : MonoBehaviour
{
    public static StartingDeckManager instance;

    [Header("UI References")]
    public GameObject selectionPanel;
    public Transform availableCardsContainer;
    public Transform selectedCardsContainer;
    public GameObject cardPrefab;
    public GameObject supportCardPrefab;
    public Button confirmButton;
    public TextMeshProUGUI countText;

    [Header("Settings")]
    public int requiredCardCount = 12;

    private List<CardData> unlockedPool = new List<CardData>();
    private List<CardData> selectedDeck = new List<CardData>();
    private DeckManager deckManager;

    void Awake() { if (instance == null) instance = this; }

    public void OpenSelection()
    {
        selectionPanel.SetActive(true);
        deckManager = FindObjectOfType<DeckManager>();

        // 1. Havuzu doldur ve DeckManager'daki asýl desteyi temizle
        unlockedPool = new List<CardData>(deckManager.allCardsInDeck);
        selectedDeck.Clear();
        deckManager.allCardsInDeck.Clear();

        // 2. SADECE hafýzada kayýtlý bir deste varsa otomatik yükle
        string savedDeck = PlayerPrefs.GetString("LastSelectedDeck", "");

        if (!string.IsNullOrEmpty(savedDeck))
        {
            string[] savedCardNames = savedDeck.Split(',');
            foreach (string cardName in savedCardNames)
            {
                if (string.IsNullOrEmpty(cardName)) continue;

                CardData foundCard = unlockedPool.Find(c => c.cardName == cardName);
                if (foundCard != null)
                {
                    selectedDeck.Add(foundCard);
                    unlockedPool.Remove(foundCard);
                }
            }
        }
        // Eðer kayýtlý deste yoksa (örn: Tutorial'dan yeni gelmiþse) 
        // sistem hiçbir þeyi otomatik seçmez, alt taraf boþ kalýr.

        RefreshSelectionUI();
    }

    public void AddToDeck(DeckSelectionCard cardScript)
    {
        if (selectedDeck.Count < requiredCardCount)
        {
            selectedDeck.Add(cardScript.myCard);
            unlockedPool.Remove(cardScript.myCard);
            RefreshSelectionUI();
        }
    }

    public void RemoveFromDeck(DeckSelectionCard cardScript)
    {
        unlockedPool.Add(cardScript.myCard);
        selectedDeck.Remove(cardScript.myCard);
        RefreshSelectionUI();
    }

    public void RefreshSelectionUI()
    {
        // Havuzdaki kartlarý yarat
        foreach (Transform child in availableCardsContainer) Destroy(child.gameObject);
        foreach (CardData card in unlockedPool)
        {
            GameObject prefabToUse = (card.cardType == CardType.Support && supportCardPrefab != null) ? supportCardPrefab : cardPrefab;
            GameObject obj = Instantiate(prefabToUse, availableCardsContainer);
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<DeckSelectionCard>().Setup(card, this, false);
        }

        // Seçilen kartlarý yarat
        foreach (Transform child in selectedCardsContainer) Destroy(child.gameObject);
        foreach (CardData card in selectedDeck)
        {
            GameObject prefabToUse = (card.cardType == CardType.Support && supportCardPrefab != null) ? supportCardPrefab : cardPrefab;
            GameObject obj = Instantiate(prefabToUse, selectedCardsContainer);
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<DeckSelectionCard>().Setup(card, this, true);
        }

        if (countText != null) countText.text = $"{selectedDeck.Count} / {requiredCardCount}";
        if (confirmButton != null) confirmButton.interactable = (selectedDeck.Count == requiredCardCount);
    }

    public void ConfirmDeck()
    {
        deckManager.allCardsInDeck = new List<CardData>(selectedDeck);

        // Seçilen desteyi bir sonraki oyun için hafýzaya kaydet
        string deckToSave = "";
        foreach (CardData card in selectedDeck)
        {
            deckToSave += card.cardName + ",";
        }
        PlayerPrefs.SetString("LastSelectedDeck", deckToSave);
        PlayerPrefs.Save();

        // Dýþlanan kartlarý Taverna (Companion) havuzuna fýrlat
        if (CompanionEvent.instance != null)
        {
            CompanionEvent.instance.allCompanionCards.AddRange(unlockedPool);
        }

        selectionPanel.SetActive(false);
        if (PathManager.instance != null) PathManager.instance.ShowMap();
    }
}
