using UnityEngine;

public class GlobalScaleManager : MonoBehaviour
{
    public static GlobalScaleManager instance;

    [Header("Kart Büyüklükleri")]
    [Range(0.5f, 3f)] public float cardInHandScale = 1.0f; // Eldeki kartlar
    [Range(0.5f, 3f)] public float cardInSlotScale = 1.5f; // Masaya konan kartlar

    [Header("Düşman Büyüklükleri")]
    [Range(0.5f, 4f)] public float normalEnemyScale = 1.0f; // Normal düşmanlar
    [Range(0.5f, 5f)] public float bossEnemyScale = 1.5f;   // Boss'lar

    private CombatManager cm;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Update()
    {
        // Anlık güncellemeleri savaş sırasında yapıyoruz
        if (cm == null) cm = CombatManager.instance != null ? CombatManager.instance : FindFirstObjectByType<CombatManager>();
        if (cm == null) return;

        // 1. MASADAKİ (SLOTLARDAKİ) KARTLARI GÜNCELLE
        foreach (Transform slot in cm.slotTransforms)
        {
            if (slot != null)
            {
                CardSlot slotComp = slot.GetComponent<CardSlot>();
                CardDisplay cardDisplay = slotComp != null ? slotComp.GetCurrentCard() : slot.GetComponentInChildren<CardDisplay>();
                if (cardDisplay != null)
                {
                    Transform card = cardDisplay.transform;
                    // Kart yutulma veya fizyon gibi sebeplerle küçülüyorsa ona dokunma
                    if (card.localScale.x > 0.1f)
                    {
                        card.localScale = Vector3.one * cardInSlotScale;
                    }
                }
            }
        }

        // 2. DÜŞMANLARI GÜNCELLE
        foreach (EnemyDisplay enemy in cm.activeEnemies)
        {
            // Ölen düşmanın küçülerek yok olma animasyonunu bozmamak için can kontrolü yapıyoruz
            if (enemy != null && enemy.currentHealth > 0)
            {
                bool isBoss = enemy.GetComponent<BossBehavior>() != null;
                float targetScale = isBoss ? bossEnemyScale : normalEnemyScale;
                enemy.transform.localScale = Vector3.one * targetScale;
            }
        }
    }
}