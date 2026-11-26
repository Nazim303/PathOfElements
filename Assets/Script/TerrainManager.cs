using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager instance;

    [Header("Current State")]
    public ElementType currentTerrain = ElementType.None;

    [Header("Visual Panels (UI Images)")]
    // Çizerlerin hazırlayacağı görsellerin atanacağı paneller
    public GameObject fireTerrainPanel;
    public GameObject waterTerrainPanel;
    public GameObject earthTerrainPanel;
    public GameObject airTerrainPanel;

    void Awake()
    {
        if (instance == null) instance = this;
        SetupTerrainStyles();
    }

    void SetupTerrainStyles()
    {
        ApplyTerrainStyle(fireTerrainPanel, new Color(0.85f, 0.20f, 0.10f, 0.30f));
        ApplyTerrainStyle(waterTerrainPanel, new Color(0.12f, 0.45f, 0.90f, 0.30f));
        ApplyTerrainStyle(earthTerrainPanel, new Color(0.20f, 0.65f, 0.25f, 0.30f));
        ApplyTerrainStyle(airTerrainPanel, new Color(0.25f, 0.80f, 0.88f, 0.28f));
    }

    void ApplyTerrainStyle(GameObject panel, Color themeColor)
    {
        if (panel == null) return;
        Image img = panel.GetComponent<Image>();
        if (img != null)
        {
            // Eğer varsayılan renksiz/düz beyaz panel olarak kalmışsa tematik renkle canlandır
            if (img.sprite == null || (img.color.r > 0.9f && img.color.g > 0.9f && img.color.b > 0.9f))
            {
                img.color = themeColor;
            }
        }
    }

    public void ChangeTerrain(ElementType newTerrain)
    {
        currentTerrain = newTerrain;
        UpdateVisuals();
        Debug.Log($"ZEMİN DEĞİŞTİ! Aktif Zemin: {newTerrain}");
    }

    public void ResetTerrain()
    {
        currentTerrain = ElementType.None;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        UpdatePanelVisual(fireTerrainPanel, currentTerrain == ElementType.Fire);
        UpdatePanelVisual(waterTerrainPanel, currentTerrain == ElementType.Water);
        UpdatePanelVisual(earthTerrainPanel, currentTerrain == ElementType.Earth);
        UpdatePanelVisual(airTerrainPanel, currentTerrain == ElementType.Air);
    }

    void UpdatePanelVisual(GameObject panel, bool isActive)
    {
        if (panel == null) return;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }

        cg.DOKill();

        if (isActive)
        {
            panel.SetActive(true);
            cg.alpha = 0f;
            cg.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
        }
        else
        {
            if (panel.activeSelf && cg.alpha > 0f)
            {
                cg.DOFade(0f, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    panel.SetActive(false);
                });
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }
}
