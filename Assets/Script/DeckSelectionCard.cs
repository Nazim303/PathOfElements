using UnityEngine;
using UnityEngine.UI;

public class DeckSelectionCard : MonoBehaviour
{
    public CardData myCard;
    private StartingDeckManager manager;
    private bool isSelected = false;

    public void Setup(CardData card, StartingDeckManager mgr, bool selected)
    {
        myCard = card;
        manager = mgr;
        isSelected = selected;

        // Görseli yükle
        CardDisplay display = GetComponent<CardDisplay>();
        if (display != null) display.LoadCard(card);

        // Menüde sürükleme (Draggable) çalýþmasýn, sadece týklama çalýþsýn
        Draggable drag = GetComponent<Draggable>();
        if (drag != null) drag.enabled = false;

        // Buton bileþeni ekle ve týklandýðýnda yer deðiþtir
        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        // Týklanan kartýn nerede olduðuna göre yöneticiden yer deðiþtirmesini iste
        if (isSelected) manager.RemoveFromDeck(this);
        else manager.AddToDeck(this);
    }
}