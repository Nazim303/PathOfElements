using UnityEngine;
using UnityEngine.UI;

public class JournalItemUI : MonoBehaviour
{
    public Image iconImage;
    public Button itemButton;

    // Arka planda tutulacak veriler
    private string itemName;
    private string itemDescription;
    private Sprite itemArt;

    public void SetupCard(CardData card)
    {
        itemName = card.cardName;
        itemArt = card.cardArt;

        // EÐER uzun journal yazýsý boþ býrakýlmýþsa, hata vermesin diye normal kýsa yazýyý (description) kullanýr
        if (!string.IsNullOrEmpty(card.journalDescription))
            itemDescription = card.journalDescription;
        else
            itemDescription = card.description;

        if (iconImage != null) iconImage.sprite = itemArt;

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnClickItem);
    }

    public void SetupEnemy(EnemyData enemy)
    {
        itemName = enemy.enemyName;
        itemArt = enemy.enemyArt;

        // Düþmanýn hikayesi
        if (!string.IsNullOrEmpty(enemy.journalDescription))
            itemDescription = enemy.journalDescription;
        else
            itemDescription = "Bu varlýk hakkýnda pek bir þey bilinmiyor...";

        if (iconImage != null) iconImage.sprite = itemArt;

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnClickItem);
    }

    void OnClickItem()
    {
        // Týklandýðýnda JournalManager'a bilgileri yolla ve popup'ý aç
        if (JournalManager.instance != null)
        {
            JournalManager.instance.ShowDetailPopup(itemName, itemArt, itemDescription);
        }
    }
}