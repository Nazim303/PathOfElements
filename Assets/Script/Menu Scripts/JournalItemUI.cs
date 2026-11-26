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

        // EĞER uzun journal yazısı boş bırakılmışsa, hata vermesin diye normal kısa yazıyı (description) kullanır
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

        // Düşmanın hikayesi
        if (!string.IsNullOrEmpty(enemy.journalDescription))
            itemDescription = enemy.journalDescription;
        else
            itemDescription = "Not much is known about this entity...";

        if (iconImage != null) iconImage.sprite = itemArt;

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnClickItem);
    }

    void OnClickItem()
    {
        // Tıklandığında JournalManager'a bilgileri yolla ve popup'ı aç
        if (JournalManager.instance != null)
        {
            JournalManager.instance.ShowDetailPopup(itemName, itemArt, itemDescription);
        }
    }
}