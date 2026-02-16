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
    public MapNode currentNode;         // Þu an hangi düðümdeyiz?

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

        ShowMap();
        // Baþlangýçta sadece Harita açýk olsun (Test için)

    }

    // Haritayý Açar, diðer her þeyi kapatýr
    public void ShowMap()
    {
        mapPanel.SetActive(true);
        combatPanel.SetActive(false);
        divinerPanel.SetActive(false);
        shopPanel.SetActive(false);
        companionPanel.SetActive(false);
        if (divinerPanel != null) divinerPanel.SetActive(false);
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