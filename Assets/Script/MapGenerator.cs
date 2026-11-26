using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Config")]
    public GameObject nodePrefab;
    public GameObject linePrefab;
    public Transform mapContainer;
    public Transform lineContainer;
    public int mapLength = 10;

    [Header("Line Settings")]
    public float dotSpacing = 25f;

    [Header("Visual Settings")]
    public float startXPadding = 200f;

    // --- GÖRSELLER ---
    public Sprite combatSprite;
    public Sprite eliteSprite;
    public Sprite shopSprite;
    public Sprite divinerSprite;
    public Sprite bossSprite;
    public Sprite companionSprite;
    public Sprite mysterySprite;

    [Header("Probabilities (Total 100)")]
    public int combatChance = 40;
    public int eliteChance = 10;
    public int shopChance = 10;
    public int divinerChance = 10;
    public int companionChance = 15;
    public int mysteryChance = 15;

    [Header("Layout Settings")]
    public float xSpacing = 200f;
    public float ySpacing = 150f;
    public int minNodesPerLayer = 2;
    public int maxNodesPerLayer = 4;

    private List<List<MapNode>> mapGrid = new List<List<MapNode>>();

    public void GenerateMap()
    {
        // 1. Temizlik
        foreach (Transform child in mapContainer)
        {
            if (child != lineContainer) Destroy(child.gameObject);
        }

        if (lineContainer != null)
        {
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
        }
        mapGrid.Clear();

        // 2. Düğümleri (Nodes) Oluştur - TÜR ATAMASI YAPMADAN
        for (int i = 0; i < mapLength; i++)
        {
            List<MapNode> currentLayerNodes = new List<MapNode>();
            int nodeCount = GetNodeCountForLayer(i);

            for (int j = 0; j < nodeCount; j++)
            {
                float yPos = (j - (nodeCount - 1) / 2f) * ySpacing;
                float randomXOffset = (i > 0 && i < mapLength - 1) ? Random.Range(-20f, 20f) : 0f;
                float xPos = (i * xSpacing) + startXPadding + randomXOffset;

                GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
                newNodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

                MapNode nodeScript = newNodeObj.GetComponent<MapNode>();
                nodeScript.nextNodes = new List<MapNode>();
                nodeScript.parentNodes = new List<MapNode>();
                nodeScript.gridLayer = i;

                currentLayerNodes.Add(nodeScript);
            }
            mapGrid.Add(currentLayerNodes);
        }

        // 3. Düğümleri Birbirine Bağla
        ConnectLayers();

        // 4. Kör düğümleri düzelt
        FixDeadEnds();

        // 5. YENİ: BAĞLANTILARA GÖRE KONTROLLÜ TÜR ATAMASI YAP
        AssignPathTypes();

        // 6. Çizgileri Çiz
        DrawConnections();
    }

    int GetNodeCountForLayer(int layerIndex)
    {
        if (layerIndex == 0) return 1;
        if (layerIndex == mapLength - 1) return 1;
        return Random.Range(minNodesPerLayer, maxNodesPerLayer + 1);
    }

    // --- YENİ: DİNAMİK TÜR ATAMA SİSTEMİ ---
    void AssignPathTypes()
    {
        // Her düğümün arkasında kaç tane aralıksız "Savaş Dışı" patika bıraktığını tutar
        Dictionary<MapNode, int> nonCombatStreaks = new Dictionary<MapNode, int>();

        for (int i = 0; i < mapLength; i++)
        {
            foreach (MapNode node in mapGrid[i])
            {
                if (i == 0)
                {
                    node.pathType = PathType.Combat;
                    node.realPathType = PathType.Combat;
                    node.SetIcon(combatSprite);
                    nonCombatStreaks[node] = 0;
                    node.UnlockNode();
                }
                else if (i == mapLength - 1)
                {
                    node.pathType = PathType.Boss;
                    node.realPathType = PathType.Boss;
                    node.SetIcon(bossSprite);
                    nonCombatStreaks[node] = 0;
                    node.isLocked = true;
                }
                else
                {
                    bool forceCombat = false;
                    List<PathType> parentTypes = new List<PathType>();
                    int maxParentStreak = 0;

                    // Bu düğüme gelen yolların (Ebeveynlerin) geçmişini oku
                    foreach (MapNode parent in node.parentNodes)
                    {
                        parentTypes.Add(parent.realPathType);
                        int pStreak = nonCombatStreaks.ContainsKey(parent) ? nonCombatStreaks[parent] : 0;
                        if (pStreak > maxParentStreak) maxParentStreak = pStreak;
                    }

                    // KURAL 2: Eğer bu yola gelene kadar 2 kere üst üste savaş dışı (Shop, Diviner vb) girildiyse, SAVAŞI ZORUNLU KIL!
                    if (maxParentStreak >= 2) forceCombat = true;

                    // Türü Zar Atarak Belirle
                    PathType rolledType = RollPathType(parentTypes, forceCombat, i);

                    if (rolledType == PathType.Mystery)
                    {
                        // Gizemli kutunun içindekini de kurallara göre belirle
                        PathType hidden = RollPathType(parentTypes, forceCombat, i, true);
                        node.SetupMystery(hidden, mysterySprite);

                        // Gizli olan şey Combat ise seriyi boz (0'la), değilse seriyi artır
                        nonCombatStreaks[node] = (hidden == PathType.Combat || hidden == PathType.Elite) ? 0 : (maxParentStreak + 1);
                    }
                    else
                    {
                        node.pathType = rolledType;
                        node.realPathType = rolledType;
                        node.SetIcon(GetIconForType(rolledType));

                        // Rolled type combat ise seriyi sıfırla, değilse artır
                        nonCombatStreaks[node] = (rolledType == PathType.Combat || rolledType == PathType.Elite) ? 0 : (maxParentStreak + 1);
                    }

                    node.isLocked = true;
                }

                node.UpdateVisuals();
            }
        }
    }

    // --- YENİ: AĞIRLIKLI VE KONTROLLÜ ZAR ATMA SİSTEMİ ---
    PathType RollPathType(List<PathType> excludeTypes, bool forceCombat, int layerIndex, bool excludeMystery = false)
    {
        int cWeight = combatChance;
        int eWeight = eliteChance;
        int sWeight = shopChance;
        int dWeight = divinerChance;
        int compWeight = companionChance;
        int mWeight = (excludeMystery || forceCombat) ? 0 : mysteryChance;

        if (forceCombat)
        {
            // Kural: Savaş zorunluysa, diğer her şeyin ihtimalini 0'a çek
            sWeight = 0; dWeight = 0; compWeight = 0; mWeight = 0;
        }
        else
        {
            // KURAL 1: Ard arda aynı "Savaş Dışı" patikanın gelmesini engelle. 
            // Örneğin bir önceki düğüm Shop ise, ihtimalini 0'a çek.
            if (excludeTypes.Contains(PathType.Shop)) sWeight = 0;
            if (excludeTypes.Contains(PathType.Diviner)) dWeight = 0;
            if (excludeTypes.Contains(PathType.Companion)) compWeight = 0;
            if (excludeTypes.Contains(PathType.Mystery)) mWeight = 0;

            // Not: Savaş veya Elite ard arda gelebilir, roguelike mantığında bu normaldir.
        }

        // Diviner sadece haritanın orta katmanlarında çıkmalı
        if (layerIndex <= 1 || layerIndex >= mapLength - 2) dWeight = 0;

        // Kalan ihtimalleri topla
        int totalWeight = cWeight + eWeight + sWeight + dWeight + compWeight + mWeight;

        // Failsafe (Eğer her şey 0 olduysa güvenli liman olarak Combat ver)
        if (totalWeight <= 0) return PathType.Combat;

        // Dinamik zar atışı
        int rand = Random.Range(0, totalWeight);

        if (rand < cWeight) return PathType.Combat; rand -= cWeight;
        if (rand < eWeight) return PathType.Elite; rand -= eWeight;
        if (rand < sWeight) return PathType.Shop; rand -= sWeight;
        if (rand < dWeight) return PathType.Diviner; rand -= dWeight;
        if (rand < compWeight) return PathType.Companion; rand -= compWeight;

        return PathType.Mystery;
    }

    Sprite GetIconForType(PathType type)
    {
        switch (type)
        {
            case PathType.Combat: return combatSprite;
            case PathType.Elite: return eliteSprite;
            case PathType.Shop: return shopSprite;
            case PathType.Diviner: return divinerSprite;
            case PathType.Boss: return bossSprite;
            case PathType.Companion: return companionSprite;
            case PathType.Mystery: return mysterySprite;
            default: return null;
        }
    }

    void ConnectLayers()
    {
        for (int i = 0; i < mapGrid.Count - 1; i++)
        {
            List<MapNode> currentLayer = mapGrid[i];
            List<MapNode> nextLayer = mapGrid[i + 1];

            for (int c = 0; c < currentLayer.Count; c++)
            {
                int targetIndex = Mathf.Clamp(Mathf.RoundToInt((float)c / currentLayer.Count * nextLayer.Count), 0, nextLayer.Count - 1);
                Connect(currentLayer[c], nextLayer[targetIndex]);

                if (Random.value < 0.3f)
                {
                    int extraIndex = targetIndex + (Random.value > 0.5f ? 1 : -1);
                    if (extraIndex >= 0 && extraIndex < nextLayer.Count)
                    {
                        Connect(currentLayer[c], nextLayer[extraIndex]);
                    }
                }
            }

            for (int n = 0; n < nextLayer.Count; n++)
            {
                if (nextLayer[n].parentNodes.Count == 0)
                {
                    int parentIndex = Mathf.Clamp(Mathf.RoundToInt((float)n / nextLayer.Count * currentLayer.Count), 0, currentLayer.Count - 1);
                    Connect(currentLayer[parentIndex], nextLayer[n]);
                }
            }
        }
    }

    void FixDeadEnds()
    {
        for (int i = 0; i < mapGrid.Count - 1; i++)
        {
            foreach (MapNode node in mapGrid[i])
            {
                if (node.nextNodes.Count == 0)
                {
                    List<MapNode> nextLayer = mapGrid[i + 1];
                    Connect(node, nextLayer[Random.Range(0, nextLayer.Count)]);
                }
            }
        }
    }

    void Connect(MapNode from, MapNode to)
    {
        if (from == null || to == null) return;
        if (!from.nextNodes.Contains(to)) from.nextNodes.Add(to);
        if (!to.parentNodes.Contains(from)) to.parentNodes.Add(from);
    }

    void DrawConnections()
    {
        if (linePrefab == null || lineContainer == null) return;

        foreach (List<MapNode> layer in mapGrid)
        {
            foreach (MapNode node in layer)
            {
                node.lineDots.Clear();

                foreach (MapNode nextNode in node.nextNodes)
                {
                    CreateLineBetween(node, nextNode);
                }
            }
        }

        UpdateAllLineColors();
    }

    void CreateLineBetween(MapNode startNode, MapNode endNode)
    {
        RectTransform start = startNode.GetComponent<RectTransform>();
        RectTransform end = endNode.GetComponent<RectTransform>();

        Vector2 diff = end.anchoredPosition - start.anchoredPosition;
        float distance = diff.magnitude;
        Vector2 direction = diff.normalized;

        int dotCount = Mathf.RoundToInt(distance / dotSpacing);
        if (dotCount < 2) dotCount = 2;

        float exactSpacing = distance / dotCount;

        List<Image> dots = new List<Image>();

        for (int i = 1; i < dotCount; i++)
        {
            GameObject dotObj = Instantiate(linePrefab, lineContainer);
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();

            Vector2 dotPosition = start.anchoredPosition + (direction * (i * exactSpacing));
            dotRect.anchoredPosition = dotPosition;

            dotObj.transform.SetAsFirstSibling();

            Image dotImage = dotObj.GetComponent<Image>();
            if (dotImage != null) dots.Add(dotImage);
        }

        startNode.lineDots.Add(endNode, dots);
    }

    public void UpdateAllLineColors()
    {
        foreach (List<MapNode> layer in mapGrid)
        {
            foreach (MapNode node in layer)
            {
                foreach (var kvp in node.lineDots)
                {
                    MapNode targetNode = kvp.Key;
                    List<Image> dots = kvp.Value;

                    bool isPathActive = false;

                    if (node.isCompleted)
                    {
                        if (targetNode.isCompleted)
                        {
                            isPathActive = true;
                        }
                        else if (!targetNode.isLocked && !targetNode.isSkipped)
                        {
                            isPathActive = true;
                        }
                    }

                    Color pathColor = isPathActive ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.4f);

                    foreach (Image dot in dots)
                    {
                        if (dot != null) dot.color = pathColor;
                    }
                }
            }
        }
    }

    public List<MapNode> GetNodesInLayer(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < mapGrid.Count) return mapGrid[layerIndex];
        return new List<MapNode>();
    }
}