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

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmDeck);
            confirmButton.onClick.AddListener(ConfirmDeck);
        }
    }

    public void OpenSelection()
    {
        selectionPanel.SetActive(true);
        deckManager = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();

        // 1. Havuzu doldur ve DeckManager'daki asıl desteyi temizle
        unlockedPool = new List<CardData>(deckManager.allCardsInDeck);
        selectedDeck.Clear();
        deckManager.allCardsInDeck.Clear();

        // 2. Hafızada kayıtlı bir deste varsa yükle
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

            // Kayıtlı destede hiç Destek (Support) kartı yoksa, oyuncunun eline en az 2 destek kartı geçmesi için takviye yap
            bool hasSupportInDeck = selectedDeck.Exists(c => c.cardType == CardType.Support);
            if (!hasSupportInDeck)
            {
                List<CardData> availableSupports = unlockedPool.FindAll(c => c.cardType == CardType.Support);
                int supportsToAdd = Mathf.Min(2, availableSupports.Count);
                for (int i = 0; i < supportsToAdd; i++)
                {
                    CardData supCard = availableSupports[i];
                    // Son kartlardan birini çıkarıp yerine destek kartı ekle
                    if (selectedDeck.Count >= requiredCardCount)
                    {
                        CardData removed = selectedDeck[selectedDeck.Count - 1];
                        selectedDeck.RemoveAt(selectedDeck.Count - 1);
                        unlockedPool.Add(removed);
                    }
                    selectedDeck.Add(supCard);
                    unlockedPool.Remove(supCard);
                }
            }
        }
        else
        {
            // Kayıtlı deste yoksa dengeli bir başlangıç destesi oluştur (Saldırı + Şifa + Destek)
            // Önce 2-3 destek kartı ekle
            List<CardData> supportPool = unlockedPool.FindAll(c => c.cardType == CardType.Support);
            int supCount = Mathf.Min(3, supportPool.Count);
            for (int i = 0; i < supCount; i++)
            {
                CardData sc = supportPool[i];
                selectedDeck.Add(sc);
                unlockedPool.Remove(sc);
            }

            // Kalan yerleri diğer kartlarla doldur
            while (selectedDeck.Count < requiredCardCount && unlockedPool.Count > 0)
            {
                CardData ac = unlockedPool[0];
                selectedDeck.Add(ac);
                unlockedPool.RemoveAt(0);
            }
        }

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
        // Havuzdaki kartları yarat
        foreach (Transform child in availableCardsContainer) Destroy(child.gameObject);
        foreach (CardData card in unlockedPool)
        {
            GameObject prefabToUse = (card.cardType == CardType.Support && supportCardPrefab != null) ? supportCardPrefab : cardPrefab;
            GameObject obj = Instantiate(prefabToUse, availableCardsContainer);
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<DeckSelectionCard>().Setup(card, this, false);
        }

        // Seçilen kartları yarat
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

        // Seçilen desteyi bir sonraki oyun için hafızaya kaydet
        string deckToSave = "";
        foreach (CardData card in selectedDeck)
        {
            deckToSave += card.cardName + ",";
        }
        PlayerPrefs.SetString("LastSelectedDeck", deckToSave);
        PlayerPrefs.Save();

        // Dışlanan kartları Taverna (Companion) havuzuna fırlat (Mükerrer eklemeyi önle)
        if (CompanionEvent.instance != null)
        {
            foreach (CardData card in unlockedPool)
            {
                if (card != null && !CompanionEvent.instance.allCompanionCards.Contains(card))
                {
                    CompanionEvent.instance.allCompanionCards.Add(card);
                }
            }
        }

        selectionPanel.SetActive(false);
        if (PathManager.instance != null) PathManager.instance.ShowMap();
    }
}
