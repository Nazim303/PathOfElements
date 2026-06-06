using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    private CombatManager combatManager;

    [Header("Effects")]
    public GameObject elementPlacementVFX;

    private GameObject activePlacementVFX;

    void Start()
    {
        combatManager = FindObjectOfType<CombatManager>();
    }

    void Update()
    {
        CardDisplay cardInSlot = GetComponentInChildren<CardDisplay>();

        if (cardInSlot == null)
        {
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
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        Draggable draggable = droppedObject.GetComponent<Draggable>();
        CardDisplay droppedCardDisplay = droppedObject.GetComponent<CardDisplay>();

        if (draggable != null && droppedCardDisplay != null)
        {
            // --- FÝZYON (BÝRLEÞME) KONTROLÜ ---
            if (transform.childCount > 0)
            {
                CardDisplay existingCardDisplay = GetComponentInChildren<CardDisplay>();

                if (existingCardDisplay != null && combatManager != null)
                {
                    Transform existingCardTransform = existingCardDisplay.transform;
                    FusionRecipe recipe = combatManager.CheckFusion(existingCardDisplay.cardData, droppedCardDisplay.cardData);

                    if (recipe != null)
                    {
                        Debug.Log($"FÝZYON BAÞARILI!");
                        StartCoroutine(AnimateFusionAndCreateCard(existingCardTransform.gameObject, droppedObject, recipe));
                        return;
                    }
                }
                return;
            }

            // --- NORMAL KART YERLEÞTÝRME ---
            droppedObject.transform.SetParent(this.transform);

            // Kartýn pivot ve çapa ayarlarýný tam merkeze (0.5) kilitliyoruz.
            RectTransform normalRect = droppedObject.GetComponent<RectTransform>();
            normalRect.pivot = new Vector2(0.5f, 0.5f);
            normalRect.anchorMin = new Vector2(0.5f, 0.5f);
            normalRect.anchorMax = new Vector2(0.5f, 0.5f);
            normalRect.anchoredPosition = Vector2.zero; // Kaymayý önleyen sihirli satýr!
            droppedObject.transform.localRotation = Quaternion.identity;

            if (combatManager != null)
            {
                combatManager.UpdateSlot(slotIndex, droppedCardDisplay.cardData);
            }

            // --- ÇÖZÜM: ÝSÝM HATASI DÜZELTÝLDÝ, ANÝMASYON BURADA BAÞLIYOR ---
            StartCoroutine(AnimateCardArrival(droppedObject));

            // --- KART MASAYA KONDUÐUNDA SÜREKLÝ EFEKT YARAT ---
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

    IEnumerator AnimateFusionAndCreateCard(GameObject cardA, GameObject cardB, FusionRecipe recipe)
    {
        Draggable dragA = cardA.GetComponent<Draggable>();
        if (dragA != null) dragA.enabled = false;

        Draggable dragB = cardB.GetComponent<Draggable>();
        if (dragB != null) dragB.enabled = false;

        cardA.transform.SetAsLastSibling();
        cardB.transform.SetAsLastSibling();

        if (recipe.fusionVFX != null)
        {
            GameObject vfx = Instantiate(recipe.fusionVFX, transform.position, Quaternion.identity, transform);
            vfx.transform.SetAsFirstSibling();
            Destroy(vfx, 2.5f);
        }

        float t = 0;
        float duration = 1.0f;
        float rotationSpeed = 720f;

        Vector3 startPosA = cardA.transform.position;
        Vector3 startPosB = cardB.transform.position;
        Vector3 centerPoint = transform.position;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float smoothT = t * t * (3f - 2f * t);

            if (cardA != null && cardB != null)
            {
                cardA.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
                cardB.transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime);
                cardA.transform.position = Vector3.Lerp(startPosA, centerPoint, smoothT);
                cardB.transform.position = Vector3.Lerp(startPosB, centerPoint, smoothT);
                cardA.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, smoothT);
                cardB.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, smoothT);
            }
            yield return null;
        }

        if (cardA != null) Destroy(cardA);
        if (cardB != null) Destroy(cardB);

        GameObject newCardObj = Instantiate(combatManager.cardPrefab, this.transform);
        RectTransform rect = newCardObj.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        newCardObj.transform.localRotation = Quaternion.identity;
        newCardObj.transform.localScale = Vector3.one;

        CardDisplay newCardDisplay = newCardObj.GetComponent<CardDisplay>();
        newCardDisplay.LoadCard(recipe.resultCard);
        combatManager.UpdateSlot(slotIndex, recipe.resultCard);

        StartCoroutine(AnimateCardArrival(newCardObj));

        if (elementPlacementVFX != null)
        {
            if (activePlacementVFX != null) Destroy(activePlacementVFX);

            activePlacementVFX = Instantiate(elementPlacementVFX, transform);
            activePlacementVFX.transform.localPosition = Vector3.zero;
            activePlacementVFX.transform.localScale = Vector3.one;
            activePlacementVFX.transform.SetAsFirstSibling();

            ParticleSystemRenderer[] renderers = activePlacementVFX.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var r in renderers) r.sortingOrder = 50;

            Color elementColor = GetElementColor(newCardDisplay.cardData.element);
            ParticleSystem[] particles = activePlacementVFX.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.startColor = elementColor;
            }
        }
    }

    // --- ÇÖZÜM: MILIMETRIK UI HÝZALAMASI (RectTransform) ---
    IEnumerator AnimateCardArrival(GameObject cardObj)
    {
        if (cardObj == null) yield break;

        RectTransform rect = cardObj.GetComponent<RectTransform>();

        float t = 0;
        float duration = 0.25f;

        // --- DÜZELTME: Animasyon süresince geçici dev boyutta baþlasýn ---
        Vector3 targetPos = Vector3.zero;
        Vector3 startPos = new Vector3(0, 200f, -100f);

        // Havadayken büyük, yere çarpýnca GlobalScaleManager'ýn istediði boyuta "doðal" düþsün
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

        // --- YERE ÇARPMA VE TESLÝM ETME ---
        if (cardObj != null)
        {
            rect.anchoredPosition3D = targetPos;
            rect.localRotation = targetRot;

            // DÜZELTME: Kartý artýk dev 1.5x'e sabitlemiyoruz!
            // SquashAndStretch animasyonunu orijinal boyutu üzerinden yapmasý için finalScale'i veriyoruz
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
