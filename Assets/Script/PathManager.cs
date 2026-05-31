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

    // --- KRÝTÝK DÜZELTME: ÇÝFT TIKLAMA VE SPAM KÝLÝDÝ ---
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

        // HARÝTAYA DÖNDÜÐÜMÜZDE KÝLÝDÝ AÇIYORUZ
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
        // --- KRÝTÝK KONTROL ---
        // Eðer zaten bir düðüme týklandýysa ve etkinlik açýlýyorsa, ekstra týklamalarý reddet!
        if (isEventActive)
        {
            Debug.Log("[SÝSTEM] Çift týklama engellendi! Zaten bir patikaya giriliyor.");
            return;
        }

        // KÝLÝDÝ KAPAT
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
        FindObjectOfType<TurnManager>().StartNewBattle(type);
    }

    void StartDivinerEvent()
    {
        if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(2);

        if (DivinerManager.instance != null) DivinerManager.instance.OpenDivinerEvent();
    }

    // Sýnýfýn üst kýsýmlarýna (isPeekingMap altýna) þu deðiþkeni ekle:
    private bool wasCombatActiveBeforePeek = false;

    // Fonksiyonu tamamen bununla deðiþtir:
    public void ToggleMapOverlay()
    {
        isPeekingMap = !isPeekingMap;

        if (isPeekingMap)
        {
            // --- YENÝ: Haritaya bakarken combat panelini geçici gizle ---
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
            // --- YENÝ: Haritayý kapatýnca combat panelini geri aç ---
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