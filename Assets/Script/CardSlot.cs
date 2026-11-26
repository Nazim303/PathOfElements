using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    private CombatManager combatManager;
    private CardDisplay currentCardDisplay;

    [Header("Effects")]
    public GameObject elementPlacementVFX;

    private GameObject activePlacementVFX;

    public CardDisplay GetCurrentCard() => currentCardDisplay;

    void Start()
    {
        combatManager = CombatManager.instance != null ? CombatManager.instance : FindFirstObjectByType<CombatManager>();
    }

    void Update()
    {
        if (currentCardDisplay != null)
        {
            if (currentCardDisplay.transform.parent != transform || !currentCardDisplay.gameObject.activeInHierarchy)
            {
                ClearCurrentCard();
            }
        }
        else if (activePlacementVFX != null || (combatManager != null && combatManager.playerSlots[slotIndex] != null))
        {
            ClearCurrentCard();
        }
    }

    public void ClearCurrentCard()
    {
        currentCardDisplay = null;
        if (activePlacementVFX != null)
        {
            Destroy(activePlacementVFX);
            activePlacementVFX = null;
        }

        if (combatManager != null && combatManager.playerSlots[slotIndex] != null)
        {
            combatManager.ClearSlot(slotIndex);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        Draggable draggable = droppedObject != null ? droppedObject.GetComponent<Draggable>() : null;
        CardDisplay droppedCardDisplay = droppedObject != null ? droppedObject.GetComponent<CardDisplay>() : null;

        if (draggable != null && droppedCardDisplay != null)
        {
            // Slot doluysa kart bırakılamaz
            if (currentCardDisplay != null) return;

            // --- NORMAL KART YERLEŞTİRME ---
            currentCardDisplay = droppedCardDisplay;
            droppedObject.transform.SetParent(this.transform);

            // Kartın pivot ve çapa ayarlarını tam merkeze (0.5) kilitliyoruz.
            RectTransform normalRect = droppedObject.GetComponent<RectTransform>();
            normalRect.pivot = new Vector2(0.5f, 0.5f);
            normalRect.anchorMin = new Vector2(0.5f, 0.5f);
            normalRect.anchorMax = new Vector2(0.5f, 0.5f);
            normalRect.anchoredPosition = Vector2.zero; // Kaymayı önleyen sihirli satır!
            droppedObject.transform.localRotation = Quaternion.identity;

            if (combatManager != null)
            {
                combatManager.UpdateSlot(slotIndex, droppedCardDisplay.cardData);
            }

            // --- ÇÖZÜM: İSİM HATASI DÜZELTİLDİ, ANİMASYON BURADA BAŞLIYOR ---
            StartCoroutine(AnimateCardArrival(droppedObject));

            // --- KART MASAYA KONDUĞUNDA SÜREKLİ EFEKT YARAT ---
            if (elementPlacementVFX != null)
            {
                if (activePlacementVFX != null) Destroy(activePlacementVFX);

                activePlacementVFX = Instantiate(elementPlacementVFX, transform);
                activePlacementVFX.transform.localPosition = Vector3.zero;
                activePlacementVFX.transform.localScale = Vector3.one;
                activePlacementVFX.transform.SetAsFirstSibling();

                ParticleSystemRenderer[] renderers = activePlacementVFX.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sortingOrder = 50;
                }

                Color elementColor = GetElementColor(droppedCardDisplay.cardData.element);
                ParticleSystem[] particles = activePlacementVFX.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particles)
                {
                    var main = ps.main;
                    main.startColor = elementColor;
                }
            }
        }
    }

    Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return Color.red;
            case ElementType.Water: return Color.blue;
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f);
            case ElementType.Air: return Color.cyan;
            default: return Color.white;
        }
    }

    IEnumerator AnimateCardArrival(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        RectTransform rect = cardObj.GetComponent<RectTransform>();

        float t = 0;
        float duration = 0.25f;

        // --- DÜZELTME: Animasyon süresince geçici dev boyutta başlasın ---
        Vector3 targetPos = Vector3.zero;
        Vector3 startPos = new Vector3(0, 200f, -100f);

        // Havadayken büyük, yere çarpınca GlobalScaleManager'ın istediği boyuta "doğal" düşsün
        float finalScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.cardInSlotScale : 1.0f;
        Vector3 targetScaleVec = Vector3.one * finalScale;
        Vector3 startScaleVec = targetScaleVec * 2.0f;

        Quaternion targetRot = Quaternion.identity;
        Quaternion startRot = Quaternion.Euler(45f, 0f, -20f);

        if (AudioManager.instance != null) AudioManager.instance.PlayCardPaper();

        while (t < 1)
        {
            if (cardObj == null) yield break;
            t += Time.deltaTime / duration;
            float smoothT = t * t * t;

            rect.anchoredPosition3D = Vector3.Lerp(startPos, targetPos, smoothT);
            rect.localScale = Vector3.Lerp(startScaleVec, targetScaleVec, smoothT);
            rect.localRotation = Quaternion.Lerp(startRot, targetRot, smoothT);

            yield return null;
        }

        // --- YERE ÇARPMA VE TESLİM ETME ---
        if (cardObj != null)
        {
            rect.anchoredPosition3D = targetPos;
            rect.localRotation = targetRot;

            // DÜZELTME: Kartı artık dev 1.5x'e sabitlemiyoruz!
            // SquashAndStretch animasyonunu orijinal boyutu üzerinden yapması için finalScale'i veriyoruz
            yield return StartCoroutine(SquashAndStretch(rect, targetScaleVec));

            if (AudioManager.instance != null) AudioManager.instance.PlayImpactSound(ElementType.Earth, 1);
        }
    }

    IEnumerator SquashAndStretch(Transform cardTransform, Vector3 originalScale)
    {
        if (cardTransform == null) yield break;

        Vector3 squashScale = new Vector3(originalScale.x * 1.15f, originalScale.y * 0.85f, originalScale.z);

        float t = 0;
        while (t < 1)
        {
            if (cardTransform == null) yield break;
            t += Time.deltaTime / 0.05f;
            cardTransform.localScale = Vector3.Lerp(originalScale, squashScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            if (cardTransform == null) yield break;
            t += Time.deltaTime / 0.1f;
            cardTransform.localScale = Vector3.Lerp(squashScale, originalScale, t);
            yield return null;
        }

        if (cardTransform != null) cardTransform.localScale = originalScale;
    }
}
