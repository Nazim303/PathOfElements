using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

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

    // --- YENİ: Anlık hesaplanan bekleyen harcama ---
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

        // Tur başında masadaki kartlar silindiği için harcama da sıfırlanır
        currentPendingCost = 0;
        UpdateUI();
    }

    public bool TrySpendEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            // Harcama yapıldıkça "bekleyen" maliyeti düşürmeye gerek yok, 
            // çünkü kartlar oynandıkça slotlardan silinecek ve CombatManager yeniden hesaplayıp 0 gönderecek.
            UpdateUI();
            return true;
        }

        if (energyText != null)
        {
            energyText.transform.DOComplete();
            energyText.transform.DOShakePosition(0.3f, new Vector3(6f, 0, 0), 10, 90f, false, true);
        }
        return false;
    }

    // Her savaşın başında enerji dolar (önceki savaştan kalan açık taşınmaz)
    public void RefillToMax()
    {
        currentEnergy = maxEnergy;
        currentPendingCost = 0;
        UpdateUI();
    }

    public void GainEnergy(int amount)
    {

        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
        UpdateUI();
    }

    // --- YENİ: Dışarıdan çağrılan "Tahmini Maliyet Göster" fonksiyonu ---
    public void UpdatePendingCost(int cost)
    {
        currentPendingCost = cost;
        UpdateUI();
    }


    // --- YENİ: Dükkandan veya bir Event'ten Max Enerjiyi kalıcı olarak artırma ---
    public void IncreaseMaxEnergy(int amount)
    {
        maxEnergy += amount;
        currentEnergy += amount; // Yeni gelen enerjiyi anında kullanıma sun
        UpdateUI();
        Debug.Log($"Max Enerji {amount} artırıldı! Yeni Max: {maxEnergy}");
    }

    void UpdateUI()
    {
        // --- 1. MAX ENERJİ YAZISI (Sabit Arka Plan) ---
        if (maxEnergyText != null)
        {
            maxEnergyText.text = maxEnergy.ToString();
        }

      
        if (energyFillBar != null)
        {
            // Barı Doldur
            energyFillBar.fillAmount = (float)currentEnergy / maxEnergy;

            // --- DÜZELTİLEN MANTIK ---
            RectTransform energyRect = energyFillBar.GetComponent<RectTransform>();
            float barWidth = energyRect.rect.width;

            // YENİ: Barın X pozisyonunu hesaba kat
            float startX = energyRect.anchoredPosition.x - (barWidth * energyRect.pivot.x);
            float currentEnergyEndX = startX + (barWidth * energyFillBar.fillAmount);

            // Kayan Yazıyı Güncelle ve Pozisyonunu Ayarla
            if (energyText != null)
            {
                if (currentPendingCost > 0)
                {
                    energyText.text = $"{currentEnergy} <color=red>(-{currentPendingCost})</color>";
                }
                else
                {
                    energyText.text = currentEnergy.ToString();
                }

                // Yazıyı barın ucuna it
                RectTransform textRect = energyText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchoredPosition = new Vector2(currentEnergyEndX, textRect.anchoredPosition.y);
                }
            }
        }
    }
}