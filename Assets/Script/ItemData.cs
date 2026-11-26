using UnityEngine;

public enum ItemEffectType
{
    IncreaseMaxHealth,  // Maksimum canı kalıcı artırır
    BonusDamage,        // Tüm saldırılara kalıcı hasar ekler
    FullHeal,           // Canı tamamen doldurur (Tek kullanımlık)

    // --- YENİ EKLENEN TÜRLER ---
    IncreaseMaxEnergy,  // Maksimum enerjiyi kalıcı artırır (Çok güçlü)
    IncreaseMaxHandSize,// Elde tutulabilen maksimum kart sayısını artırır
    GainGold,           // Parayla para almak? (Belki event'lerde kullanılır)
    GainCard,           // Satın alınınca desteye özel bir kart ekler
    IncreaseDrawPerTurn // Her tur çekilen kart sayısını kalıcı artırır (sona eklendi: asset'lerdeki sayılar kaymasın)
}

[CreateAssetMenu(fileName = "New Item", menuName = "Path of Elements/Shop Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public int cost;            // Altın fiyatı

    [Header("Effect Settings")]
    public ItemEffectType effectType;
    public int effectValue;     // Örn: +10 Max HP, +2 Hasar, +1 Enerji

    [Header("Card Reward (If applicable)")]
    public CardData rewardCard; // Eğer bu eşya bir kart veriyorsa
}