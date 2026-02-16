using UnityEngine;
using System.Collections;

public abstract class BossBehavior : MonoBehaviour
{
    protected EnemyDisplay myDisplay;
    protected CombatManager combatManager;
    protected int turnCount = 0;

    public virtual void Setup(CombatManager cm, EnemyDisplay display)
    {
        combatManager = cm;
        myDisplay = display;
    }

    // Boss'un sýrasý gelince ne yapacak? (TurnManager bunu çaðýracak)
    public abstract IEnumerator ExecuteBossTurn();

    // Boss þu an hasar alabilir mi?
    public virtual bool IsInvulnerable()
    {
        return false; // Varsayýlan olarak hasar alýr
    }

    // Boss hasar alýnca özel bir tepki verir mi?
    public virtual void OnTakeDamage(int amount) { }
}