using UnityEngine;
using UnityEngine.UI;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager instance;

    [Header("Current State")]
    public ElementType currentTerrain = ElementType.None;

    [Header("Visual Panels (UI Images)")]
    // Çizerlerin hazýrlayacaðý görsellerin atanacaðý paneller
    public GameObject fireTerrainPanel;
    public GameObject waterTerrainPanel;
    public GameObject earthTerrainPanel;
    public GameObject airTerrainPanel;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void ChangeTerrain(ElementType newTerrain)
    {
        currentTerrain = newTerrain;
        UpdateVisuals();
        Debug.Log($"ZEMÝN DEÐÝÞTÝ! Aktif Zemin: {newTerrain}");
    }

    public void ResetTerrain()
    {
        currentTerrain = ElementType.None;
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Sadece aktif olan zeminin panelini aç, diðerlerini kapat
        if (fireTerrainPanel != null) fireTerrainPanel.SetActive(currentTerrain == ElementType.Fire);
        if (waterTerrainPanel != null) waterTerrainPanel.SetActive(currentTerrain == ElementType.Water);
        if (earthTerrainPanel != null) earthTerrainPanel.SetActive(currentTerrain == ElementType.Earth);
        if (airTerrainPanel != null) airTerrainPanel.SetActive(currentTerrain == ElementType.Air);
    }
}