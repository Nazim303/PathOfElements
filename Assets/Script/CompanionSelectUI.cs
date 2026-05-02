using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CompanionSelectUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public Image cardArt;
    public Image cardBander;
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

        if (cardBander != null && card.cardbander != null)
        {
            cardBander.sprite = card.cardbander;
        }

        selectButton.onClick.RemoveAllListeners();
        // YENÝ: Artýk animasyonun nereden baþlayacaðýný bilmesi için kendi transform'unu da gönderiyor
        selectButton.onClick.AddListener(() => manager.OnCompanionSelected(myCard, this.transform));
    }
}