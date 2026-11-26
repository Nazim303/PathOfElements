using UnityEngine;
using System.Collections;

public class Boss_Elementalist : BossBehavior
{
    private ElementType[] elements = { ElementType.Fire, ElementType.Water, ElementType.Earth, ElementType.Air };
    private int elementIndex = 0;

    public override IEnumerator ExecuteBossTurn()
    {
        // 1. Saldır
        yield return StartCoroutine(myDisplay.AttackAnimation());
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.TakeDamage(myDisplay.enemyData.damage);
        }

        // 2. Element Değiştir
        elementIndex++;
        if (elementIndex >= elements.Length) elementIndex = 0;

        ElementType newElement = elements[elementIndex];

        // EnemyDisplay üzerindeki anlık elementi değiştiriyoruz (ScriptableObject asset'ini bozmaz)
        myDisplay.ChangeElement(newElement);

        Debug.Log($"Boss Element Değiştirdi: {newElement}");
        yield return new WaitForSeconds(1f);
    }
}