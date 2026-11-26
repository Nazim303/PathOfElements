using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Buton hover efekti (UI_STIL_REHBERI.md): üzerine gelince %5 büyüme + metin altına döner, 0.12 sn.
/// UIThemeTool temalanan butonlara otomatik ekler; elle de eklenebilir.
/// timeScale 0 iken de çalışır (pause/ayar menüleri).
/// </summary>
[DisallowMultipleComponent]
public class UIButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.05f;
    public float pressScale = 0.97f;
    public float duration = 0.12f;
    public bool tintText = true;
    public Color hoverTextColor = new Color32(0xD4, 0xAF, 0x37, 0xFF);

    Selectable selectable;
    TMP_Text label;
    Color labelColor;
    Vector3 baseScale;
    bool hovering;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        label = GetComponentInChildren<TMP_Text>(true);
        if (label != null) labelColor = label.color;
        baseScale = transform.localScale;
    }

    bool Interactable => selectable == null || selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData e)
    {
        if (!Interactable) return;
        hovering = true;
        ScaleTo(hoverScale);
        if (tintText && label != null) label.DOColor(hoverTextColor, duration).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hovering = false;
        ScaleTo(1f);
        if (tintText && label != null) label.DOColor(labelColor, duration).SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (Interactable) ScaleTo(pressScale);
    }

    public void OnPointerUp(PointerEventData e)
    {
        ScaleTo(hovering ? hoverScale : 1f);
    }

    void ScaleTo(float k)
    {
        transform.DOKill();
        transform.DOScale(baseScale * k, duration).SetUpdate(true);
    }

    void OnDisable()
    {
        hovering = false;
        transform.DOKill();
        transform.localScale = baseScale;
        if (label != null)
        {
            label.DOKill();
            label.color = labelColor;
        }
    }
}
