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
        if (energyText != null)
        {
            // Eðer bekleyen bir harcama varsa kýrmýzý ile yanýna ekle
            if (currentPendingCost > 0)
            {
                energyText.text = $"{currentEnergy}/{maxEnergy} <color=red>(-{currentPendingCost})</color>";
            }
            else
            {
                energyText.text = $"{currentEnergy}/{maxEnergy}";
            }
        }

        if (energyFillBar != null)
            energyFillBar.fillAmount = (float)currentEnergy / maxEnergy;
    }
}