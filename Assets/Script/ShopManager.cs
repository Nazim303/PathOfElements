using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

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

        // Rastgele 3 eşya seçimi
        // (Eğer listede 3'ten az eşya varsa hata vermemesi için koruma)
        int itemsToSpawn = Mathf.Min(3, allPossibleItems.Count);

        List<ItemData> availablePool = new List<ItemData>(allPossibleItems);

        for (int i = 0; i < itemsToSpawn; i++)
        {
            int randomIndex = Random.Range(0, availablePool.Count);
            ItemData randomItem = availablePool[randomIndex];

            CreateItemSlot(randomItem);

            // Aynı eşyadan dükkanda 2 tane çıkmaması için havuzdan çıkar
            availablePool.RemoveAt(randomIndex);
        }
    }

    void CreateItemSlot(ItemData item)
    {
        GameObject newSlot = Instantiate(shopItemPrefab, itemsContainer);
        currentShopSlots.Add(newSlot);

        newSlot.transform.localScale = Vector3.zero;
        newSlot.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        ShopSlot slotScript = newSlot.GetComponent<ShopSlot>();
        if (slotScript != null)
        {
            slotScript.Setup(item, this);
        }
    }

    // EŞYA SATIN ALMA
    public void TryBuyItem(ItemData item, GameObject slotObj)
    {
        if (PlayerManager.instance.currentGold >= item.cost)
        {
            // 1. Parayı düş
            PlayerManager.instance.AddGold(-item.cost);
            if (AudioManager.instance != null) AudioManager.instance.PlayPurchase();

            // 2. Efekti uygula
            ApplyItemEffect(item);

            // 3. Eşyayı dükkandan kaldır (animasyonlu)
            currentShopSlots.Remove(slotObj);
            if (slotObj != null)
            {
                slotObj.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    Destroy(slotObj);
                });
            }

            UpdateUI();
            Debug.Log($"[SHOP] {item.itemName} satın alındı!");
        }
        else
        {
            Debug.Log("[SHOP] Yetersiz Bakiye!");
            if (slotObj != null)
            {
                slotObj.transform.DOComplete();
                slotObj.transform.DOShakePosition(0.35f, new Vector3(8f, 0, 0), 10, 90f, false, true);
            }
            if (playerGoldText != null)
            {
                playerGoldText.transform.DOComplete();
                playerGoldText.transform.DOShakePosition(0.3f, new Vector3(6f, 0, 0), 10, 90f, false, true);
            }
        }
    }

    // --- GÜNCELLENEN KISIM: YENİ EFEKTLERİN ÇALIŞMA MANTIĞI ---
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

            case ItemEffectType.GainGold:
                if (PlayerManager.instance != null)
                {
                    PlayerManager.instance.AddGold(item.effectValue);
                    Debug.Log($"[SHOP] Added gold: {item.effectValue} G");
                }
                break;

            case ItemEffectType.IncreaseDrawPerTurn:
                if (DeckManager.instance != null)
                {
                    DeckManager.instance.drawPerTurn += item.effectValue;
                    Debug.Log($"[SHOP] Draw per turn: {DeckManager.instance.drawPerTurn}");
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

    // REROLL (YENİLEME)
    public void RerollShop()
    {
        if (PlayerManager.instance.currentGold >= rerollCost)
        {
            PlayerManager.instance.AddGold(-rerollCost);
            GenerateItems();

            // Opsiyonel: Her yenilemede fiyat artsın (Hardcore roguelike hissi)
            // rerollCost += 5; 

            UpdateUI();
        }
        else
        {
            Debug.Log("[SHOP] Yenilemek için paran yetmiyor!");
            if (rerollCostText != null)
            {
                rerollCostText.transform.DOComplete();
                rerollCostText.transform.DOShakePosition(0.35f, new Vector3(8f, 0, 0), 10, 90f, false, true);
            }
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