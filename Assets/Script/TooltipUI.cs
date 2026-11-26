using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI instance;

    [Header("UI Components")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI contentText;
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup; // --- YENİ: Şeffaflık kontrolü için ---

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, 15f);
    public float fadeDuration = 0.2f; // --- YENİ: Ne kadar sürede belirsin? ---

    void Awake()
    {
        if (instance == null) instance = this;

        // --- ÇÖZÜM 1: SIRALAMA (Z-INDEX) ---
        // Tooltip'in her zaman en üstte (büyüyen kartların bile önünde) olması için
        // ona özel bir Canvas ekleyip sırasını yükseltiyoruz.
        Canvas c = GetComponent<Canvas>();
        if (c == null) c = gameObject.AddComponent<Canvas>();

        c.overrideSorting = true;
        c.sortingOrder = 300; // Kartlar 100 oluyor, bu 300 olsun ki hep üstte kalsın.

        // UI etkileşimi için Raycaster da lazım (Eğer yoksa)
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        HideTooltip();
    }

    void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // 1. Tooltip'in bulunduğu Canvas'ı veya bir üst objesini referans alıyoruz
            RectTransform parentRect = rectTransform.parent.GetComponent<RectTransform>();

            // 2. Farenin piksel pozisyonunu, Canvas'ın yerel koordinatlarına çeviriyoruz
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                Input.mousePosition,
                Camera.main, // Canvas kameraya bağlı olduğu için ana kamerayı referans veriyoruz
                out Vector2 localMousePos);

            // Pivotu ayarla
            if (rectTransform.pivot != Vector2.zero)
                rectTransform.pivot = Vector2.zero;

            // 3. Pozisyonu güncelle (Z eksenini -50 yaparak en öne çekiyoruz)
            // Artık "position" yerine "localPosition" kullanıyoruz çünkü ebeveynine göre hizaladık
            transform.localPosition = new Vector3(localMousePos.x + offset.x, localMousePos.y + offset.y, -50f);
        }
    }

    public void ShowTooltip(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        contentText.text = text;
        tooltipPanel.SetActive(true);

        // --- YENİ: Hiyerarşide en alta (en öne) al ve Z pozisyonunu kameraya doğru çek ---
        transform.SetAsLastSibling();
        Vector3 pos = transform.localPosition;
        pos.z = -50f; // Z ekseninde objelerden daha önde durmasını sağlar
        transform.localPosition = pos;

        // Boyutu yazıya göre yenile
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
    }

    public void HideTooltip()
    {
        if (canvasGroup != null) canvasGroup.DOKill();
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}