using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EnergyManager : MonoBehaviour
{
    public static EnergyManager instance;

    [Header("Settings")]
    public int maxEnergy = 11;
    public int currentEnergy;

    [Header("UI")]
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI maxEnergyText;
    public Image energyFillBar;

    // --- YENÝ: Anlýk hesaplanan bekleyen harcama ---
    private int currentPendingCost = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        UpdateUI();
    }

    public void RefillEnergy()
    {
        currentEnergy += 2;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        // Tur baþýnda masadaki kartlar silindiði için harcama da sýfýrlanýr
        currentPendingCost = 0;
        UpdateUI();
    }

    public bool TrySpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            // Harcama yapýldýkça "bekleyen" maliyeti düþürmeye gerek yok, 
            // çünkü kartlar oynandýkça slotlardan silinecek ve CombatManager yeniden hesaplayýp 0 gönderecek.
            UpdateUI();
            return true;
        }
        return false;
    }

    public void GainEnergy(int amount)
    {

        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
        UpdateUI();
    }

    // --- YENÝ: Dýþarýdan çaðrýlan "Tahmini Maliyet Göster" fonksiyonu ---
    public void UpdatePendingCost(int cost)
    {
        currentPendingCost = cost;
        UpdateUI();
    }

    void UpdateUI()
    {
        // --- 1. MAX ENERJÝ YAZISI (Sabit Arka Plan) ---
        if (maxEnergyText != null)
        {
            maxEnergyText.text = maxEnergy.ToString();
        }

        // --- 2. BAR VE KAYAN YAZI MANTIÐI ---
        if (energyFillBar != null)
        {
            // Barý Doldur
            energyFillBar.fillAmount = (float)currentEnergy / maxEnergy;

            // Barýn Pivot'una göre sol baþlangýç noktasýný bul ve doluluk oranýný ekle
            RectTransform energyRect = energyFillBar.GetComponent<RectTransform>();
            float barWidth = energyRect.rect.width;
            float startX = -barWidth * energyRect.pivot.x;
            float currentEnergyEndX = startX + (barWidth * energyFillBar.fillAmount);

            // Kayan Yazýyý Güncelle ve Pozisyonunu Ayarla
            if (energyText != null)
            {
                // Eðer harcama varsa kýrmýzý ile göster (Artýk /Max kýsmý yok, sadece mevcut yazýyor)
                if (currentPendingCost > 0)
                {
                    energyText.text = $"{currentEnergy} <color=red>(-{currentPendingCost})</color>";
                }
                else
                {
                    energyText.text = currentEnergy.ToString();
                }

                // Yazýyý barýn ucuna it
                RectTransform textRect = energyText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchoredPosition = new Vector2(currentEnergyEndX, textRect.anchoredPosition.y);
                }
            }
        }
    }
}