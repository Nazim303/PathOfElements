using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Config")]
    public GameObject nodePrefab;
    public Transform mapContainer;
    public int mapLength = 10;

    [Header("Visual Settings")]
    public float startXPadding = 200f;

    // --- GÖRSELLER ---
    public Sprite combatSprite;
    public Sprite eliteSprite;
    public Sprite shopSprite;
    public Sprite divinerSprite;
    public Sprite bossSprite;
    public Sprite companionSprite;
    public Sprite mysterySprite; // --- YENÝ: Soru Ýþareti Ýkonu ---

    [Header("Probabilities (Total 100)")]
    public int combatChance = 40; // Biraz azalttým
    public int eliteChance = 10;
    public int shopChance = 10;
    public int divinerChance = 10;
    public int companionChance = 15;
    public int mysteryChance = 15; // --- YENÝ: Gizem Ýhtimali ---

    [Header("Layout Settings")]
    public float xSpacing = 200f;
    public float ySpacing = 150f;

    private List<List<MapNode>> mapGrid = new List<List<MapNode>>();

    public void GenerateMap()
    {
        foreach (Transform child in mapContainer) Destroy(child.gameObject);
        mapGrid.Clear();

        for (int i = 0; i < mapLength; i++)
        {
            List<MapNode> currentLayerNodes = new List<MapNode>();
            int nodeCount = GetNodeCountForLayer(i);

            for (int j = 0; j < nodeCount; j++)
            {
                float yPos = (j - (nodeCount - 1) / 2f) * ySpacing;
                float xPos = (i * xSpacing) + startXPadding;

                GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
                newNodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

                MapNode nodeScript = newNodeObj.GetComponent<MapNode>();

                nodeScript.nextNodes = new List<MapNode>();
                nodeScript.parentNodes = new List<MapNode>();
                nodeScript.gridLayer = i;

                // Türü belirle
                PathType type = GetRandomPathType(i);

                // --- GÝZEM MANTIÐI ---
                if (type == PathType.Mystery)
                {
                    // Gizemliyse, içine rastgele baþka bir tür sakla (Boss hariç)
                    PathType hiddenType = GetRandomPathType(i, true); // true = Mystery hariç getir
                    nodeScript.SetupMystery(hiddenType, mysterySprite);
                }
                else
                {
                    // Normal düðüm
                    nodeScript.pathType = type;
                    nodeScript.realPathType = type; // Gerçeði de aynýsý
                    nodeScript.SetIcon(GetIconForType(type));
                }
                // ---------------------

                if (i == 0) nodeScript.UnlockNode();
                else nodeScript.isLocked = true;

                nodeScript.UpdateVisuals();
                currentLayerNodes.Add(nodeScript);
            }
            mapGrid.Add(currentLayerNodes);
        }

        ConnectLayers();
    }

    int GetNodeCountForLayer(int layerIndex)
    {
        if (layerIndex == 0) return 1;
        if (layerIndex == mapLength - 1) return 1;
        return Random.value > 0.4f ? 2 : 1;
    }

    // excludeMystery: Gizemli düðümün içindeki gizemli düðüm olmasýn diye kontrol
    // MapGenerator.cs içinde bu fonksiyonu bul ve deðiþtir:

    PathType GetRandomPathType(int layerIndex, bool excludeMystery = false)
    {
        if (layerIndex == 0) return PathType.Combat;
        if (layerIndex == mapLength - 1) return PathType.Boss;

        int currentMysteryChance = excludeMystery ? 0 : mysteryChance;
        int total = combatChance + eliteChance + shopChance + divinerChance + companionChance + currentMysteryChance;

        int rand = Random.Range(0, total);

        // COMBAT
        if (rand < combatChance) return PathType.Combat;
        rand -= combatChance;

        // ELITE
        if (rand < eliteChance) return PathType.Elite;
        rand -= eliteChance;

        // SHOP
        if (rand < shopChance) return PathType.Shop;
        rand -= shopChance;

        // DIVINER (FALCI) - KISITLAMA BURADA
        if (rand < divinerChance)
        {
            // KURAL: Ýlk katmandan hemen sonra (1) ve Boss'tan hemen önce (mapLength - 2) çýkmasýn.
            // Çünkü bu katmanlar "Sonraki Düðümü Deðiþtirme" yeteneði için kritik veya anlamsýzdýr.
            // Boss'u deðiþtirmek istemeyiz.

            if (layerIndex < mapLength - 2 && layerIndex > 1)
            {
                return PathType.Diviner;
            }
            else
            {
                return PathType.Combat; // Kurala uymazsa Savaþ olsun
            }
        }
        rand -= divinerChance;

        // COMPANION
        if (rand < companionChance) return PathType.Companion;
        rand -= companionChance;

        // MYSTERY
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

            if (currentLayer.Count == 1 && nextLayer.Count == 1) Connect(currentLayer[0], nextLayer[0]);
            else if (currentLayer.Count == 1 && nextLayer.Count == 2) { Connect(currentLayer[0], nextLayer[0]); Connect(currentLayer[0], nextLayer[1]); }
            else if (currentLayer.Count == 2 && nextLayer.Count == 1) { Connect(currentLayer[0], nextLayer[0]); Connect(currentLayer[1], nextLayer[0]); }
            else if (currentLayer.Count == 2 && nextLayer.Count == 2) { Connect(currentLayer[0], nextLayer[0]); Connect(currentLayer[1], nextLayer[1]); }
        }
    }

    void Connect(MapNode from, MapNode to)
    {
        if (from == null || to == null) return;
        from.nextNodes.Add(to);
        to.parentNodes.Add(from);
    }

    public List<MapNode> GetNodesInLayer(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < mapGrid.Count) return mapGrid[layerIndex];
        return new List<MapNode>();
    }
}