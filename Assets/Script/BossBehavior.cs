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

    // Boss'un sırası gelince ne yapacak? (TurnManager bunu çağıracak)
    public abstract IEnumerator ExecuteBossTurn();

    // Boss şu an hasar alabilir mi?
    public virtual bool IsInvulnerable()
    {
        return false; // Varsayılan olarak hasar alır
    }

    // Boss hasar alınca özel bir tepki verir mi?
    public virtual void OnTakeDamage(int amount) { }
}