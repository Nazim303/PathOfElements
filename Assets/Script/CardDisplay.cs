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
    public Image banderimage;
    public TextMeshProUGUI damageText;
    public Image elementIcon; // Ana Element İkonu
    public TextMeshProUGUI descriptionText;

    public Transform costContainer;
    public GameObject costIconPrefab;

    void Start()
    {
        if (cardData != null) LoadCard(cardData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Eğer kart eldeyse (HandArea'nın çocuğuysa)
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
        if (data == null) return;
        this.cardData = data;

        if (nameText != null) nameText.text = data.cardName;
        if (artImage != null) artImage.sprite = data.cardArt;
        if (banderimage != null) banderimage.sprite = data.cardbander;
        if (damageText != null) damageText.text = data.baseDamage.ToString();

        // 1. Önce Element Rengini Belirle
        Color targetColor = GetElementColor(data.element);

        // 2. Ana İkonu Boya (Döngü dışında, bir kere)
        if (elementIcon != null) elementIcon.color = targetColor;

        if (descriptionText != null)
        {
            descriptionText.text = data.description; // CardData'daki yazıyı UI'a aktar
        }

        // 3. Enerji İkonlarını Oluştur ve Boya
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
                // elementIcon.color yerine iconImg.color kullanıyoruz.
                if (iconImg != null)
                {
                    iconImg.color = targetColor;
                }
            }
        }
    }

    public static Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return new Color(1f, 0.28f, 0.22f); // Ateş Kırmızı
            case ElementType.Water: return new Color(0.25f, 0.65f, 1f); // Su Mavisi
            case ElementType.Earth: return new Color(0.55f, 0.78f, 0.3f); // Doğa Yeşili
            case ElementType.Air: return new Color(0.4f, 0.95f, 0.95f); // Hava Camgöbeği
            case ElementType.Magma: return new Color(1f, 0.45f, 0.15f); // Magma Turuncu
            case ElementType.Steam: return new Color(0.75f, 0.85f, 1f); // Buhar
            case ElementType.Ice: return new Color(0.5f, 0.88f, 1f); // Buz Mavisi
            case ElementType.Dust: return new Color(0.85f, 0.75f, 0.5f); // Kum / Toz
            default: return Color.white;
        }
    }
}