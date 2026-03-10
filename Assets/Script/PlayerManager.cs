using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int currentGold = 0;

    [Header("Defense")]
    public int currentShield = 0;

    [Header("UI References")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI shieldText;
    public Image healthFillBar;
    public Image shieldFillBar;

    [Header("Visual Effects")]
    public Image damageFlashImage;
    public Image healFlashImage;

    [Header("Passive Stats")]
    public int bonusDamage = 0;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void ResetShield()
    {
        currentShield = 0;
        UpdateUI();
    }

    public void AddShield(int amount)
    {
        currentShield += amount;
        UpdateUI();
        Debug.Log($"Kalkan Kazanýldý: +{amount}");
    }

    // --- GÜNCELLENEN HASAR FONKSÝYONU ---
    public void TakeDamage(int damage)
    {
        bool shieldAbsorbedAll = false; // Kalkan hepsini tankladý mý?

        // Önce kalkandan düþ
        if (currentShield > 0)
        {
            if (currentShield >= damage)
            {
                // Kalkan hasarý tamamen karþýladý
                currentShield -= damage;
                damage = 0;
                shieldAbsorbedAll = true;
            }
            else
            {
                // Kalkan kýrýldý, hasar taþtý
                damage -= currentShield;
                currentShield = 0;
                shieldAbsorbedAll = false;
            }
        }

        // --- GÖRSEL EFEKTLER VE HASAR UYGULAMA ---

        if (damage > 0)
        {
            // >>> CAN HASARI (Aðýr Darbe) <<<
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;

            Debug.Log($"Oyuncu hasar aldý: -{damage}");

            // Kýrmýzý Yanýp Sönme
            if (damageFlashImage != null) StartCoroutine(FlashRoutine(damageFlashImage, Color.red));

            // Güçlü Sarsýntý (0.4 süre, 10 þiddet)
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.4f, 10f);
        }
        else if (shieldAbsorbedAll)
        {
            // >>> KALKAN HASARI (Hafif Darbe / Blok) <<<
            Debug.Log("Kalkan hasarý emdi!");

            // Gri/Metalik Yanýp Sönme (Kalkan Rengi)
            // Ýstersen Color.cyan (Mavi) veya Color.gray yapabilirsin.
            if (damageFlashImage != null) StartCoroutine(FlashRoutine(damageFlashImage, Color.gray));

            // Hafif Sarsýntý (0.2 süre, 3 þiddet - Çok daha az)
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.2f, 3f);
        }

        UpdateUI();

        if (currentHealth <= 0)
        {
            Debug.Log("OYUN BÝTTÝ! KAYBETTÝN.");
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
        if (healFlashImage != null) StartCoroutine(FlashRoutine(healFlashImage, Color.green));
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        // 1. Yazýlarý Güncelle (Yine de kalsýn, barýn üstüne yazýlýr)
        if (healthText != null)
        {
            string shieldInfo = currentShield > 0 ? $" <color=#00FFFF>(+{currentShield})</color>" : "";
            healthText.text = $"{currentHealth}/{maxHealth}{shieldInfo}";
        }

        if (goldText != null) goldText.text = $"{currentGold} G";
        if (shieldText != null) shieldText.gameObject.SetActive(false);

        // --- 2. YENÝ: Barlarý Güncelle ---

        // Can Barý
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = (float)currentHealth / maxHealth;
        }

        // Kalkan Barý (Slay the Spire Mantýðý)
        // Kalkan, can barýnýn bittiði yerden baþlamalýdýr veya can barýnýn üstüne ayrý bir katman olarak binmelidir.
        if (shieldFillBar != null)
        {
            if (currentShield > 0)
            {
                shieldFillBar.gameObject.SetActive(true);

                // Kalkan miktarýný Max HP'ye göre orantýla (Can barý üzerinde kaplayacaðý alan)
                float shieldPercentage = (float)currentShield / maxHealth;

                // Kalkaný, canýn kaldýðý yerden baþlatacak þekilde bir "offset" verebiliriz, 
                // ya da direkt can barýnýn üstünü kaplayacak þekilde ayarlayabiliriz. 
                // En basiti: Kalkan barýnýn doluluðunu (Can + Kalkan) kadar yapýp, Can barýnýn ARKASINA veya ÜSTÜNE koymaktýr.

                // Yöntem: Shield Bar, Can Barý ile ayný boyutta olsun. Doluluðu sadece kalkan kadar olsun.
                shieldFillBar.fillAmount = shieldPercentage;

                // UI'da ShieldFillBar'ýn RectTransform'unda pivot'u Left (X:0) yap ve 
                // kod ile X pozisyonunu Can barýnýn bittiði yere it:
                RectTransform shieldRect = shieldFillBar.GetComponent<RectTransform>();
                RectTransform healthRect = healthFillBar.GetComponent<RectTransform>();

                if (shieldRect != null && healthRect != null)
                {
                    float healthWidth = healthRect.rect.width * healthFillBar.fillAmount;
                    shieldRect.anchoredPosition = new Vector2(healthWidth, shieldRect.anchoredPosition.y);
                }
            }
            else
            {
                shieldFillBar.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator FlashRoutine(Image imageToFlash, Color targetColor)
    {
        Color flashColor = targetColor;
        flashColor.a = 0.5f; // Yarým þeffaflýk
        float t = 0;

        // Parlat
        while (t < 1) { t += Time.deltaTime / 0.1f; imageToFlash.color = Color.Lerp(Color.clear, flashColor, t); yield return null; }

        t = 0;
        // Söndür
        while (t < 1) { t += Time.deltaTime / 0.1f; imageToFlash.color = Color.Lerp(flashColor, Color.clear, t); yield return null; }

        imageToFlash.color = Color.clear;
    }
}