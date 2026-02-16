using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DivinerManager : MonoBehaviour
{
    public static DivinerManager instance;

    [Header("UI")]
    public GameObject divinerPanel;
    public Transform cardsContainer;
    public GameObject cardPrefab; // Arkasý kapalý kart prefabý
    public Button continueButton;

    [Header("Tarot Card Arts")]
    public Sprite cardBackSprite; // Kartýn arkasý
    public Sprite tarotCombatSprite; // Tarot Büyücü (Combat Temsili)
    public Sprite tarotCompanionSprite; // Tarot Aþýklar (Companion Temsili)
    public Sprite tarotShopSprite; // Tarot Araba (Shop Temsili)

    [Header("Map Icons (Real Path Icons)")]
    public Sprite mapCombatIcon; // Haritadaki Kýlýç Ýkonu
    public Sprite mapCompanionIcon; // Haritadaki Yoldaþ Ýkonu
    public Sprite mapShopIcon; // Haritadaki Kese Ýkonu

    [Header("Settings")]
    public float shuffleSpeed = 0.4f; // Yer deðiþtirme hýzý
    public int shuffleCount = 5; // Kaç kere yer deðiþsin?

    private List<DivinerCard> activeCards = new List<DivinerCard>();
    private bool isShuffling = false;

    private Coroutine minigameCoroutine;

    private void Start()
    {
        if(continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.gameObject.SetActive(false);// baþta gizli 
        }


    }

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void OpenDivinerEvent()
    {
        divinerPanel.SetActive(true);
        // Sadece kartlarý göster, karýþtýrma
        StartCoroutine(ShowCardsPhase());
    }

    IEnumerator StartMinigame()
    {
        // 1. Kartlarý Temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);
        activeCards.Clear();

        // 2. Layout Group'u Geçici Olarak Aç (Hizalamak için)
        HorizontalLayoutGroup layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = true;

        // 3. Kartlarý Oluþtur (Combat, Companion, Shop)
        CreateCard(PathType.Combat, tarotCombatSprite);
        CreateCard(PathType.Companion, tarotCompanionSprite);
        CreateCard(PathType.Shop, tarotShopSprite);

        // Layout'un kartlarý dizmesi için 1 kare bekle
        yield return new WaitForEndOfFrame();

        // 4. Layout Group'u KAPAT (Artýk biz hareket ettireceðiz)
        if (layout != null) layout.enabled = false;

        yield return new WaitForSeconds(2.5f);

        // 5. Kartlarý Kapat (Arkasý dönsün)
        foreach (var card in activeCards) card.FlipBack(cardBackSprite);

        yield return new WaitForSeconds(0.5f);

        // 6. Karýþtýr (Animasyonlu)
        yield return StartCoroutine(ShuffleRoutine());

        // 7. Seçime Ýzin Ver
        foreach (var card in activeCards) card.button.interactable = true;
    }


    void OnContinueClicked()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        // Aþama 2: Kapat ve Karýþtýr
        StartCoroutine(ShufflePhase());
    }

    IEnumerator ShufflePhase()
    {
        // 5. Kartlarý Kapat
        foreach (var card in activeCards) card.FlipBack(cardBackSprite);

        yield return new WaitForSeconds(0.5f);

        // 6. Karýþtýr
        yield return StartCoroutine(ShuffleRoutine());

        // 7. Seçime Ýzin Ver
        foreach (var card in activeCards) card.button.interactable = true;
    }
    void CreateCard(PathType type, Sprite tarotArt)
    {
        GameObject obj = Instantiate(cardPrefab, cardsContainer);
        DivinerCard script = obj.AddComponent<DivinerCard>();
        script.Setup(type, tarotArt, this);
        activeCards.Add(script);
    }

    // --- YENÝ: ANÝMASYONLU KARISTIRMA ---
    IEnumerator ShuffleRoutine()
    {
        isShuffling = true;

        for (int i = 0; i < shuffleCount; i++)
        {
            // Rastgele iki kart seç
            int index1 = Random.Range(0, activeCards.Count);
            int index2 = Random.Range(0, activeCards.Count);

            // Aynýysa tekrar seç
            while (index1 == index2) index2 = Random.Range(0, activeCards.Count);

            DivinerCard card1 = activeCards[index1];
            DivinerCard card2 = activeCards[index2];

            // Pozisyonlarýný al
            Vector3 pos1 = card1.transform.localPosition;
            Vector3 pos2 = card2.transform.localPosition;

            // Animasyonla yer deðiþtir (Swap)
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / shuffleSpeed;
                // Smoothstep ile yumuþak hareket
                float smoothT = t * t * (3f - 2f * t);

                // Yay çizerek gitmeleri için (Shell game hissi) biraz Y offset verebiliriz ama düz swap yeterli þimdilik.
                card1.transform.localPosition = Vector3.Lerp(pos1, pos2, smoothT);
                card2.transform.localPosition = Vector3.Lerp(pos2, pos1, smoothT);

                yield return null;
            }

            // Pozisyonlarý tam oturt
            card1.transform.localPosition = pos2;
            card2.transform.localPosition = pos1;

            yield return new WaitForSeconds(0.1f); // Kýsa bekleme
        }

        isShuffling = false;
    }

    public void OnCardSelected(DivinerCard selectedCard)
    {
        if (isShuffling) return;

        // Seçileni aç
        selectedCard.FlipFront();

        // Diðerlerini de aç ama pasif yap
        foreach (var card in activeCards)
        {
            card.button.interactable = false;
            if (card != selectedCard) card.FlipFront();
        }

        // --- DÜZELTME: HARÝTAYA GERÇEK ÝKONU GÖNDER ---
        Sprite realMapIcon = GetMapIconForType(selectedCard.myType);
        ApplyFate(selectedCard.myType, realMapIcon);

        StartCoroutine(CloseDelay());
    }

    Sprite GetMapIconForType(PathType type)
    {
        switch (type)
        {
            case PathType.Combat: return mapCombatIcon;
            case PathType.Companion: return mapCompanionIcon;
            case PathType.Shop: return mapShopIcon;
            default: return null;
        }
    }

    void ApplyFate(PathType type, Sprite icon)
    {
        MapNode currentNode = PathManager.instance.currentNode;
        if (currentNode != null && currentNode.nextNodes.Count > 0)
        {
            // Rastgele bir sonraki düðümü seç
            int randIndex = Random.Range(0, currentNode.nextNodes.Count);
            MapNode nextNode = currentNode.nextNodes[randIndex];

            // --- YENÝ: BOSS KORUMASI ---
            // Eðer sýradaki düðüm Boss ise, onu deðiþtirme!
            // (Alternatif olarak: Baþka bir nextNode dene veya hiçbir þey yapma)
            if (nextNode.pathType == PathType.Boss)
            {
                Debug.Log("Sýradaki durak Boss olduðu için kader deðiþtirilemedi.");
                return;
            }
            // ---------------------------

            nextNode.ChangeType(type, icon);
            Debug.Log($"Gelecek deðiþti! Sonraki durak: {type}");
        }
    }

    IEnumerator CloseDelay()
    {
        yield return new WaitForSeconds(2f);
        divinerPanel.SetActive(false);
        PathManager.instance.CompleteCurrentNode();
    }

    IEnumerator ShowCardsPhase()
    {
        // 1. Kartlarý Temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);
        activeCards.Clear();

        // 2. Layout Aç
        HorizontalLayoutGroup layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = true;

        // 3. Kartlarý Oluþtur
        CreateCard(PathType.Combat, tarotCombatSprite);
        CreateCard(PathType.Companion, tarotCompanionSprite);
        CreateCard(PathType.Shop, tarotShopSprite);

        yield return new WaitForEndOfFrame();

        // 4. Layout Kapat
        if (layout != null) layout.enabled = false;

        // --- YENÝ: Oyuncunun incelemesi için bekle (Butonu aç) ---
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }

        // Butona basýlana kadar bekle...
    }
}

// Kart Yardýmcý Sýnýfý (Ayný kalýyor ama scriptin içine dahil ettim)
public class DivinerCard : MonoBehaviour
{
    public PathType myType;
    public Sprite frontSprite;
    public Image img;
    public Button button;
    private DivinerManager manager;

    public void Setup(PathType type, Sprite icon, DivinerManager mgr)
    {
        myType = type;
        frontSprite = icon;
        manager = mgr;

        img = GetComponent<Image>();
        button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();

        img.sprite = icon; // Ýlk baþta açýk gelsin
        button.onClick.AddListener(() => manager.OnCardSelected(this));
        button.interactable = false;
    }

    public void FlipBack(Sprite back) { img.sprite = back; }
    public void FlipFront() { img.sprite = frontSprite; }
}