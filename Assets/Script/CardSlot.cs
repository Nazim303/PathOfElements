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
        CardDisplay cardDisplay = droppedObject.GetComponent<CardDisplay>();

        if (draggable != null && cardDisplay != null)
        {

            if (transform.childCount > 0)
            {
                return;
            }

            // 1. Eski slotu temizle (Varsa)
            CardSlot oldSlot = droppedObject.transform.parent.GetComponent<CardSlot>();
            if (oldSlot != null && combatManager != null)
            {
                combatManager.ClearSlot(oldSlot.slotIndex);
            }

            // 2. Kartý bu slotun çocuðu yap
            droppedObject.transform.SetParent(this.transform);

            // --- DÜZELTME BAÞLANGICI: POZÝSYON VE PIVOT AYARLARI ---

            RectTransform rect = droppedObject.GetComponent<RectTransform>();

            // Pivot'u (Dönme/Hizalama Noktasý) tam ortaya al (0.5, 0.5)
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Anchorlarý (Çapalarý) slotun tam ortasýna kilitle
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);

            // Pozisyonu sýfýrla (Tam merkeze oturmasý için)
            rect.anchoredPosition = Vector2.zero;

            // (Opsiyonel) Kartýn açýsýný düzelt (LayoutGroup bazen bozabilir)
            droppedObject.transform.localRotation = Quaternion.identity;

            // --- DÜZELTME BÝTÝÞÝ ---

            // Backend'e haber ver
            if (combatManager != null)
            {
                combatManager.UpdateSlot(slotIndex, cardDisplay.cardData);
            }
        }
    }
}