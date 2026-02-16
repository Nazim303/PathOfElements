using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public CardData cardData;

    [Header("UI Components")]
    public TextMeshProUGUI nameText;
    public Image artImage;
    public TextMeshProUGUI damageText;
    public Image elementIcon; // Ana Element Ýkonu

    public Transform costContainer;
    public GameObject costIconPrefab;

    void Start()
    {
        if (cardData != null) LoadCard(cardData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Eðer kart eldeyse (HandArea'nýn çocuðuysa)
        HandVisualManager visualManager = GetComponentInParent<HandVisualManager>();
        if (visualManager != null)
        {
            visualManager.OnCardEnter(GetComponent<RectTransform>());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HandVisualManager visualManager = GetComponentInParent<HandVisualManager>();
        if (visualManager != null)
        {
            visualManager.OnCardExit(GetComponent<RectTransform>());
        }
    }

    public void LoadCard(CardData data)
    {
        this.cardData = data;

        nameText.text = data.cardName;
        artImage.sprite = data.cardArt;
        if (damageText != null) damageText.text = data.baseDamage.ToString();

        // 1. Önce Element Rengini Belirle
        Color targetColor = Color.white; // Varsayýlan (None)
        switch (data.element)
        {
            case ElementType.Fire: targetColor = Color.red; break;
            case ElementType.Water: targetColor = Color.blue; break;
            case ElementType.Earth: targetColor = new Color(0.6f, 0.4f, 0.2f); break; // Kahverengi
            case ElementType.Air: targetColor = Color.cyan; break;
            default: targetColor = Color.gray; break; // None veya diðerleri için Gri
        }

        // 2. Ana Ýkonu Boya (Döngü dýþýnda, bir kere)
        if (elementIcon != null) elementIcon.color = targetColor;

        // 3. Enerji Ýkonlarýný Oluþtur ve Boya
        if (costContainer != null && costIconPrefab != null)
        {
            // Eskileri temizle
            foreach (Transform child in costContainer) Destroy(child.gameObject);

            // Yenileri ekle
            for (int i = 0; i < data.energyCost; i++)
            {
                GameObject iconObj = Instantiate(costIconPrefab, costContainer);
                Image iconImg = iconObj.GetComponent<Image>();

                // --- DÜZELTME BURADA ---
                // elementIcon.color yerine iconImg.color kullanýyoruz.
                if (iconImg != null)
                {
                    iconImg.color = targetColor;
                }
            }
        }
    }
}