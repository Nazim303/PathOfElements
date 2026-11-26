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

        // Metinler kutuya sığsın: ilk yüklemede auto-size ayarla (bir kez)
        if (!textFitReady)
        {
            FitText(nameText, true);
            FitText(descriptionText, false);
            textFitReady = true;
        }

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

    private bool textFitReady = false;

    /// <summary>
    /// TMP metnini kutusuna sığdırır: prefab'daki boyut üst sınır olur, gerekirse
    /// %45'ine kadar küçülür, yine sığmazsa "..." ile kesilir.
    /// Metin atanmadan ÖNCE ve nesne başına bir kez çağrılmalı (fontSize o an prefab değeridir).
    /// </summary>
    public static void FitText(TMP_Text text, bool singleLine)
    {
        if (text == null) return;

        float authoredSize = text.fontSize;
        float minSize = authoredSize * 0.45f;
        if (text.enableAutoSizing) minSize = Mathf.Min(minSize, text.fontSizeMin);

        text.enableAutoSizing = true;
        text.fontSizeMax = authoredSize;
        text.fontSizeMin = Mathf.Max(1f, minSize);
        text.textWrappingMode = singleLine ? TextWrappingModes.NoWrap : TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    public static Color GetElementColor(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire: return new Color(1f, 0.28f, 0.22f); // Ateş Kırmızı
            case ElementType.Water: return new Color(0.25f, 0.65f, 1f); // Su Mavisi
            case ElementType.Earth: return new Color(0.55f, 0.78f, 0.3f); // Doğa Yeşili
            case ElementType.Air: return new Color(0.4f, 0.95f, 0.95f); // Hava Camgöbeği
            default: return Color.white;
        }
    }
}