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

    // --- YENÝ: Kartýn Güç Seviyesi (1, 2 veya 3) ---
    [Header("Power Level")]
    [Range(1, 3)]
    [Tooltip("Efekt seviyesini belirler (1=Zayýf, 2=Orta, 3=Güçlü)")]
    public int cardPower = 1;

    [Header("Modifier Settings")]
    [Tooltip("Eðer iþaretlenirse, solundaki karta destek çýkar.")]
    public bool isElementalModifier = false;

    [Header("Companion Settings")]
    public SpecialEffect specialEffect;

    [Header("Cost")]
    [Range(0, 5)]
    public int energyCost = 1;

    [Header("Support Settings")]
    public SupportEffect supportEffect;
    public int supportValue;

   

    [Header("Journal Info")]
    [TextArea(5, 10)] // Inspector'da büyük bir metin kutusu açar (5-10 satýr)
    public string journalDescription; // Günlükte görünen uzun hikaye
}