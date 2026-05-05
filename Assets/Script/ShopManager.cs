using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("Configuration")]
    public List<ItemData> allPossibleItems;
    public int rerollCost = 10;

    [Header("UI References")]
    public Transform itemsContainer;
    public GameObject shopItemPrefab;
    public TextMeshProUGUI rerollCostText;
    public TextMeshProUGUI playerGoldText;

    private List<GameObject> currentShopSlots = new List<GameObject>();

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void OpenShop()
    {
        GenerateItems();
        UpdateUI();
    }

    void GenerateItems()
    {
        foreach (GameObject slot in currentShopSlots) Destroy(slot);
        currentShopSlots.Clear();

        // Rastgele 3 eþya seçimi
        // (Eðer listede 3'ten az eþya varsa hata vermemesi için koruma)
        int itemsToSpawn = Mathf.Min(3, allPossibleItems.Count);

        List<ItemData> availablePool = new List<ItemData>(allPossibleItems);

        for (int i = 0; i < itemsToSpawn; i++)
        {
            int randomIndex = Random.Range(0, availablePool.Count);
            ItemData randomItem = availablePool[randomIndex];

            CreateItemSlot(randomItem);

            // Ayný eþyadan dükkanda 2 tane çýkmamasý için havuzdan çýkar
            availablePool.RemoveAt(randomIndex);
        }
    }

    void CreateItemSlot(ItemData item)
    {
        GameObject newSlot = Instantiate(shopItemPrefab, itemsContainer);
        currentShopSlots.Add(newSlot);

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
            PlayerManager.instance.AddGold(-item.cost);

            // 2. Efekti uygula
            ApplyItemEffect(item);

            // 3. Eþyayý dükkandan kaldýr
            Destroy(slotObj);
            currentShopSlots.Remove(slotObj);

            UpdateUI();
            Debug.Log($"[SHOP] {item.itemName} satýn alýndý!");
        }
        else
        {
            Debug.Log("[SHOP] Yetersiz Bakiye!");
            // (Ýleride buraya ui titremesi veya kýrmýzý uyarý yazýsý koyabilirsin)
        }
    }

    // --- GÜNCELLENEN KISIM: YENÝ EFEKTLERÝN ÇALIÞMA MANTIÐI ---
    void ApplyItemEffect(ItemData item)
    {
        switch (item.effectType)
        {
            case ItemEffectType.IncreaseMaxHealth:
                PlayerManager.instance.maxHealth += item.effectValue;
                PlayerManager.instance.Heal(item.effectValue);
                break;

            case ItemEffectType.BonusDamage:
                PlayerManager.instance.bonusDamage += item.effectValue;
                break;

            case ItemEffectType.FullHeal:
                PlayerManager.instance.Heal(9999);
                break;

            case ItemEffectType.IncreaseMaxEnergy:
                if (EnergyManager.instance != null)
                {
                    EnergyManager.instance.IncreaseMaxEnergy(item.effectValue);
                }
                break;

            case ItemEffectType.IncreaseMaxHandSize:
                // --- STREAMLINED FIX ---
                if (DeckManager.instance != null)
                {
                    DeckManager.instance.maxHandSize += item.effectValue;
                    Debug.Log($"[SHOP] Max Hand Size increased to: {DeckManager.instance.maxHandSize}");
                }
                else
                {
                    Debug.LogError("[SHOP ERROR] DeckManager instance is NULL! Cannot increase hand size.");
                }
                break;

            case ItemEffectType.GainCard:
                if (item.rewardCard != null)
                {
                    if (DeckManager.instance != null)
                    {
                        DeckManager.instance.allCardsInDeck.Add(item.rewardCard);
                        Debug.Log($"[SHOP] Added new card to deck: {item.rewardCard.cardName}");
                    }
                }
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

            // Opsiyonel: Her yenilemede fiyat artsýn (Hardcore roguelike hissi)
            // rerollCost += 5; 

            UpdateUI();
        }
        else
        {
            Debug.Log("[SHOP] Yenilemek için paran yetmiyor!");
        }
    }

    public void LeaveShop()
    {
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