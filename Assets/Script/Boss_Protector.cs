using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Boss_Protector : BossBehavior
{
    [Header("Minion Settings")]
    public EnemyData minionData; // Inspector'dan atanacak minyon türü

    public override void Setup(CombatManager cm, EnemyDisplay display)
    {
        base.Setup(cm, display);
        // Savaþ baþlar baþlamaz minyonlarý çaðýr
        StartCoroutine(SpawnMinions());
    }

    IEnumerator SpawnMinions()
    {
        // CombatManager'a minyon ekletiyoruz
        // Not: CombatManager'da AddEnemy fonksiyonu yazacaðýz
        yield return new WaitForSeconds(0.5f);
        combatManager.SpawnExtraEnemy(minionData);
        yield return new WaitForSeconds(0.3f);
        combatManager.SpawnExtraEnemy(minionData);
        Debug.Log("Boss koruyucularýný çaðýrdý!");
    }

    public override bool IsInvulnerable()
    {
        // Eðer kendisi dýþýnda baþka düþman varsa ölümsüzdür
        return combatManager.activeEnemies.Count > 1;
    }

    public override IEnumerator ExecuteBossTurn()
    {
        turnCount++;

        // Her 3 turda bir (3, 6, 9...) Ýyileþtirme yapar
        if (turnCount % 3 == 0)
        {
            Debug.Log("BOSS ÖZEL YETENEK: Kutsal Ýyileþtirme!");

            // Efekt (Titre)
            yield return StartCoroutine(myDisplay.AttackAnimation());

            foreach (var enemy in combatManager.activeEnemies)
            {
                if (enemy != null)
                {
                    // %20 Can yenile
                    int heal = 20;
                    enemy.currentHealth += heal;
                    if (enemy.currentHealth > enemy.enemyData.maxHealth)
                        enemy.currentHealth = enemy.enemyData.maxHealth;

                    enemy.UpdateHealthUI(enemy.currentHealth);
                }
            }
            yield return new WaitForSeconds(1f);
        }
        else
        {
            // Normal Saldýrý
            yield return StartCoroutine(myDisplay.AttackAnimation());
            if (PlayerManager.instance != null)
            {
                PlayerManager.instance.TakeDamage(myDisplay.enemyData.damage);
            }
        }
    }
}