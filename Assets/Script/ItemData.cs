using UnityEngine;

public enum ItemEffectType
{
    IncreaseMaxHealth,  // Maksimum caný kalýcý artýrýr
    BonusDamage,        // Tüm saldýrýlara kalýcý hasar ekler
    FullHeal            // Caný tamamen doldurur (Tek kullanýmlýk)
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
    public int effectValue;     // Örn: +10 Max HP, +2 Hasar
}