using UnityEngine;
using UnityEngine.EventSystems;

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
            // Eðer slot zaten doluysa fizyon var mý diye bak.
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

                        // A. Eski kartlarý yok et (Eldeki ve Slottaki)
                        Destroy(existingCardTransform.gameObject);
                        Destroy(droppedObject);

                        // (Opsiyonel) Fizyon Efekti oynat
                        if (recipe.fusionVFX != null)
                        {
                            GameObject vfx = Instantiate(recipe.fusionVFX, transform.position, Quaternion.identity, transform);
                            Destroy(vfx, 2f); // 2 saniye sonra sil
                        }

                        // B. Sonuç kartýný oluþtur ve slota yerleþtir
                        GameObject newCardObj = Instantiate(combatManager.cardPrefab, this.transform);

                        // Pivot ve pozisyon ayarlarý (Tam merkeze oturt)
                        RectTransform rect = newCardObj.GetComponent<RectTransform>();
                        rect.pivot = new Vector2(0.5f, 0.5f);
                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.anchoredPosition = Vector2.zero;
                        newCardObj.transform.localRotation = Quaternion.identity;

                        newCardObj.transform.localScale = new Vector3(1, 1, 1);

                        // Veriyi Yükle
                        CardDisplay newCardDisplay = newCardObj.GetComponent<CardDisplay>();
                        newCardDisplay.LoadCard(recipe.resultCard);

                        // C. Backend'e haber ver (Enerji otomatik yeniden hesaplanacak)
                        combatManager.UpdateSlot(slotIndex, recipe.resultCard);
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

            droppedObject.transform.localScale = new Vector3(1, 1, 1);

            if (combatManager != null)
            {
                combatManager.UpdateSlot(slotIndex, droppedCardDisplay.cardData);
            }
        }
    }
}