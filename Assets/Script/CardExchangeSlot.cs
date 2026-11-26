using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;

public class CardExchangeSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    public int hpCost = 10;

    [Header("Effects")]
    public GameObject burnEffectPrefab;
    public GameObject castEffectPrefab;
    public GameObject chamberEffectPrefab; // --- YENİ: Sürekli dönecek olan Chamber Efekti ---

    [Header("Material (M_MasterCard)")]
    public Image slotImage;

    private Material instancedMaterial;
    private GameObject activeChamberEffect; // Yaratılan efekti hafızada tutarız
    // --- YENİ: ÖZEL BÜYÜ EFEKTLERİ ---
    public GameObject castWaterHealPrefab;
    public GameObject castBloodTransferPrefab;

    void Start()
    {
        if (slotImage != null)
        {
            instancedMaterial = new Material(slotImage.material);
            slotImage.material = instancedMaterial;

            if (instancedMaterial.HasProperty("_Intensity"))
                instancedMaterial.SetFloat("_Intensity", 0f);
        }

        // --- YENİ: OYUN BAŞLARKEN CHAMBER EFEKTİNİ YARAT VE LOOPA SOK ---
        if (chamberEffectPrefab != null)
        {
            // YENİ: Sadece parent verdik (transform)
            activeChamberEffect = Instantiate(chamberEffectPrefab, transform);

            // YENİ: Pozisyonu tam merkeze sabitledik
            activeChamberEffect.transform.localPosition = Vector3.zero;

            // KRİTİK ÇÖZÜM: UI Canvas'ın partikülü ezmesini engeller, dikdörtgen şekli korur!
            activeChamberEffect.transform.localScale = Vector3.one;

            // Yuvanın arkasında değil, önünde ama kartın arkasında durması için
            activeChamberEffect.transform.SetAsLastSibling();

            // Sorting Order (Çizim Sırası) Yükseltmesi
            ParticleSystemRenderer[] renderers = activeChamberEffect.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers)
            {
                r.sortingOrder = 50; // Yuvanın üstünde
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging && instancedMaterial != null && instancedMaterial.HasProperty("_Intensity"))
        {
            instancedMaterial.SetFloat("_Intensity", 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (instancedMaterial != null && instancedMaterial.HasProperty("_Intensity"))
        {
            instancedMaterial.SetFloat("_Intensity", 0f);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (instancedMaterial != null && instancedMaterial.HasProperty("_Intensity"))
        {
            instancedMaterial.SetFloat("_Intensity", 0f);
        }

        TurnManager tm = TurnManager.instance != null ? TurnManager.instance : FindFirstObjectByType<TurnManager>();
        if (tm != null && tm.state != BattleState.PLAYERTURN) return;

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        CardDisplay cardDisplay = droppedObj.GetComponent<CardDisplay>();
        if (cardDisplay == null) return;

        DeckManager dm = DeckManager.instance != null ? DeckManager.instance : FindFirstObjectByType<DeckManager>();
        if (dm == null) return;

        // --- CAN KONTROLÜ (Slottan silmeden ÖNCE kontrol edilir) ---
        int requiredHp = 0;
        if (cardDisplay.cardData.cardType != CardType.Support)
        {
            requiredHp = cardDisplay.cardData.isElementalModifier ? (hpCost * 2) : hpCost;
        }

        if (PlayerManager.instance != null && PlayerManager.instance.currentHealth < requiredHp)
        {
            Debug.Log("[FEDA HATASI] Yetersiz can! Kart feda edilemez.");
            return;
        }

        CardSlot oldSlot = droppedObj.transform.parent.GetComponent<CardSlot>();
        if (oldSlot != null)
        {
            CombatManager cm = CombatManager.instance != null ? CombatManager.instance : FindFirstObjectByType<CombatManager>();
            if (cm != null) cm.ClearSlot(oldSlot.slotIndex);
        }

        if (AudioManager.instance != null) AudioManager.instance.PlayExchange();

        // --- FEDA DURUMLARI ---
        if (cardDisplay.cardData.isElementalModifier)
        {
            int penaltyCost = hpCost * 2;

            // Kalkanı Es Geç - Direkt Candan Düş
            PlayerManager.instance.currentHealth -= penaltyCost;
            if (PlayerManager.instance.currentHealth < 0) PlayerManager.instance.currentHealth = 0;
            PlayerManager.instance.UpdateUI();

            // --- YENİ: KAN BEDELİ ÖDENDİĞİNDE EKRAN SALLANSIN ---
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.2f, 15f);

            // Ölüm Kontrolü
            if (PlayerManager.instance.currentHealth <= 0)
            {
                if (GameOverManager.instance != null) GameOverManager.instance.TriggerGameOver();
                return;
            }

            dm.AddToDiscard(cardDisplay.cardData);
            DisableCardRaycast(droppedObj);
            StartCoroutine(CastAndDestroyCard(droppedObj, dm, false));

            if (EnergyManager.instance != null)
            {
                EnergyManager.instance.GainEnergy(cardDisplay.cardData.energyCost);
            }
            return;
        }
        else if (cardDisplay.cardData.cardType == CardType.Support)
        {
            ApplySupportEffect(cardDisplay.cardData, dm, droppedObj);
            dm.AddToDiscard(cardDisplay.cardData);
            DisableCardRaycast(droppedObj);
            StartCoroutine(CastAndDestroyCard(droppedObj, dm, false));
            return;
        }
        else
        {
            // Kalkanı Es Geç - Direkt Candan Düş
            PlayerManager.instance.currentHealth -= hpCost;
            if (PlayerManager.instance.currentHealth < 0) PlayerManager.instance.currentHealth = 0;
            PlayerManager.instance.UpdateUI();

            // --- YENİ: KAN BEDELİ ÖDENDİĞİNDE EKRAN SALLANSIN ---
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.2f, 15f);

            // Ölüm Kontrolü
            if (PlayerManager.instance.currentHealth <= 0)
            {
                if (GameOverManager.instance != null) GameOverManager.instance.TriggerGameOver();
                return;
            }

            dm.AddToDiscard(cardDisplay.cardData);
            DisableCardRaycast(droppedObj);
            StartCoroutine(CastAndDestroyCard(droppedObj, dm, false));

            if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(cardDisplay.cardData.energyCost);
        }
    }


    void DisableCardRaycast(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;
    }

    void ApplySupportEffect(CardData card, DeckManager dm, GameObject playedCardObj)
    {
        switch (card.supportEffect)
        {
            case SupportEffect.DrawExtraCards:
                dm.DrawCard(card.supportValue, playedCardObj);
                if (AudioManager.instance != null) AudioManager.instance.PlayGambling();
                break;

            case SupportEffect.DiscardHandAndDraw:
                dm.DiscardHandAndDrawNew(card.supportValue, playedCardObj);
                if (AudioManager.instance != null) AudioManager.instance.PlayGambling();
                break;

            case SupportEffect.GainGold:
                if (PlayerManager.instance != null) PlayerManager.instance.AddGold(card.supportValue);
                if (AudioManager.instance != null) AudioManager.instance.PlayGreed();
                break;

            case SupportEffect.HealPlayer:
                if (PlayerManager.instance != null) PlayerManager.instance.Heal(card.supportValue);

                // --- ÇÖZÜM: KAN TRANSFERİ VEYA SU ŞİFASI EFEKTİ BURADA ÇALIŞACAK ---
                if (card.element == ElementType.Water && castWaterHealPrefab != null)
                {
                    GameObject fx = Instantiate(castWaterHealPrefab, transform.position, Quaternion.identity, transform.root);
                    fx.transform.SetAsLastSibling();
                    Destroy(fx, 2f);
                }
                else if (castBloodTransferPrefab != null) // Su değilse Kan Efektini çal!
                {
                    GameObject fx = Instantiate(castBloodTransferPrefab, transform.position, Quaternion.identity, transform.root);
                    fx.transform.SetAsLastSibling();
                    Destroy(fx, 2f);
                }
                break;

            case SupportEffect.GainEnergy: if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(card.supportValue); break;
            case SupportEffect.GainShield: if (PlayerManager.instance != null) PlayerManager.instance.AddShield(card.supportValue); break;
        }

        if (card.specialEffect == SpecialEffect.HalveEnemyHealth && castBloodTransferPrefab != null)
        {
            GameObject fx = Instantiate(castBloodTransferPrefab, transform.position, Quaternion.identity, transform.root);
            fx.transform.SetAsLastSibling();
            Destroy(fx, 2f);
        }
    }
    IEnumerator CastAndDestroyCard(GameObject cardObj, DeckManager dmRef, bool drawReplacement)
    {
        if (castEffectPrefab != null)
        {
            GameObject effect = Instantiate(castEffectPrefab, transform.position, Quaternion.identity, transform.root);
            effect.transform.SetAsLastSibling();
            ParticleSystemRenderer[] renderers = effect.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers) r.sortingOrder = 200; // En önde patlasın
            Destroy(effect, 2f);
        }

        if (dmRef != null && cardObj != null) dmRef.StartCoroutine(dmRef.AnimateCardToDiscard(cardObj));
        else if (cardObj != null) Destroy(cardObj);

        if (drawReplacement)
        {
            yield return new WaitForSeconds(0.1f);
            if (dmRef != null) dmRef.DrawCard(1, cardObj);
        }
    }
}