using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompanionSelectUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText; // Özel gücünü yazar
    public Image cardArt;
    public Button selectButton;

    private CardData myCard;
    private CompanionEvent manager;

    public void Setup(CardData card, CompanionEvent eventManager)
    {
        myCard = card;
        manager = eventManager;

        if (nameText != null) nameText.text = card.cardName;
        if (descText != null) descText.text = card.description;
        if (cardArt != null) cardArt.sprite = card.cardArt;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => manager.OnCompanionSelected(myCard));
    }
}