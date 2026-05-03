using UnityEngine;

public enum ItemEffectType
{
    IncreaseMaxHealth,  // Maksimum caný kalýcý artýrýr
    BonusDamage,        // Tüm saldýrýlara kalýcý hasar ekler
    FullHeal,           // Caný tamamen doldurur (Tek kullanýmlýk)

    // --- YENÝ EKLENEN TÜRLER ---
    IncreaseMaxEnergy,  // Maksimum enerjiyi kalýcý artýrýr (Çok güçlü)
    IncreaseMaxHandSize,// Elde tutulabilen maksimum kart sayýsýný artýrýr
    GainGold,           // Parayla para almak? (Belki event'lerde kullanýlýr)
    GainCard            // Satýn alýnýnca desteye özel bir kart ekler
}

[CreateAssetMenu(fileName = "New Item", menuName = "Path of Elements/Shop Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public int cost;            // Altýn fiyatý

    [Header("Effect Settings")]
    public ItemEffectType effectType;
    public int effectValue;     // Örn: +10 Max HP, +2 Hasar, +1 Enerji

    [Header("Card Reward (If applicable)")]
    public CardData rewardCard; // Eðer bu eþya bir kart veriyorsa
}