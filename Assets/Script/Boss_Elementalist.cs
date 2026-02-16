using UnityEngine;
using System.Collections;

public class Boss_Elementalist : BossBehavior
{
    private ElementType[] elements = { ElementType.Fire, ElementType.Water, ElementType.Earth, ElementType.Air };
    private int elementIndex = 0;

    public override IEnumerator ExecuteBossTurn()
    {
        // 1. Saldýr
        yield return StartCoroutine(myDisplay.AttackAnimation());
        if (PlayerManager.instance != null)
        {
            PlayerManager.instance.TakeDamage(myDisplay.enemyData.damage);
        }

        // 2. Element Deðiþtir
        elementIndex++;
        if (elementIndex >= elements.Length) elementIndex = 0;

        ElementType newElement = elements[elementIndex];

        // Datayý anlýk deðiþtiriyoruz (ScriptableObject kopyasý üzerinde çalýþtýðýmýz için güvenli)
        myDisplay.enemyData.element = newElement;

        // Görsel güncelleme (Renk deðiþimi)
        // EnemyDisplay içinde UpdateElementVisual public olmalý (Adým 6'da düzelteceðiz)
        myDisplay.UpdateElementVisual(newElement);

        Debug.Log($"Boss Element Deðiþtirdi: {newElement}");
        yield return new WaitForSeconds(1f);
    }
}