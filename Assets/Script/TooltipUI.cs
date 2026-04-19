using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // Coroutine için gerekli

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI instance;

    [Header("UI Components")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI contentText;
    public RectTransform rectTransform;
    public CanvasGroup canvasGroup; // --- YENÝ: Þeffaflýk kontrolü için ---

    [Header("Settings")]
    public Vector2 offset = new Vector2(15f, 15f);
    public float fadeDuration = 0.2f; // --- YENÝ: Ne kadar sürede belirsin? ---

    void Awake()
    {
        if (instance == null) instance = this;

        // --- ÇÖZÜM 1: SIRALAMA (Z-INDEX) ---
        // Tooltip'in her zaman en üstte (büyüyen kartlarýn bile önünde) olmasý için
        // ona özel bir Canvas ekleyip sýrasýný yükseltiyoruz.
        Canvas c = GetComponent<Canvas>();
        if (c == null) c = gameObject.AddComponent<Canvas>();

        c.overrideSorting = true;
        c.sortingOrder = 300; // Kartlar 100 oluyor, bu 300 olsun ki hep üstte kalsýn.

        // UI etkileþimi için Raycaster da lazým (Eðer yoksa)
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        HideTooltip();
    }

    void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // 1. Tooltip'in bulunduðu Canvas'ý veya bir üst objesini referans alýyoruz
            RectTransform parentRect = rectTransform.parent.GetComponent<RectTransform>();

            // 2. Farenin piksel pozisyonunu, Canvas'ýn yerel koordinatlarýna çeviriyoruz
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                Input.mousePosition,
                Camera.main, // Canvas kameraya baðlý olduðu için ana kamerayý referans veriyoruz
                out Vector2 localMousePos);

            // Pivotu ayarla
            if (rectTransform.pivot != Vector2.zero)
                rectTransform.pivot = Vector2.zero;

            // 3. Pozisyonu güncelle (Z eksenini -50 yaparak en öne çekiyoruz)
            // Artýk "position" yerine "localPosition" kullanýyoruz çünkü ebeveynine göre hizaladýk
            transform.localPosition = new Vector3(localMousePos.x + offset.x, localMousePos.y + offset.y, -50f);
        }
    }

    public void ShowTooltip(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        contentText.text = text;
        tooltipPanel.SetActive(true);

        // --- YENÝ: Hiyerarþide en alta (en öne) al ve Z pozisyonunu kameraya doðru çek ---
        transform.SetAsLastSibling();
        Vector3 pos = transform.localPosition;
        pos.z = -50f; // Z ekseninde objelerden daha önde durmasýný saðlar
        transform.localPosition = pos;

        // Boyutu yazýya göre yenile
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine());
        }
    }

    public void HideTooltip()
    {
        // Gizlerken anýnda kapansýn ki takýlma hissi olmasýn
        tooltipPanel.SetActive(false);
    }

    IEnumerator FadeInRoutine()
    {
        canvasGroup.alpha = 0f; // Tamamen þeffaf baþla
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}