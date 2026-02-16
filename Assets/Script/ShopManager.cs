using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Configuration")]
    public List<ItemData> allPossibleItems; // Editörden tüm eþyalarý buraya at
    public int rerollCost = 10;             // Yenileme ücreti

    [Header("UI References")]
    public Transform itemsContainer;        // Eþyalarýn dizileceði panel (Grid Layout)
    public GameObject shopItemPrefab;       // Eþya butonu prefab'ý
    public TextMeshProUGUI rerollCostText;
    public TextMeshProUGUI playerGoldText;

    // Anlýk dükkandaki eþyalarý tutan liste
    private List<GameObject> currentShopSlots = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    // PathManager dükkaný açtýðýnda bu fonksiyonu çaðýracak
    public void OpenShop()
    {
        GenerateItems();
        UpdateUI();
    }

    // 3 Rastgele eþya oluþtur
    void GenerateItems()
    {
        // Önce eski slotlarý temizle
        foreach (GameObject slot in currentShopSlots) Destroy(slot);
        currentShopSlots.Clear();

        // 3 tane rastgele eþya seç
        for (int i = 0; i < 3; i++)
        {
            if (allPossibleItems.Count == 0) break;

            ItemData randomItem = allPossibleItems[Random.Range(0, allPossibleItems.Count)];
            CreateItemSlot(randomItem);
        }
    }

    // Eþyayý UI'da oluþtur
    void CreateItemSlot(ItemData item)
    {
        GameObject newSlot = Instantiate(shopItemPrefab, itemsContainer);
        currentShopSlots.Add(newSlot);

        // UI elemanlarýný bul ve doldur (Prefab yapýna göre burayý düzenle)
        // Örn: ShopItemUI diye bir script yapýp ona da atabilirsin ama þimdilik basit tutalým:
        // Hiyerarþi varsayýmý: Image(Icon) -> Text(Name) -> Text(Cost) -> Button(Buy)

        // Basit eriþim (Prefabýna ShopSlot scripti eklemen daha temiz olur, aþaðýda veriyorum)
        ShopSlot slotScript = newSlot.GetComponent<ShopSlot>();
        if (slotScript != null)
        {
            slotScript.Setup(item, this);
        }
    }

    // EÞYA SATIN ALMA
    public void TryBuyItem(ItemData item, GameObject slotObj)
    {
        if (PlayerManager.instance.currentGold >= item.cost)
        {
            // 1. Parayý düþ
            PlayerManager.instance.AddGold(-item.cost); // Negatif ekleyerek düþüyoruz

            // 2. Efekti uygula
            ApplyItemEffect(item);

            // 3. Eþyayý dükkandan kaldýr (Satýldý)
            Destroy(slotObj);

            UpdateUI();
            Debug.Log($"{item.itemName} satýn alýndý!");
        }
        else
        {
            Debug.Log("Yetersiz Bakiye!");
            // Buraya kýrmýzý yanýp sönme efekti ekleyebilirsin
        }
    }

    void ApplyItemEffect(ItemData item)
    {
        switch (item.effectType)
        {
            case ItemEffectType.IncreaseMaxHealth:
                PlayerManager.instance.maxHealth += item.effectValue;
                PlayerManager.instance.Heal(item.effectValue); // Artan can kadar iyileþtir
                break;

            case ItemEffectType.BonusDamage:
                PlayerManager.instance.bonusDamage += item.effectValue;
                break;

            case ItemEffectType.FullHeal:
                PlayerManager.instance.Heal(9999);
                break;
        }
    }

    // REROLL (YENÝLEME)
    public void RerollShop()
    {
        if (PlayerManager.instance.currentGold >= rerollCost)
        {
            PlayerManager.instance.AddGold(-rerollCost);
            GenerateItems();
            UpdateUI();

            // Her yenilemede fiyat artsýn istersen: rerollCost += 5;
        }
        else
        {
            Debug.Log("Yenilemek için paran yetmiyor!");
        }
    }

    // GEÇ / ÇIK BUTONU
    public void LeaveShop()
    {
        // Haritaya geri dön
        if (PathManager.instance != null)
        {
            PathManager.instance.CompleteCurrentNode();
        }
        
    }

    void UpdateUI()
    {
        if (playerGoldText != null && PlayerManager.instance != null)
            playerGoldText.text = PlayerManager.instance.currentGold.ToString() + " G";

        if (rerollCostText != null)
            rerollCostText.text = "ReRoll: " + rerollCost + " G";
    }
}