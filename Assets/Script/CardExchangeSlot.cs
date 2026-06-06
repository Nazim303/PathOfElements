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
    public GameObject chamberEffectPrefab; // --- YENÝ: Sürekli dönecek olan Chamber Efekti ---

    [Header("Material (M_MasterCard)")]
    public Image slotImage;

    private Material instancedMaterial;
    private GameObject activeChamberEffect; // Yaratýlan efekti hafýzada tutarýz
    // --- YENÝ: ÖZEL BÜYÜ EFEKTLERÝ ---
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

        // --- YENÝ: OYUN BAÞLARKEN CHAMBER EFEKTÝNÝ YARAT VE LOOPA SOK ---
        if (chamberEffectPrefab != null)
        {
            // YENÝ: Sadece parent verdik (transform)
            activeChamberEffect = Instantiate(chamberEffectPrefab, transform);

            // YENÝ: Pozisyonu tam merkeze sabitledik
            activeChamberEffect.transform.localPosition = Vector3.zero;

            // KRÝTÝK ÇÖZÜM: UI Canvas'ýn partikülü ezmesini engeller, dikdörtgen þekli korur!
            activeChamberEffect.transform.localScale = Vector3.one;

            // Yuvanýn arkasýnda deðil, önünde ama kartýn arkasýnda durmasý için
            activeChamberEffect.transform.SetAsLastSibling();

            // Sorting Order (Çizim Sýrasý) Yükseltmesi
            ParticleSystemRenderer[] renderers = activeChamberEffect.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers)
            {
                r.sortingOrder = 50; // Yuvanýn üstünde
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

        TurnManager tm = FindObjectOfType<TurnManager>();
        if (tm != null && tm.state != BattleState.PLAYERTURN) return;

        GameObject droppedObj = eventData.pointerDrag;
        if (droppedObj == null) return;

        CardDisplay cardDisplay = droppedObj.GetComponent<CardDisplay>();
        if (cardDisplay == null) return;

        DeckManager dm = FindObjectOfType<DeckManager>();
        if (dm == null) return;

        CardSlot oldSlot = droppedObj.transform.parent.GetComponent<CardSlot>();
        if (oldSlot != null)
        {
            CombatManager cm = FindObjectOfType<CombatManager>();
            if (cm != null) cm.ClearSlot(oldSlot.slotIndex);
        }
        // --- YENÝ: KART YUVAYA GÝRÝÞ/EXCHANGE SESÝ ---
        if (AudioManager.instance != null) AudioManager.instance.PlayExchange();

        // --- FEDA DURUMLARI ---

        if (cardDisplay.cardData.isElementalModifier)
        {

            int penaltyCost = hpCost * 2;
            if (PlayerManager.instance.currentHealth <= penaltyCost) return;

            PlayerManager.instance.TakeDamage(penaltyCost);
            dm.AddToDiscard(cardDisplay.cardData);
            DisableCardRaycast(droppedObj);
            StartCoroutine(CastAndDestroyCard(droppedObj, dm, false));
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
            if (PlayerManager.instance.currentHealth <= hpCost) return;

            PlayerManager.instance.TakeDamage(hpCost);
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

                // --- ÇÖZÜM: KAN TRANSFERÝ VEYA SU ÞÝFASI EFEKTÝ BURADA ÇALIÞACAK ---
                if (card.element == ElementType.Water && castWaterHealPrefab != null)
                {
                    GameObject fx = Instantiate(castWaterHealPrefab, transform.position, Quaternion.identity, transform.root);
                    fx.transform.SetAsLastSibling();
                    Destroy(fx, 2f);
                }
                else if (castBloodTransferPrefab != null) // Su deðilse Kan Efektini çal!
                {
                    GameObject fx = Instantiate(castBloodTransferPrefab, transform.position, Quaternion.identity, transform.root);
                    fx.transform.SetAsLastSibling();
                    Destroy(fx, 2f);
                }
                break;

            case SupportEffect.GainEnergy: if (EnergyManager.instance != null) EnergyManager.instance.GainEnergy(card.supportValue); break;
            case SupportEffect.GainShield: if (PlayerManager.instance != null) PlayerManager.instance.AddShield(card.supportValue); break;
            case SupportEffect.ChangeTerrain: if (TerrainManager.instance != null) TerrainManager.instance.ChangeTerrain(card.element); break;
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
            foreach (var r in renderers) r.sortingOrder = 200; // En önde patlasýn
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