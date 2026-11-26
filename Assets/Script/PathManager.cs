using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager instance;

    [Header("Canvases / Panels")]
    public GameObject mapPanel;
    public GameObject combatPanel;
    public GameObject divinerPanel;
    public GameObject shopPanel;
    public GameObject companionPanel;

    public MapGenerator mapGenerator;

    [Header("Current State")]
    public MapNode currentNode;
    public bool isPeekingMap = false;

    // --- KRİTİK DÜZELTME: ÇİFT TIKLAMA VE SPAM KİLİDİ ---
    public bool isEventActive = false;

    [Header("UI Elements")]
    public GameObject closePeekMapButton;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
        }

        mapPanel.SetActive(false);
        combatPanel.SetActive(false);
        if (divinerPanel != null) divinerPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (companionPanel != null) companionPanel.SetActive(false);

        if (StartingDeckManager.instance != null)
        {
            StartingDeckManager.instance.OpenSelection();
        }
        else
        {
            ShowMap();
        }
    }

    public void ShowMap()
    {
        isPeekingMap = false;

        // HARİTAYA DÖNDÜĞÜMÜZDE KİLİDİ AÇIYORUZ
        isEventActive = false;

        if (AudioManager.instance != null) AudioManager.instance.PlayBGM(AudioManager.instance.mapBGM);

        mapPanel.SetActive(true);
        combatPanel.SetActive(false);
        if (divinerPanel != null) divinerPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (companionPanel != null) companionPanel.SetActive(false);

        if (closePeekMapButton != null) closePeekMapButton.SetActive(false);
    }

    public void OnNodeSelected(MapNode node)
    {
        // --- KRİTİK KONTROL ---
        // Eğer zaten bir düğüme tıklandıysa ve etkinlik açılıyorsa, ekstra tıklamaları reddet!
        if (isEventActive)
        {
            Debug.Log("[SİSTEM] Çift tıklama engellendi! Zaten bir patikaya giriliyor.");
            return;
        }

        // KİLİDİ KAPAT
        isEventActive = true;

        currentNode = node;
        mapPanel.SetActive(false);

        switch (node.realPathType)
        {
            case PathType.Combat:
            case PathType.Elite:
            case PathType.Boss:
                StartCombat(node.realPathType);
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
        }
    }

    void StartShopEvent()
    {
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            ShopManager.instance.OpenShop();
        }
    }

    void StartCombat(PathType type)
    {
        combatPanel.SetActive(true);
        TurnManager tm = TurnManager.instance != null ? TurnManager.instance : FindFirstObjectByType<TurnManager>();
        if (tm != null) tm.StartNewBattle(type);
    }

    void StartDivinerEvent()
    {
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (DivinerManager.instance != null) DivinerManager.instance.OpenDivinerEvent();
    }

    // Sınıfın üst kısımlarına (isPeekingMap altına) şu değişkeni ekle:
    private bool wasCombatActiveBeforePeek = false;

    // Fonksiyonu tamamen bununla değiştir:
    public void ToggleMapOverlay()
    {
        isPeekingMap = !isPeekingMap;

        if (isPeekingMap)
        {
            // --- YENİ: Haritaya bakarken combat panelini geçici gizle ---
            wasCombatActiveBeforePeek = combatPanel.activeSelf;
            if (wasCombatActiveBeforePeek) combatPanel.SetActive(false);

            mapPanel.SetActive(true);
            mapPanel.transform.SetAsLastSibling();

            if (closePeekMapButton != null) closePeekMapButton.SetActive(true);

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
            // --- YENİ: Haritayı kapatınca combat panelini geri aç ---
            if (wasCombatActiveBeforePeek) combatPanel.SetActive(true);

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

    public void CompleteCurrentNode()
    {
        if (PlayerManager.instance != null) PlayerManager.instance.pathsCompleted++;

        if (currentNode != null)
        {

            if (currentNode.realPathType == PathType.Boss)
            {
                if (GameOverManager.instance != null) GameOverManager.instance.TriggerVictory();
                return; // Haritayı açma, kodu burada kes.
            }

            currentNode.SetCompleted();

            if (mapGenerator != null)
            {
                List<MapNode> siblingNodes = mapGenerator.GetNodesInLayer(currentNode.gridLayer);
                foreach (MapNode sibling in siblingNodes)
                {
                    if (sibling != currentNode && !sibling.isCompleted)
                    {
                        sibling.SetSkipped();
                    }
                }

                mapGenerator.UpdateAllLineColors();
            }
        }

        ShowMap();
    }
}