using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlot : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descText;
    public Button buyButton;

    private ItemData myItem;
    private ShopManager manager;

    public void Setup(ItemData item, ShopManager shopManager)
    {
        myItem = item;
        manager = shopManager;

        if (iconImage != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;
        if (costText != null) costText.text = item.cost.ToString() + " G";
        if (descText != null) descText.text = item.description;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    void OnBuyClicked()
    {
        manager.TryBuyItem(myItem, this.gameObject);
    }
}