using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Boss_Protector : BossBehavior
{
    [Header("Minion Settings")]
    public EnemyData minionData; // Inspector'dan atanacak minyon türü
    public int healAmount = 10;   // Her 3 turda bir tüm düşmanlara
    public int healEvery = 3;

    public override void Setup(CombatManager cm, EnemyDisplay display)
    {
        base.Setup(cm, display);
        // Savaş başlar başlamaz minyonları çağır
        StartCoroutine(SpawnMinions());
    }

    IEnumerator SpawnMinions()
    {
        // CombatManager'a minyon ekletiyoruz
        // Not: CombatManager'da AddEnemy fonksiyonu yazacağız
        yield return new WaitForSeconds(0.5f);
        combatManager.SpawnExtraEnemy(minionData);
        yield return new WaitForSeconds(0.3f);
        combatManager.SpawnExtraEnemy(minionData);
        Debug.Log("Boss koruyucularını çağırdı!");
    }

    public override bool IsInvulnerable()
    {
        // Eğer kendisi dışında başka düşman varsa ölümsüzdür
        return combatManager.activeEnemies.Count > 1;
    }

    public override IEnumerator ExecuteBossTurn()
    {
        turnCount++;

        // Her 3 turda bir (3, 6, 9...) İyileştirme yapar
        if (turnCount % healEvery == 0)
        {
            Debug.Log("BOSS ÖZEL YETENEK: Kutsal İyileştirme!");

            // Efekt (Titre)
            yield return StartCoroutine(myDisplay.AttackAnimation());

            foreach (var enemy in combatManager.activeEnemies)
            {
                if (enemy != null)
                {
                    enemy.currentHealth += healAmount;
                    if (enemy.currentHealth > enemy.enemyData.maxHealth)
                        enemy.currentHealth = enemy.enemyData.maxHealth;

                    enemy.UpdateHealthUI(enemy.currentHealth);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else
        {
            // Normal Saldırı
            yield return StartCoroutine(myDisplay.AttackAnimation());
            if (PlayerManager.instance != null)
            {
                PlayerManager.instance.TakeDamage(myDisplay.enemyData.damage);
            }
        }
    }
}