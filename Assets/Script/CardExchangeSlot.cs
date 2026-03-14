using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardExchangeSlot : MonoBehaviour, IDropHandler
{
    [Header("Settings")]
    public int hpCost = 10;

    [Header("Effects")]
    public GameObject burnEffectPrefab;

    public void OnDrop(PointerEventData eventData)
    {
        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null && tm.state != BattleState.PLAYERTURN)
        {
            Debug.Log("Sýra sizde deðil!");
            return;
        }

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        CardDisplay cardDisplay = droppedObj.GetComponent<CardDisplay>();
        if (cardDisplay == null) return;

        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm == null) return;

        // --- DÜZELTME BAÞLANGICI: Eski Slotu Temizle ---
        // Eðer bu kart masadaki bir slottan geliyorsa, CombatManager'a "Bunu sildim" de.
        // Bu sayede enerji hesaplamasý (Recalculate) tetiklenir.
        CardSlot oldSlot = droppedObj.transform.parent.GetComponent<CardSlot>();
        if (oldSlot != null)
        {
            CombatManager cm = FindObjectOfType<CombatManager>();
            if (cm != null)
            {
                cm.ClearSlot(oldSlot.slotIndex); // Bu fonksiyon enerjiyi yeniden hesaplar
            }
        }
        // --- DÜZELTME BÝTÝÞÝ ---

        // --- TÜR KONTROLÜ ---
        if (cardDisplay.cardData.cardType == CardType.Support)
        {
            // >>> DESTEK KARTI MANTIÐI (Ücretsiz) <<<
            ApplySupportEffect(cardDisplay.cardData, dm, droppedObj);

            dm.AddToDiscard(cardDisplay.cardData);

            CanvasGroup cg = droppedObj.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;

            StartCoroutine(BurnAndDestroyCard(droppedObj, dm, false));
        }
        else
        {
            // >>> NORMAL FEDA MANTIÐI (Can Karþýlýðý Enerji) <<<
            if (PlayerManager.instance.currentHealth <= hpCost)
            {
                Debug.Log("Can yetersiz!");
                return;
            }

            PlayerManager.instance.TakeDamage(hpCost);
            dm.AddToDiscard(cardDisplay.cardData);

            CanvasGroup cg = droppedObj.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;

            StartCoroutine(BurnAndDestroyCard(droppedObj, dm, false));
        }

        // Her iki durumda da Enerji Kazanýlýr
        if (EnergyManager.instance != null)
        {
            EnergyManager.instance.GainEnergy(cardDisplay.cardData.energyCost);
        }
    }

    void ApplySupportEffect(CardData card, DeckManager dm, GameObject playedCardObj)
    {
        switch (card.supportEffect)
        {
            case SupportEffect.DrawExtraCards:
                dm.DrawCard(card.supportValue, playedCardObj);
                Debug.Log($"Destek: {card.supportValue} kart çekildi.");
                break;

            case SupportEffect.DiscardHandAndDraw:
                dm.DiscardHandAndDrawNew(card.supportValue, playedCardObj);
                Debug.Log("Destek: El yenilendi.");
                break;

            case SupportEffect.GainGold:
                if (PlayerManager.instance != null)
                    PlayerManager.instance.AddGold(card.supportValue);
                break;

            case SupportEffect.HealPlayer:
                if (PlayerManager.instance != null)
                    PlayerManager.instance.Heal(card.supportValue);
                break;

            case SupportEffect.GainEnergy:
                if (EnergyManager.instance != null)
                {
                    EnergyManager.instance.GainEnergy(card.supportValue);
                }
                break;

            case SupportEffect.GainShield:
                if (PlayerManager.instance != null)
                {
                    PlayerManager.instance.AddShield(card.supportValue);
                }
                break;

            case SupportEffect.ChangeTerrain:
                if (TerrainManager.instance != null)
                {
                    // Kartýn kendi elementini zemin elementi yapýyoruz
                    TerrainManager.instance.ChangeTerrain(card.element);
                }
                break;
        }
    }

    IEnumerator BurnAndDestroyCard(GameObject cardObj, DeckManager dmRef, bool drawReplacement)
    {
        // 1. Efekt (Yanma partikülü)
        if (burnEffectPrefab != null)
        {
            GameObject effect = Instantiate(burnEffectPrefab, cardObj.transform.position, Quaternion.identity, transform.root);
            Destroy(effect, 2f);
        }

        // 2. YENÝ: Kartý iskartaya uçur
        if (dmRef != null && cardObj != null)
        {
            dmRef.StartCoroutine(dmRef.AnimateCardToDiscard(cardObj));
        }
        else if (cardObj != null)
        {
            Destroy(cardObj);
        }

        // 3. Yeni Kart Çek
        if (drawReplacement)
        {
            yield return new WaitForSeconds(0.1f);
            if (dmRef != null) dmRef.DrawCard(1, cardObj);
        }
    }
}