using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StartingDeckManager : MonoBehaviour
{
    public static StartingDeckManager instance;

    [Header("UI References")]
    public GameObject selectionPanel;         // Ana Panel
    public Transform availableCardsContainer; // Üst Taraf (Tüm Kartlar)
    public Transform selectedCardsContainer;  // Alt Taraf (Seçilen 12 Kart)
    public GameObject cardPrefab;             // Kart Prefabý (DeckManager'daki ile ayný)
    public Button confirmButton;              // Belirle Butonu
    public TextMeshProUGUI countText;         // "X / 12" yazýsý
    public GameObject supportCardPrefab;

    [Header("Settings")]
    public int requiredCardCount = 12;        // Limitimiz

    private List<CardData> unlockedPool = new List<CardData>();
    private List<CardData> selectedDeck = new List<CardData>();

    private DeckManager deckManager;

    void Awake() { if (instance == null) instance = this; }

    public void OpenSelection()
    {
        selectionPanel.SetActive(true);
        deckManager = FindObjectOfType<DeckManager>();

        // 1. DeckManager'daki "All Cards" havuzunu tamamen al
        unlockedPool = new List<CardData>(deckManager.allCardsInDeck);
        selectedDeck.Clear();

        // DeckManager'ýn içini boþalt ki sonra biz dolduralým
        deckManager.allCardsInDeck.Clear();

        // --- YENÝ: KAYDEDÝLEN DESTEYÝ YÜKLE ---
        string savedDeckStr = PlayerPrefs.GetString("LastSelectedDeck", "");
        if (!string.IsNullOrEmpty(savedDeckStr))
        {
            string[] savedCardNames = savedDeckStr.Split(',');

            foreach (string cName in savedCardNames)
            {
                if (string.IsNullOrEmpty(cName)) continue;

                // Havuzda bu isimde bir kart var mý bul
                CardData foundCard = unlockedPool.Find(x => x.cardName == cName);
                if (foundCard != null)
                {
                    // Varsa havuzdan çýkar, seçilenlere otomatik ekle
                    unlockedPool.Remove(foundCard);
                    selectedDeck.Add(foundCard);
                }
            }
        }
        // --------------------------------------

        confirmButton.interactable = false;
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(ConfirmDeck);

        RefreshUI(); // RefreshUI çalýþýnca butonu da otomatik aktif edecektir (Eðer 12 kart yüklendiyse)
    }

    public void AddToDeck(DeckSelectionCard cardUI)
    {
        // 12 limitini geçtiyse ekleme
        if (selectedDeck.Count >= requiredCardCount) return;

        unlockedPool.Remove(cardUI.myCard);
        selectedDeck.Add(cardUI.myCard);
        RefreshUI();
    }

    public void RemoveFromDeck(DeckSelectionCard cardUI)
    {
        selectedDeck.Remove(cardUI.myCard);
        unlockedPool.Add(cardUI.myCard);
        RefreshUI();
    }

    void RefreshUI()
    {
        // Mevcut görselleri temizle
        foreach (Transform child in availableCardsContainer) Destroy(child.gameObject);
        foreach (Transform child in selectedCardsContainer) Destroy(child.gameObject);

        // --- Üst Tarafý Doldur ---
        foreach (CardData card in unlockedPool)
        {
            // Kart türüne göre prefab seçimi
            GameObject prefabToUse = cardPrefab;
            if (card.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab;
            }

            GameObject obj = Instantiate(prefabToUse, availableCardsContainer);
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<DeckSelectionCard>().Setup(card, this, false);
        }

        // --- Alt Tarafý Doldur ---
        foreach (CardData card in selectedDeck)
        {
            // Kart türüne göre prefab seçimi
            GameObject prefabToUse = cardPrefab;
            if (card.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab;
            }

            GameObject obj = Instantiate(prefabToUse, selectedCardsContainer);
            obj.transform.localScale = Vector3.one;
            obj.AddComponent<DeckSelectionCard>().Setup(card, this, true);
        }

        // Sayacý ve Butonu güncelle
        if (countText != null) countText.text = $"{selectedDeck.Count} / {requiredCardCount}";
        confirmButton.interactable = (selectedDeck.Count == requiredCardCount); // Sadece 12 olunca aktifleþir
    }
    void ConfirmDeck()
    {
        // 1. Oyuncunun seçtiði 12 kartý asýl desteye at
        deckManager.allCardsInDeck = new List<CardData>(selectedDeck);

        // --- YENÝ: SEÇÝLEN DESTEYÝ HAFIZAYA KAYDET ---
        string deckToSave = "";
        foreach (CardData card in selectedDeck)
        {
            deckToSave += card.cardName + ","; // Kart isimlerini virgülle ayýrarak birleþtir
        }
        PlayerPrefs.SetString("LastSelectedDeck", deckToSave);
        PlayerPrefs.Save();
        // ---------------------------------------------

        // 2. Seçilmeyen dýþlanan kartlarý, Taverna (Companion) havuzuna fýrlat
        if (CompanionEvent.instance != null)
        {
            CompanionEvent.instance.allCompanionCards.AddRange(unlockedPool);
        }

        // 3. Paneli kapat ve haritayý aç
        selectionPanel.SetActive(false);
        if (PathManager.instance != null) PathManager.instance.ShowMap();
    }
}