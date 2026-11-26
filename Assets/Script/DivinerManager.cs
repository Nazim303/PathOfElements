using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class DivinerManager : MonoBehaviour
{
    public static DivinerManager instance;

    [Header("UI")]
    public GameObject divinerPanel;
    public Transform cardsContainer;
    public GameObject cardPrefab; // Arkası kapalı kart prefabı
    public Button continueButton;

    [Header("Tarot Card Arts")]
    public Sprite cardBackSprite; // Kartın arkası
    public Sprite tarotCombatSprite; // Tarot Büyücü (Combat Temsili)
    public Sprite tarotCompanionSprite; // Tarot Aşıklar (Companion Temsili)
    public Sprite tarotShopSprite; // Tarot Araba (Shop Temsili)

    [Header("Map Icons (Real Path Icons)")]
    public Sprite mapCombatIcon; // Haritadaki Kılıç İkonu
    public Sprite mapCompanionIcon; // Haritadaki Yoldaş İkonu
    public Sprite mapShopIcon; // Haritadaki Kese İkonu

    [Header("Settings")]
    public float shuffleSpeed = 0.4f; // Yer değiştirme hızı
    public int shuffleCount = 5; // Kaç kere yer değişsin?

    private List<DivinerCard> activeCards = new List<DivinerCard>();
    private bool isShuffling = false;

    private Coroutine minigameCoroutine;

    private void Start()
    {
        if(continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.gameObject.SetActive(false);// başta gizli 
        }


    }

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void OpenDivinerEvent()
    {
        if (divinerPanel != null)
        {
            divinerPanel.SetActive(true);
            CanvasGroup cg = divinerPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = divinerPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.35f);
            divinerPanel.transform.localScale = Vector3.one * 0.95f;
            divinerPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
        // Sadece kartları göster, karıştırma
        StartCoroutine(ShowCardsPhase());
    }

    IEnumerator StartMinigame()
    {
        // 1. Kartları Temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);
        activeCards.Clear();

        // 2. Layout Group'u Geçici Olarak Aç (Hizalamak için)
        HorizontalLayoutGroup layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = true;

        // 3. Kartları Oluştur (Combat, Companion, Shop)
        CreateCard(PathType.Combat, tarotCombatSprite);
        CreateCard(PathType.Companion, tarotCompanionSprite);
        CreateCard(PathType.Shop, tarotShopSprite);

        // Layout'un kartları dizmesi için 1 kare bekle
        yield return new WaitForEndOfFrame();

        // 4. Layout Group'u KAPAT (Artık biz hareket ettireceğiz)
        if (layout != null) layout.enabled = false;

        yield return new WaitForSeconds(2.5f);

        // 5. Kartları Kapat (Arkası dönsün)
        foreach (var card in activeCards) card.FlipBack(cardBackSprite);

        yield return new WaitForSeconds(0.5f);

        // 6. Karıştır (Animasyonlu)
        yield return StartCoroutine(ShuffleRoutine());

        // 7. Seçime İzin Ver
        foreach (var card in activeCards) card.button.interactable = true;
    }


    void OnContinueClicked()
    {
        if (continueButton != null) continueButton.gameObject.SetActive(false);

        // Aşama 2: Kapat ve Karıştır
        StartCoroutine(ShufflePhase());
    }

    IEnumerator ShufflePhase()
    {
        // 5. Kartları Kapat
        foreach (var card in activeCards) card.FlipBack(cardBackSprite);

        yield return new WaitForSeconds(0.5f);

        // 6. Karıştır
        yield return StartCoroutine(ShuffleRoutine());

        // 7. Seçime İzin Ver
        foreach (var card in activeCards) card.button.interactable = true;
    }
    void CreateCard(PathType type, Sprite tarotArt)
    {
        GameObject obj = Instantiate(cardPrefab, cardsContainer);
        DivinerCard script = obj.AddComponent<DivinerCard>();
        script.Setup(type, tarotArt, this);
        activeCards.Add(script);
    }

    // --- YENİ: ANİMASYONLU KARISTIRMA ---
    IEnumerator ShuffleRoutine()
    {
        isShuffling = true;

        for (int i = 0; i < shuffleCount; i++)
        {
            // Rastgele iki kart seç
            int index1 = Random.Range(0, activeCards.Count);
            int index2 = Random.Range(0, activeCards.Count);

            // Aynıysa tekrar seç
            while (index1 == index2) index2 = Random.Range(0, activeCards.Count);

            DivinerCard card1 = activeCards[index1];
            DivinerCard card2 = activeCards[index2];

            // Pozisyonlarını al
            Vector3 pos1 = card1.transform.localPosition;
            Vector3 pos2 = card2.transform.localPosition;

            // Animasyonla yer değiştir (Swap)
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / shuffleSpeed;
                // Smoothstep ile yumuşak hareket
                float smoothT = t * t * (3f - 2f * t);

                // Yay çizerek gitmeleri için (Shell game hissi) biraz Y offset verebiliriz ama düz swap yeterli şimdilik.
                card1.transform.localPosition = Vector3.Lerp(pos1, pos2, smoothT);
                card2.transform.localPosition = Vector3.Lerp(pos2, pos1, smoothT);

                yield return null;
            }

            // Pozisyonları tam oturt
            card1.transform.localPosition = pos2;
            card2.transform.localPosition = pos1;

            yield return new WaitForSeconds(0.1f); // Kısa bekleme
        }

        isShuffling = false;
    }

    public void OnCardSelected(DivinerCard selectedCard)
    {
        if (isShuffling) return;

        // Seçileni aç ve hafifçe öne çıkar
        selectedCard.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
        selectedCard.FlipFront();

        // Diğerlerini de aç ama pasif yap
        foreach (var card in activeCards)
        {
            card.button.interactable = false;
            if (card != selectedCard) card.FlipFront();
        }

        // --- DÜZELTME: HARİTAYA GERÇEK İKONU GÖNDER ---
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
            // Rastgele bir sonraki düğümü seç
            int randIndex = Random.Range(0, currentNode.nextNodes.Count);
            MapNode nextNode = currentNode.nextNodes[randIndex];

            // --- YENİ: BOSS KORUMASI ---
            // Eğer sıradaki düğüm Boss ise, onu değiştirme!
            // (Alternatif olarak: Başka bir nextNode dene veya hiçbir şey yapma)
            if (nextNode.pathType == PathType.Boss)
            {
                Debug.Log("Sıradaki durak Boss olduğu için kader değiştirilemedi.");
                return;
            }
            // ---------------------------

            nextNode.ChangeType(type, icon);
            Debug.Log($"Gelecek değişti! Sonraki durak: {type}");
        }
    }

    IEnumerator CloseDelay()
    {
        yield return new WaitForSeconds(1.8f);
        if (divinerPanel != null)
        {
            CanvasGroup cg = divinerPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.DOFade(0f, 0.25f);
            yield return new WaitForSeconds(0.25f);
            divinerPanel.SetActive(false);
        }
        PathManager.instance.CompleteCurrentNode();
    }

    IEnumerator ShowCardsPhase()
    {
        // 1. Kartları Temizle
        foreach (Transform child in cardsContainer) Destroy(child.gameObject);
        activeCards.Clear();

        // 2. Layout Aç
        HorizontalLayoutGroup layout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = true;

        // 3. Kartları Oluştur
        CreateCard(PathType.Combat, tarotCombatSprite);
        CreateCard(PathType.Companion, tarotCompanionSprite);
        CreateCard(PathType.Shop, tarotShopSprite);

        yield return new WaitForEndOfFrame();

        // 4. Layout Kapat
        if (layout != null) layout.enabled = false;

        // --- YENİ: Oyuncunun incelemesi için bekle (Butonu aç) ---
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }

        // Butona basılana kadar bekle...
    }
}

// Kart Yardımcı Sınıfı (Aynı kalıyor ama scriptin içine dahil ettim)
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

        img.sprite = icon; // İlk başta açık gelsin
        button.onClick.AddListener(() => manager.OnCardSelected(this));
        button.interactable = false;
    }

    public void FlipBack(Sprite back)
    {
        transform.DOKill();
        transform.DORotate(new Vector3(0, 90f, 0), 0.15f).OnComplete(() =>
        {
            if (img != null) img.sprite = back;
            transform.DORotate(Vector3.zero, 0.15f);
        });
    }

    public void FlipFront()
    {
        transform.DOKill();
        transform.DORotate(new Vector3(0, 90f, 0), 0.15f).OnComplete(() =>
        {
            if (img != null) img.sprite = frontSprite;
            transform.DORotate(Vector3.zero, 0.15f);
        });
    }
}