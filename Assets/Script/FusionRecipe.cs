using UnityEngine;

[CreateAssetMenu(fileName = "New Fusion Recipe", menuName = "Path of Elements/Fusion Recipe")]
public class FusionRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Bileþenler (Hangi kartlar birleþecek?)")]
    public CardData cardA;
    public CardData cardB;

    [Header("Sonuç (Hangi kart çýkacak?)")]
    public CardData resultCard;

    [Header("Görsel Efekt (Opsiyonel)")]
    public GameObject fusionVFX; // Birleþme anýnda patlayacak efekt
}