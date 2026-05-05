using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum JournalCategory { Elementals, Spells, Notes }

public class JournalManager : MonoBehaviour
{
    public static JournalManager instance;

    [Header("Veritabaný (Tüm Objeleri Buraya At)")]
    public List<EnemyData> allEnemies;
    public List<CardData> allCards;

    [Header("UI Panelleri")]
    public GameObject journalMainPanel;
    public Transform gridContainer;      // Izgaranýn olduðu yer
    public GameObject journalItemPrefab; // Hazýrladýðýmýz küçük kare prefabý

    [Header("Detay Popup (Ýkinci Görsel)")]
    public GameObject detailPopupPanel;
    public Image detailImage;
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailDescription;

    // Anlýk durumlar
    private JournalCategory currentCategory = JournalCategory.Spells;
    private ElementType currentElement = ElementType.Fire;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        CloseDetailPopup();
        journalMainPanel.SetActive(false);
    }

    // --- YENÝ MANTIK: BUTON FONKSÝYONLARI ---

    // 1. Sadece kategoriyi hafýzaya alýr (Ekraný yenilemez)
    // 0 = Elementals, 1 = Spells, 2 = Notes
    public void SetCategoryOnly(int categoryIndex)
    {
        currentCategory = (JournalCategory)categoryIndex;
    }

    // 2. Elementi seçer ve KOMBÝNASYONU ekrana basar
    public void SetElementAndRefresh(int elementIndex)
    {
        currentElement = (ElementType)elementIndex;
        RefreshGrid();
    }

    // (Opsiyonel) Sadece "Notes" gibi elementsiz bir baþlýða týklanýrsa direkt yenilemek için
    public void SetCategoryAndRefresh(int categoryIndex)
    {
        currentCategory = (JournalCategory)categoryIndex;
        RefreshGrid();
    }

    // --- LÝSTELEME MANTIÐI ---
    public void RefreshGrid()
    {
        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentCategory == JournalCategory.Elementals)
        {
            foreach (EnemyData enemy in allEnemies)
            {
                if (enemy.element == currentElement)
                {
                    CreateItem(enemy: enemy);
                }
            }
        }
        else if (currentCategory == JournalCategory.Spells)
        {
            foreach (CardData card in allCards)
            {
                if (card.element == currentElement)
                {
                    CreateItem(card: card);
                }
            }
        }
        else if (currentCategory == JournalCategory.Notes)
        {
            Debug.Log("Notes henüz eklenmedi.");
        }
    }

    void CreateItem(EnemyData enemy = null, CardData card = null)
    {
        GameObject newItem = Instantiate(journalItemPrefab, gridContainer);
        JournalItemUI itemUI = newItem.GetComponent<JournalItemUI>();

        if (enemy != null) itemUI.SetupEnemy(enemy);
        else if (card != null) itemUI.SetupCard(card);
    }

    // --- DETAY POPUP YÖNETÝMÝ ---
    public void ShowDetailPopup(string title, Sprite art, string desc)
    {
        detailTitle.text = title;
        detailImage.sprite = art;
        detailDescription.text = desc;

        detailPopupPanel.SetActive(true);
        detailPopupPanel.transform.SetAsLastSibling();
    }

    public void CloseDetailPopup()
    {
        detailPopupPanel.SetActive(false);
    }

    public void OpenJournal()
    {
        journalMainPanel.SetActive(true);
        RefreshGrid();
    }

    public void CloseJournal()
    {
        journalMainPanel.SetActive(false);
        CloseDetailPopup();
    }
}