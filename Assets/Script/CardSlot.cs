using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class CardSlot : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    private CombatManager combatManager;

    void Start()
    {
        combatManager = FindObjectOfType<CombatManager>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        Draggable draggable = droppedObject.GetComponent<Draggable>();
        CardDisplay droppedCardDisplay = droppedObject.GetComponent<CardDisplay>();

        if (draggable != null && droppedCardDisplay != null)
        {
            // ==========================================
            // 1. FÝZYON (BÝRLEÞME) KONTROLÜ
            // ==========================================
            if (transform.childCount > 0)
            {
                Transform existingCardTransform = transform.GetChild(0);
                CardDisplay existingCardDisplay = existingCardTransform.GetComponent<CardDisplay>();

                if (existingCardDisplay != null && combatManager != null)
                {
                    // Tarif var mý diye CombatManager'a sor
                    FusionRecipe recipe = combatManager.CheckFusion(existingCardDisplay.cardData, droppedCardDisplay.cardData);

                    if (recipe != null)
                    {
                        Debug.Log($"FÝZYON BAÞARILI! {existingCardDisplay.cardData.cardName} + {droppedCardDisplay.cardData.cardName} = {recipe.resultCard.cardName}");

                        // SADECE animasyonu baþlatýyoruz. Kartý yaratma iþini ve VFX'i animasyonun içine devrettik.
                        StartCoroutine(AnimateFusionAndCreateCard(
                            existingCardTransform.gameObject,
                            droppedObject,
                            recipe));

                        return; // Ýþlemi bitir, kart geri sekmesin!
                    }
                }

                // Tarif yoksa, kartý geri çevir (Normal sekme hareketi)
                return;
            }

            // ==========================================
            // 2. NORMAL KART YERLEÞTÝRME (Slot Boþsa)
            // ==========================================
            CardSlot oldSlot = droppedObject.transform.parent.GetComponent<CardSlot>();
            if (oldSlot != null && combatManager != null)
            {
                combatManager.ClearSlot(oldSlot.slotIndex);
            }

            droppedObject.transform.SetParent(this.transform);

            // Pivot ve pozisyon ayarlarý
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
        }
    }

    // ==========================================
    // --- YENÝ: FÝZYON GÖRSEL EFEKTÝ ---
    // ==========================================
    IEnumerator AnimateFusionAndCreateCard(GameObject cardA, GameObject cardB, FusionRecipe recipe)
    {
        // Sürüklemeyi (Draggable) hemen kapat ki animasyon sýrasýnda kart oynamasýn
        Draggable dragA = cardA.GetComponent<Draggable>();
        if (dragA != null) dragA.enabled = false;

        Draggable dragB = cardB.GetComponent<Draggable>();
        if (dragB != null) dragB.enabled = false;

        // Kartlarý UI'da her þeyin üstüne çýkar (VFX katmaný gibi)
        cardA.transform.SetAsLastSibling();
        cardB.transform.SetAsLastSibling();

        // --- GÖRSEL AYARLAR (VFX) ---
        if (recipe.fusionVFX != null)
        {
            GameObject vfx = Instantiate(recipe.fusionVFX, transform.position, Quaternion.identity, transform);
            // Girdap efektini kartlarýn arkasýna koymak için:
            vfx.transform.SetAsFirstSibling();
            Destroy(vfx, 2.5f); // Efekti temizle
        }

        // --- ANÝMASYON: Girdap Gibi Dönme ve Küçülme ---
        float t = 0;
        float duration = 1.0f; // Animasyon 1 saniye sürsün
        float rotationSpeed = 720f; // Saniyede 2 tam tur (Daha hýzlý olabilir)

        Vector3 startPosA = cardA.transform.position;
        Vector3 startPosB = cardB.transform.position;
        Vector3 centerPoint = transform.position; // Slotun merkezi

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            // Hareketi yumuþat
            float smoothT = t * t * (3f - 2f * t);

            if (cardA != null && cardB != null)
            {
                // 1. DÖNDÜRME (Kendi eksenlerinde ve merkez etrafýnda)
                cardA.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
                cardB.transform.Rotate(Vector3.forward * -rotationSpeed * Time.deltaTime); // Tersine dönsün

                // 2. MERKEZE ÇEKME (Vortex hissi)
                cardA.transform.position = Vector3.Lerp(startPosA, centerPoint, smoothT);
                cardB.transform.position = Vector3.Lerp(startPosB, centerPoint, smoothT);

                // 3. KÜÇÜLTME
                cardA.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, smoothT);
                cardB.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, smoothT);
            }

            yield return null;
        }

        // --- TEMÝZLÝK VE YENÝ KART YARATMA ---
        if (cardA != null) Destroy(cardA);
        if (cardB != null) Destroy(cardB);

        // Sonuç kartýný oluþtur ve slota yerleþtir
        GameObject newCardObj = Instantiate(combatManager.cardPrefab, this.transform);

        // Pivot ve pozisyon ayarlarý
        RectTransform rect = newCardObj.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        newCardObj.transform.localRotation = Quaternion.identity;
        // Boyutu sabitle
        newCardObj.transform.localScale = Vector3.one;

        // Veriyi Yükle
        CardDisplay newCardDisplay = newCardObj.GetComponent<CardDisplay>();
        newCardDisplay.LoadCard(recipe.resultCard);

        // Backend'e haber ver (Enerji otomatik yeniden hesaplanacak)
        combatManager.UpdateSlot(slotIndex, recipe.resultCard);

        // YENÝ KART GELÝÞ ANÝMASYONU (Ufaktan büyüyerek gelsin)
        StartCoroutine(AnimateCardArrival(newCardObj));
    }

    // Yeni kartýn ufaktan büyüyerek belirmesi animasyonu
    // Yeni kartýn ufaktan büyüyerek belirmesi animasyonu
    IEnumerator AnimateCardArrival(GameObject cardObj)
    {
        float t = 0;
        float duration = 0.3f;

        float targetScale = GlobalScaleManager.instance != null ? GlobalScaleManager.instance.cardInSlotScale : 1.5f;

        if (cardObj != null) cardObj.transform.localScale = Vector3.zero; // Sýfýrdan baþla

        while (t < 1)
        {
            t += Time.deltaTime / duration;
            float smoothT = t * t * (3f - 2f * t);

            if (cardObj != null) cardObj.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, smoothT);
            yield return null;
        }

        if (cardObj != null) cardObj.transform.localScale = Vector3.one * targetScale; // Sabitle
    }
}