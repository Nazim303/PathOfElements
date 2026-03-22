using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Path of Elements/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHealth;
    public ElementType element;
    public Sprite enemyArt;
    public Sprite enemybander;
    public int damage;      // Düþmanýn tur baþýna vurduðu hasar
    public int goldReward;  // Yendiðimizde düþecek para [YENÝ]
}