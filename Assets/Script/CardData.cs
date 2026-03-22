using UnityEngine;

public enum CardType
{
    Attack,     // Standart Saldýrý
    Heal,       // Standart Þifa
    Companion,  // Yoldaþ
    Support     // --- YENÝ: Destek Kartý ---
}

public enum SpecialEffect
{
    None,
    HalveEnemyHealth,
    FullPlayerHeal,
    AOE_Damage
}

// --- YENÝ: Destek Kartý Efektleri ---
// --- YENÝ: Destek Kartý Efektleri ---
public enum SupportEffect
{
    DrawExtraCards,
    DiscardHandAndDraw,
    GainGold,
    HealPlayer,
    GainEnergy,
    GainShield,
    ChangeTerrain       
}

[CreateAssetMenu(fileName = "New Card", menuName = "Path of Elements/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Visuals")]
    public string cardName;
    public Sprite cardArt;
    public Sprite cardbander;
    [TextArea] public string description;

    [Header("Stats")]
    public CardType cardType;
    public ElementType element;

    public int baseDamage;
    public int healAmount;

    [Header("Companion Settings")]
    public SpecialEffect specialEffect;

    [Header("Cost")]
    [Range(0, 5)]
    public int energyCost = 1; // Varsayýlan 1 enerji

    [Header("Support Settings")]
    public SupportEffect supportEffect; // --- YENÝ ---
    public int supportValue;            // --- YENÝ: Kaç kart? Kaç altýn? ---
}