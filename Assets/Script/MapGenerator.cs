using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Config")]
    public GameObject nodePrefab;
    public GameObject linePrefab; // --- YENÝ: Düðümler arasýna çizilecek çizgi prefabý ---
    public Transform mapContainer;
    public Transform lineContainer; // --- YENÝ: Çizgilerin kalabalýk yapmamasý için ayrý klasör ---
    public int mapLength = 10;

    [Header("Line Settings")]
    public float dotSpacing = 25f; // Kesik çizgiler (noktalar) arasýndaki boþluk mesafesi

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
    public int minNodesPerLayer = 2; // --- YENÝ: Katman baþýna en az düðüm ---
    public int maxNodesPerLayer = 4; // --- YENÝ: Katman baþýna en fazla düðüm ---

    private List<List<MapNode>> mapGrid = new List<List<MapNode>>();

    public void GenerateMap()
    {
        // 1. Temizlik
        foreach (Transform child in mapContainer)
        {
            // Eðer çocuk obje LineContainer DEÐÝLSE sil (Böylece klasörümüz korunur)
            if (child != lineContainer) Destroy(child.gameObject);
        }

        if (lineContainer != null)
        {
            // Klasörün içindeki eski çizgileri/noktalarý sil
            foreach (Transform child in lineContainer) Destroy(child.gameObject);
        }
        mapGrid.Clear();

        // 2. Düðümleri (Nodes) Oluþtur
        for (int i = 0; i < mapLength; i++)
        {
            List<MapNode> currentLayerNodes = new List<MapNode>();
            int nodeCount = GetNodeCountForLayer(i);

            for (int j = 0; j < nodeCount; j++)
            {
                // Y ekseninde (yukarýdan aþaðýya) düðümleri ortalayarak diz
                float yPos = (j - (nodeCount - 1) / 2f) * ySpacing;

                // --- X ekseninde biraz zikzak/rastgelelik katalým (Doðal dursun) ---
                float randomXOffset = (i > 0 && i < mapLength - 1) ? Random.Range(-20f, 20f) : 0f;
                float xPos = (i * xSpacing) + startXPadding + randomXOffset;

                GameObject newNodeObj = Instantiate(nodePrefab, mapContainer);
                newNodeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

                MapNode nodeScript = newNodeObj.GetComponent<MapNode>();
                nodeScript.nextNodes = new List<MapNode>();
                nodeScript.parentNodes = new List<MapNode>();
                nodeScript.gridLayer = i;

                // Tür Belirleme
                PathType type = GetRandomPathType(i);

                if (type == PathType.Mystery)
                {
                    PathType hiddenType = GetRandomPathType(i, true);
                    nodeScript.SetupMystery(hiddenType, mysterySprite);
                }
                else
                {
                    nodeScript.pathType = type;
                    nodeScript.realPathType = type;
                    nodeScript.SetIcon(GetIconForType(type));
                }

                // Sadece Ýlk katman (Layer 0) açýk baþlar
                if (i == 0) nodeScript.UnlockNode();
                else nodeScript.isLocked = true;

                nodeScript.UpdateVisuals();
                currentLayerNodes.Add(nodeScript);
            }
            mapGrid.Add(currentLayerNodes);
        }

        // 3. Düðümleri Birbirine Baðla (Slay the Spire Mantýðý)
        ConnectLayers();

        // 4. Ýlerisi olmayan kör düðümleri temizle veya zorla baðla
        FixDeadEnds();

        // 5. Çizgileri Çiz
        DrawConnections();
    }

    int GetNodeCountForLayer(int layerIndex)
    {
        // Ýlk ve son katman (Boss) her zaman 1 düðüm olur
        if (layerIndex == 0) return 1;
        if (layerIndex == mapLength - 1) return 1;

        // Ara katmanlar 2 ile 4 arasýnda rastgele oluþur
        return Random.Range(minNodesPerLayer, maxNodesPerLayer + 1);
    }

    PathType GetRandomPathType(int layerIndex, bool excludeMystery = false)
    {
        if (layerIndex == 0) return PathType.Combat;
        if (layerIndex == mapLength - 1) return PathType.Boss;

        int currentMysteryChance = excludeMystery ? 0 : mysteryChance;
        int total = combatChance + eliteChance + shopChance + divinerChance + companionChance + currentMysteryChance;

        int rand = Random.Range(0, total);

        if (rand < combatChance) return PathType.Combat; rand -= combatChance;
        if (rand < eliteChance) return PathType.Elite; rand -= eliteChance;
        if (rand < shopChance) return PathType.Shop; rand -= shopChance;

        if (rand < divinerChance)
        {
            if (layerIndex < mapLength - 2 && layerIndex > 1) return PathType.Diviner;
            else return PathType.Combat;
        }
        rand -= divinerChance;

        if (rand < companionChance) return PathType.Companion; rand -= companionChance;

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

    // --- YENÝ: SLAY THE SPIRE BAÐLANTI ALGORÝTMASI ---
    void ConnectLayers()
    {
        for (int i = 0; i < mapGrid.Count - 1; i++)
        {
            List<MapNode> currentLayer = mapGrid[i];
            List<MapNode> nextLayer = mapGrid[i + 1];

            // Her katmandan sonraki katmana mantýklý bir bað kur
            for (int c = 0; c < currentLayer.Count; c++)
            {
                // Bulunduðumuz düðümün (c), bir sonraki katmandaki hedef (target) sýrasý
                // Basitçe: Yukarýdaki yukarýya, aþaðýdaki aþaðýya meyillidir
                int targetIndex = Mathf.Clamp(Mathf.RoundToInt((float)c / currentLayer.Count * nextLayer.Count), 0, nextLayer.Count - 1);

                Connect(currentLayer[c], nextLayer[targetIndex]);

                // Bazen fazladan bir yol (çapraz) yarat (%30 Ýhtimal)
                if (Random.value < 0.3f)
                {
                    int extraIndex = targetIndex + (Random.value > 0.5f ? 1 : -1);
                    // Çapraz baðýn dýþarý taþmamasýný kontrol et
                    if (extraIndex >= 0 && extraIndex < nextLayer.Count)
                    {
                        Connect(currentLayer[c], nextLayer[extraIndex]);
                    }
                }
            }

            // Geriye dönük kontrol: Sonraki katmanda HÝÇ ebeveyni olmayan (Havada kalan) düðüm var mý?
            for (int n = 0; n < nextLayer.Count; n++)
            {
                if (nextLayer[n].parentNodes.Count == 0)
                {
                    // Havada kalmýþ! Ona en yakýn mantýklý ebeveyni zorla baðla
                    int parentIndex = Mathf.Clamp(Mathf.RoundToInt((float)n / nextLayer.Count * currentLayer.Count), 0, currentLayer.Count - 1);
                    Connect(currentLayer[parentIndex], nextLayer[n]);
                }
            }
        }
    }

    void FixDeadEnds()
    {
        // Boss hariç hiçbir düðümün "Ýlerisi (nextNodes)" boþ olmamalý.
        for (int i = 0; i < mapGrid.Count - 1; i++)
        {
            foreach (MapNode node in mapGrid[i])
            {
                if (node.nextNodes.Count == 0)
                {
                    // Çýkmaz sokak oldu! Hemen bir sonraki katmandan rastgele birine baðla
                    List<MapNode> nextLayer = mapGrid[i + 1];
                    Connect(node, nextLayer[Random.Range(0, nextLayer.Count)]);
                }
            }
        }
    }

    void Connect(MapNode from, MapNode to)
    {
        if (from == null || to == null) return;

        // Zaten baðlýlarsa bir daha baðlama
        if (!from.nextNodes.Contains(to)) from.nextNodes.Add(to);
        if (!to.parentNodes.Contains(from)) to.parentNodes.Add(from);
    }

    // --- YENÝ: DÜÐÜMLER ARASINA ÇÝZGÝ ÇÝZME ---
    void DrawConnections()
    {
        if (linePrefab == null || lineContainer == null) return;

        foreach (List<MapNode> layer in mapGrid)
        {
            foreach (MapNode node in layer)
            {
                node.lineDots.Clear(); // Önce hafýzayý temizle

                foreach (MapNode nextNode in node.nextNodes)
                {
                    // Artýk RectTransform deðil, direkt Düðümleri (Node) gönderiyoruz
                    CreateLineBetween(node, nextNode);
                }
            }
        }

        UpdateAllLineColors(); // Harita ilk oluþtuðunda renkleri ayarla
    }

    
    void CreateLineBetween(MapNode startNode, MapNode endNode)
    {
        RectTransform start = startNode.GetComponent<RectTransform>();
        RectTransform end = endNode.GetComponent<RectTransform>();

        Vector2 diff = end.anchoredPosition - start.anchoredPosition;
        float distance = diff.magnitude;
        Vector2 direction = diff.normalized;

        // --- YENÝ KUSURSUZ DAÐILIM MANTIÐI ---
        // Toplam mesafeye tahmini kaç nokta sýðacaðýný yuvarlayarak buluyoruz
        int dotCount = Mathf.RoundToInt(distance / dotSpacing);
        if (dotCount < 2) dotCount = 2; // Çok yakýnsalar bile en az 1 nokta çizilsin

        // Noktalar arasý "gerçek" mesafeyi hesaplýyoruz ki sonda boþluk kalmasýn
        float exactSpacing = distance / dotCount;

        List<Image> dots = new List<Image>();

        for (int i = 1; i < dotCount; i++)
        {
            GameObject dotObj = Instantiate(linePrefab, lineContainer);
            RectTransform dotRect = dotObj.GetComponent<RectTransform>();

            // Sabit 25 birim yerine exactSpacing kullanýyoruz, böylece noktalar tam düðümün dibinde biter
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

                    // --- DÜZELTÝLEN AKTÝF YOL KURALI ---
                    bool isPathActive = false;

                    // 1. KURAL: Bir yolun parlamasý için kesinlikle BAÞLANGIÇ noktasýndan (node) geçmiþ olmalýyýz!
                    if (node.isCompleted)
                    {
                        // 2. KURAL: Eðer baþlangýçtan geçmiþsek;
                        if (targetNode.isCompleted)
                        {
                            // A) Hedefi de geçmiþiz (Geçmiþte yürüdüðümüz temiz rota)
                            isPathActive = true;
                        }
                        else if (!targetNode.isLocked && !targetNode.isSkipped)
                        {
                            // B) Hedefi henüz geçmedik ama önümüzde AÇIK bir seçenek olarak duruyor
                            isPathActive = true;
                        }
                    }

                    // Aktifse tam beyaz, kilitliyse/atlanmýþsa koyu gri
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