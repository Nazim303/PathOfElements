using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum JournalCategory { Elementals, Spells, Notes }

public class JournalManager : MonoBehaviour
{
    public static JournalManager instance;

    [Header("Veritabanı (Tüm Objeleri Buraya At)")]
    public List<EnemyData> allEnemies;
    public List<CardData> allCards;

    [Header("UI Panelleri")]
    public GameObject journalMainPanel;
    public Transform gridContainer;      // Izgaranın olduğu yer
    public GameObject journalItemPrefab; // Hazırladığımız küçük kare prefabı

    [Header("Detay Popup (İkinci Görsel)")]
    public GameObject detailPopupPanel;
    public Image detailImage;
    public TextMeshProUGUI detailTitle;
    public TextMeshProUGUI detailDescription;

    // Anlık durumlar
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

    // --- YENİ MANTIK: BUTON FONKSİYONLARI ---

    // 1. Sadece kategoriyi hafızaya alır (Ekranı yenilemez)
    // 0 = Elementals, 1 = Spells, 2 = Notes
    public void SetCategoryOnly(int categoryIndex)
    {
        currentCategory = (JournalCategory)categoryIndex;
    }

    // 2. Elementi seçer ve KOMBİNASYONU ekrana basar
    public void SetElementAndRefresh(int elementIndex)
    {
        currentElement = (ElementType)elementIndex;
        RefreshGrid();
    }

    // (Opsiyonel) Sadece "Notes" gibi elementsiz bir başlığa tıklanırsa direkt yenilemek için
    public void SetCategoryAndRefresh(int categoryIndex)
    {
        currentCategory = (JournalCategory)categoryIndex;
        RefreshGrid();
    }

    // --- LİSTELEME MANTIĞI ---
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

    // --- DETAY POPUP YÖNETİMİ ---
    public void ShowDetailPopup(string title, Sprite art, string desc)
    {
        if (detailTitle != null) detailTitle.text = title;
        if (detailImage != null) detailImage.sprite = art;
        if (detailDescription != null) detailDescription.text = desc;

        if (detailPopupPanel != null)
        {
            detailPopupPanel.SetActive(true);
            detailPopupPanel.transform.SetAsLastSibling();

            CanvasGroup cg = detailPopupPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = detailPopupPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.2f);

            detailPopupPanel.transform.localScale = Vector3.one * 0.9f;
            detailPopupPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
    }

    public void CloseDetailPopup()
    {
        if (detailPopupPanel != null && detailPopupPanel.activeSelf)
        {
            CanvasGroup cg = detailPopupPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.15f).OnComplete(() => detailPopupPanel.SetActive(false));
            }
            else
            {
                detailPopupPanel.SetActive(false);
            }
        }
        else if (detailPopupPanel != null)
        {
            detailPopupPanel.SetActive(false);
        }
    }

    public void OpenJournal()
    {
        if (journalMainPanel != null)
        {
            journalMainPanel.SetActive(true);
            CanvasGroup cg = journalMainPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = journalMainPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.25f);

            journalMainPanel.transform.localScale = Vector3.one * 0.95f;
            journalMainPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
        RefreshGrid();
    }

    public void CloseJournal()
    {
        CloseDetailPopup();
        if (journalMainPanel != null && journalMainPanel.activeSelf)
        {
            CanvasGroup cg = journalMainPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.2f).OnComplete(() => journalMainPanel.SetActive(false));
            }
            else
            {
                journalMainPanel.SetActive(false);
            }
        }
        else if (journalMainPanel != null)
        {
            journalMainPanel.SetActive(false);
        }
    }
}