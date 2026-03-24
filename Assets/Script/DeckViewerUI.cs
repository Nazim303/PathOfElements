using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckViewerUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject viewerPanel;        // Saðdan açýlacak ana panel
    public Transform contentContainer;    // Scroll View içindeki Content objesi
    public GameObject cardPrefab;         // Standart kart prefabý 
    public GameObject supportCardPrefab;  // <--- YENÝ: Destek Kartý Prefabý ---

    private DeckManager deckManager;

    void Start()
    {
        // Baþlangýçta paneli gizle
        if (viewerPanel != null) viewerPanel.SetActive(false);
        deckManager = FindObjectOfType<DeckManager>();
    }

    // Butona týklandýðýnda çalýþacak fonksiyon
    public void ToggleDeckViewer()
    {
        if (viewerPanel == null) return;

        bool isActive = viewerPanel.activeSelf;
        viewerPanel.SetActive(!isActive);

        // Panel açýlýyorsa (aktif hale geliyorsa) listeyi yenile
        if (!isActive)
        {
            RefreshDeckView();
        }
    }

    public void RefreshDeckView()
    {
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (deckManager == null) return;

        // 1. Önceki listelenen kartlarý temizle
        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Güncel desteyi döngüye al ve yarat
        foreach (CardData card in deckManager.allCardsInDeck)
        {
            // --- YENÝ: KART TÜRÜNE GÖRE PREFAB SEÇÝMÝ ---
            GameObject prefabToUse = cardPrefab;
            if (card.cardType == CardType.Support && supportCardPrefab != null)
            {
                prefabToUse = supportCardPrefab;
            }

            // Seçilen prefabý yarat
            GameObject newCard = Instantiate(prefabToUse, contentContainer);
            // ----------------------------------------------

            // Kartýn UI'da düzgün durmasý için boyutunu sabitle
            newCard.transform.localScale = Vector3.one;

            // Sürüklemeyi (Draggable) kapat! Sadece görüntüleme amaçlý olduðu için kartlar yerinden oynamamalý.
            Draggable drag = newCard.GetComponent<Draggable>();
            if (drag != null) drag.enabled = false;

            // Kartýn verisini yükle
            CardDisplay display = newCard.GetComponent<CardDisplay>();
            if (display != null) display.LoadCard(card);
        }
    }
}