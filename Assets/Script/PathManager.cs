using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager instance;

    [Header("Canvases / Panels")]
    public GameObject mapPanel;         // Harita Ekraný
    public GameObject combatPanel;      // Savaþ Ekraný (Mevcut oyunun)
    public GameObject divinerPanel;        // Can Yenileme Ekraný
    public GameObject shopPanel; // --- YENÝ: Inspector'da Shop Panelini ata ---
    public GameObject companionPanel; // --- YENÝ ---
    // Ýleride: public GameObject shopPanel;

    public MapGenerator mapGenerator; // Inspector'dan baðla

    [Header("Current State")]
    public MapNode currentNode;
    public bool isPeekingMap = false; // --- YENÝ: Þu an haritaya sadece bakýyor muyuz? ---

    [Header("UI Elements")]
    public GameObject closePeekMapButton; // --- YENÝ: Haritayý kapatma butonu (Sadece Peek modunda görünür) ---


    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // Oyunu baþlatýrken haritayý oluþtur
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
        }

        // Önce her þeyi gizle
        mapPanel.SetActive(false);
        combatPanel.SetActive(false);
        if (divinerPanel != null) divinerPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (companionPanel != null) companionPanel.SetActive(false);

        // --- YENÝ EKLENEN: Baþlangýç destesi seçimini aç ---
        if (StartingDeckManager.instance != null)
        {
            StartingDeckManager.instance.OpenSelection();
        }
        else
        {
            ShowMap(); // Eðer script sahnede yoksa direkt haritayý aç (Hata önleyici)
        }
    }

    // Haritayý Açar, diðer her þeyi kapatýr
    public void ShowMap()
    {
        isPeekingMap = false;
        mapPanel.SetActive(true);
        combatPanel.SetActive(false);
        if (divinerPanel != null) divinerPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (companionPanel != null) companionPanel.SetActive(false);

        // --- YENÝ: Normal harita seçimindeysek kapatma butonunu gizle ---
        if (closePeekMapButton != null) closePeekMapButton.SetActive(false);
    }

    // Düðüme týklandýðýnda çalýþýr
    public void OnNodeSelected(MapNode node)
    {
        currentNode = node;
        mapPanel.SetActive(false);

        // --- DÜZELTME: Görünen türe deðil, GERÇEK türe (realPathType) bak ---
        // Mystery düðümüne týklanýnca içindeki (örn: Shop) çalýþacak.
        // Normal düðümlerde zaten ikisi aynýdýr.
        switch (node.realPathType)
        {
            case PathType.Combat:
            case PathType.Elite:
            case PathType.Boss:
                StartCombat(node.realPathType); // Boss ise ona göre zorluk ayarlarýz
                break;

            case PathType.Diviner:
                StartDivinerEvent();
                break;

            case PathType.Shop:
                StartShopEvent();
                break;

            case PathType.Companion:
                StartCompanionEvent();
                break;

                // Mystery case'ine gerek yok çünkü realPathType asla Mystery olmaz, 
                // Mystery setup edilirken içine baþka bir tür atanýr.
        }
    }

    void StartShopEvent()
    {
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            // ShopManager'ý tetikle
            ShopManager.instance.OpenShop();
        }
        Debug.Log("Dükkandasýn. Paraný harca...");
    }

    void StartCombat(PathType type)
    {
        combatPanel.SetActive(true);

        // TurnManager'a savaþý baþlatmasýný söyle
        // (Boss ise ona göre setup yapýlabilir)
        FindObjectOfType<TurnManager>().StartNewBattle(type);
    }

    void StartDivinerEvent()
    {
        // Enerji Bonusu
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (divinerPanel != null)
        {
            // Paneli DivinerManager kendi içinde açýyor ama garanti olsun
            // DivinerManager.instance.OpenDivinerEvent();
        }
        // Eðer DivinerManager sahnede yoksa hata vermesin diye kontrol et
        if (DivinerManager.instance != null) DivinerManager.instance.OpenDivinerEvent();
    }

    // --- YENÝ: Haritaya sadece göz atmak için ---
    public void ToggleMapOverlay()
    {
        isPeekingMap = !isPeekingMap;

        if (isPeekingMap)
        {
            mapPanel.SetActive(true);
            mapPanel.transform.SetAsLastSibling();

            // --- YENÝ: Sadece bakmak için açtýysak, haritayý kapatma butonunu göster ---
            if (closePeekMapButton != null) closePeekMapButton.SetActive(true);

            // Sýradaki patikalarý önizleme için "Görünür" yap (Önceki yazdýðýmýz kod)
            if (currentNode != null && currentNode.nextNodes != null)
            {
                foreach (MapNode next in currentNode.nextNodes)
                {
                    if (next != null)
                    {
                        next.isPreviewVisible = true;
                        next.UpdateVisuals();
                    }
                }
            }
        }
        else
        {
            // Harita kapandýðýnda önizleme modunu temizle
            if (currentNode != null && currentNode.nextNodes != null)
            {
                foreach (MapNode next in currentNode.nextNodes)
                {
                    if (next != null)
                    {
                        next.isPreviewVisible = false;
                        next.UpdateVisuals();
                    }
                }
            }

            if (currentNode != null && !currentNode.isCompleted)
            {
                mapPanel.SetActive(false);

                // --- YENÝ: Harita kapandýðýnda butonu da gizle ---
                if (closePeekMapButton != null) closePeekMapButton.SetActive(false);
            }
        }
    }
    void StartCompanionEvent()
    {
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (companionPanel != null)
        {
            companionPanel.SetActive(true);
            CompanionEvent.instance.OpenEvent();
        }
    }

    // Savaþ veya Olay bitince çaðrýlacak fonksiyon
    // PathManager.cs içine ekle/güncelle:

 

    // Bir düðümü tamamlayýp haritaya dönünce
    public void CompleteCurrentNode()
    {
        if (currentNode != null)
        {
            currentNode.SetCompleted();

            // --- YENÝ: KARDEÞ DÜÐÜMLERÝ KAPATMA MANTIÐI ---
            // Eðer ayný katmanda (Layer) baþka düðümler varsa, onlarý "Atlandý" (Skipped) yap.
            // Böylece oyuncu 2 yoldan birini seçtiðinde diðeri ölür.

            if (mapGenerator != null)
            {
                List<MapNode> siblingNodes = mapGenerator.GetNodesInLayer(currentNode.gridLayer);
                foreach (MapNode sibling in siblingNodes)
                {
                    // Kendisi deðilse ve tamamlanmadýysa kapat
                    if (sibling != currentNode && !sibling.isCompleted)
                    {
                        sibling.SetSkipped();
                    }
                }
            }
            // ------------------------------------------------
        }

        ShowMap();
    }
}