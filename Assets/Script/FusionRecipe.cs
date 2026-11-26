using UnityEngine;

[CreateAssetMenu(fileName = "New Fusion Recipe", menuName = "Path of Elements/Fusion Recipe")]
public class FusionRecipe : ScriptableObject
{
    public string recipeName;

    [Header("Bileşenler (Hangi kartlar birleşecek?)")]
    public CardData cardA;
    public CardData cardB;

    [Header("Sonuç (Hangi kart çıkacak?)")]
    public CardData resultCard;

    [Header("Görsel Efekt (Opsiyonel)")]
    public GameObject fusionVFX; // Birleşme anında patlayacak efekt
}