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

    // ==========================================
    // --- YENÝ: OTONOM TEMÝZLÝK SÝSTEMÝ ---
    // ==========================================
    void Update()
    {
        // 1. Sadece "childCount"a bakmýyoruz çünkü VFX de bir obje!
        // Bunun yerine "Ýçimde gerçek bir CardDisplay (Kart) var mý?" diye kontrol ediyoruz.
        CardDisplay cardInSlot = GetComponentInChildren<CardDisplay>();

        // Eðer yuvada bir kart YOKSA (oyuncu tutup çekmiþse veya feda edilmiþse)
        if (cardInSlot == null)
        {
            // 1. Arka planda dönen efekti sil
            if (activePlacementVFX != null)
            {
                Destroy(activePlacementVFX);
                activePlacementVFX = null;
            }

            // 2. CombatManager'da burasý hala "dolu" görünüyorsa, temizle
            // Bu sayede enerji hesaplamasý da anýnda güncellenir!
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
                Transform existingCardTransform = transform.GetChild(0);
                CardDisplay existingCardDisplay = existingCardTransform.GetComponent<CardDisplay>();

                if (existingCardDisplay != null && combatManager != null)
                {
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
            // Artýk eski slotu burada temizlememize gerek yok, Update fonksiyonu eski slotu otomatik boþaltacak!
            droppedObject.transform.SetParent(this.transform);

            RectTransform normalRect = droppedObject.GetComponent<RectTransform>();
            normalRect.pivot = new Vector2(0.5f, 0.5f);
            normalRect.anchorMin = new Vector2(0.5f, 0.5f);
            normalRect.anchorMax = new Vector2(0.5f, 0.5f);
            normalRect.anchoredPosition = Vector2.zero;
            droppedObject.transform.localRotation = Quaternion.identity;

            float slotScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.cardInSlotScale : 1.5f;
            droppedObject.transform.localScale = new Vector3(slotScale, slotScale, slotScale);

            if (combatManager != null)
            {
                combatManager.UpdateSlot(slotIndex, droppedCardDisplay.cardData);
            }

            // --- KART MASAYA KONDUÐUNDA SÜREKLÝ EFEKT YARAT ---
            if (elementPlacementVFX != null)
            {
                // Varsa eskiyi sil
                if (activePlacementVFX != null) Destroy(activePlacementVFX);

                activePlacementVFX = Instantiate(elementPlacementVFX, transform);
                activePlacementVFX.transform.localPosition = Vector3.zero;
                activePlacementVFX.transform.localScale = Vector3.one;

                // Efekt Kartýn ARKASINDA dursun diye en baþa atýyoruz
                activePlacementVFX.transform.SetAsFirstSibling();

                ParticleSystemRenderer[] renderers = activePlacementVFX.GetComponentsInChildren<ParticleSystemRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sortingOrder = 50;
                }

                // EFEKTÝN RENGÝNÝ KARTIN ELEMENTÝNE GÖRE DEÐÝÞTÝR
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

        // --- FÝZYON BÝTÝNCE YENÝ KART ÝÇÝN ELEMENT EFEKTÝ YARAT ---
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

    IEnumerator AnimateCardArrival(GameObject cardObj)
    {
        float t = 0;
        float duration = 0.3f;
        float targetScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.cardInSlotScale : 1.5f;

        if (cardObj != null) cardObj.transform.localScale = Vector3.zero;

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float smoothT = t * t * (3f - 2f * t);
            if (cardObj != null) cardObj.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, smoothT);
            yield return null;
        }
        if (cardObj != null) cardObj.transform.localScale = Vector3.one * targetScale;
    }
}