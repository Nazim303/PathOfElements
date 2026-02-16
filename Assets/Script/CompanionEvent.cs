using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CompanionEvent : MonoBehaviour
{
    public static CompanionEvent instance;

    [Header("Settings")]
    public List<CardData> allCompanionCards; // Tüm olasý yoldaþlar (Inspector'dan ata)
    public Transform cardsContainer;         // Kartlarýn dizileceði yer
    public GameObject choiceButtonPrefab;    // Seçim butonu taslaðý

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void OpenEvent()
    {
        GenerateChoices();
    }

    void GenerateChoices()
    {
        // Önce eski butonlarý temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);

        // 3 Rastgele kart seç
        List<CardData> choices = new List<CardData>();
        List<CardData> pool = new List<CardData>(allCompanionCards); // Havuzu kopyala

        for (int i = 0; i < 3; i++)
        {
            if (pool.Count == 0) break;
            int randIndex = Random.Range(0, pool.Count);
            choices.Add(pool[randIndex]);
            pool.RemoveAt(randIndex); // Ayný kart iki kere gelmesin
        }

        // Butonlarý oluþtur
        foreach (CardData card in choices)
        {
            GameObject btnObj = Instantiate(choiceButtonPrefab, cardsContainer);

            // Butonun üzerindeki scripti bul ve ayarla
            CompanionSelectUI uiScript = btnObj.GetComponent<CompanionSelectUI>();
            if (uiScript != null)
            {
                uiScript.Setup(card, this);
            }
        }
    }

    public void OnCompanionSelected(CardData selectedCard)
    {
        // 1. Desteye Ekle (Yeni Fonksiyonu Kullanýyoruz)
        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm != null)
        {
            dm.AddCardFromEvent(selectedCard);
        }

        // 2. Haritaya Dön
        if (PathManager.instance != null)
        {
            PathManager.instance.CompleteCurrentNode();
        }
    }
}