using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy", menuName = "Path of Elements/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public int maxHealth;
    public ElementType element;
    public Sprite enemyArt;
    public Sprite enemybander;
    public int damage;      // Düşmanın tur başına vurduğu hasar
    public int goldReward;  // Yendiğimizde düşecek para [YENİ]

    
    [Header("Journal Info")]
    [TextArea(5, 10)]
    public string journalDescription; // Düşmanın uzun hikayesi
}