using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class DeckViewerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewerPanel;        // Sağdan açılacak ana panel
    public Transform contentContainer;    // Scroll View içindeki Content objesi
    public GameObject cardPrefab;         // Standart kart prefabı 
    public GameObject supportCardPrefab;  // <--- YENİ: Destek Kartı Prefabı ---

    private DeckManager deckManager;

    void Start()
    {
        // Başlangıçta paneli gizle
        if (viewerPanel != null) viewerPanel.SetActive(false);
        deckManager = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
    }

    // Butona tıklandığında çalışacak fonksiyon
    public void ToggleDeckViewer()
    {
        if (viewerPanel == null) return;

        bool isActive = viewerPanel.activeSelf;

        if (!isActive)
        {
            viewerPanel.SetActive(true);
            viewerPanel.transform.localScale = Vector3.one * 0.9f;
            viewerPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

            CanvasGroup cg = viewerPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = viewerPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.2f);

            RefreshDeckView();
        }
        else
        {
            CanvasGroup cg = viewerPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOFade(0f, 0.15f);

            viewerPanel.transform.DOScale(Vector3.one * 0.9f, 0.15f).SetEase(Ease.InCubic).OnComplete(() =>
            {
                viewerPanel.SetActive(false);
            });
        }
    }

    public void RefreshDeckView()
    {
        if (deckManager == null) deckManager = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
        if (deckManager == null) return;

        // 1. Önceki listelenen kartları temizle
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Güncel desteyi döngüye al ve yarat
        foreach (CardData card in deckManager.allCardsInDeck)
        {
            // --- YENİ: KART TÜRÜNE GÖRE PREFAB SEÇİMİ ---
            GameObject prefabToUse = cardPrefab;
            if (card.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab;
            }

            // Seçilen prefabı yarat
            GameObject newCard = Instantiate(prefabToUse, contentContainer);
            // ----------------------------------------------

            // Kartın UI'da düzgün durması için boyutunu sabitle
            newCard.transform.localScale = Vector3.one;

            // Sürüklemeyi (Draggable) kapat! Sadece görüntüleme amaçlı olduğu için kartlar yerinden oynamamalı.
            Draggable drag = newCard.GetComponent<Draggable>();
            if (drag != null) drag.enabled = false;

            // Kartın verisini yükle
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) display.LoadCard(card);
        }
    }
}