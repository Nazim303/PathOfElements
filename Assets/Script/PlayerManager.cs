using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;


    [Header("Run Stats (Oyun Sonu İstatistikleri)")]
    public int enemiesDefeated = 0;
    public int pathsCompleted = 0;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int currentGold = 0;

    [Header("Defense")]
    public int currentShield = 0;

    [Header("UI References")]
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI maxHealthText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI shieldText;
    public Image healthFillBar;
    public Image shieldFillBar;

    [Header("Visual Effects")]
    public Image damageFlashImage;
    public Image healFlashImage;
    public GameObject generalHealVFXPrefab; // --- YENİ ---
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
        Debug.Log($"Kalkan Kazanıldı: +{amount}");
    }

    // --- GÜNCELLENEN HASAR FONKSİYONU ---
    public void TakeDamage(int damage)
    {
        bool shieldAbsorbedAll = false; // Kalkan hepsini tankladı mı?

        // Önce kalkandan düş
        if (currentShield > 0)
        {
            if (currentShield >= damage)
            {
                // Kalkan hasarı tamamen karşıladı
                currentShield -= damage;
                damage = 0;
                shieldAbsorbedAll = true;
            }
            else
            {
                // Kalkan kırıldı, hasar taştı
                damage -= currentShield;
                currentShield = 0;
                shieldAbsorbedAll = false;
            }
        }

        // --- GÖRSEL EFEKTLER VE HASAR UYGULAMA ---

        if (damage > 0)
        {
            // >>> CAN HASARI (Ağır Darbe) <<<
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;

            Debug.Log($"Oyuncu hasar aldı: -{damage}");

            // Kırmızı Yanıp Sönme
            if (damageFlashImage != null) StartCoroutine(FlashRoutine(damageFlashImage, Color.red));

            // Güçlü Sarsıntı (0.4 süre, 10 şiddet)
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.4f, 10f);
        }
        else if (shieldAbsorbedAll)
        {
            // >>> KALKAN HASARI (Hafif Darbe / Blok) <<<
            Debug.Log("Kalkan hasarı emdi!");

            // Gri/Metalik Yanıp Sönme (Kalkan Rengi)
            // İstersen Color.cyan (Mavi) veya Color.gray yapabilirsin.
            if (damageFlashImage != null) StartCoroutine(FlashRoutine(damageFlashImage, Color.gray));

            // Hafif Sarsıntı (0.2 süre, 3 şiddet - Çok daha az)
            if (UIShake.instance != null) UIShake.instance.TriggerShake(0.2f, 3f);
        }

        UpdateUI();

        if (currentHealth <= 0)
        {
            Debug.Log("OYUN BİTTİ! KAYBETTİN.");
            // --- YENİ: Ölüm Ekranını Çağır ---
            if (GameOverManager.instance != null) GameOverManager.instance.TriggerGameOver();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateUI();
        if (healFlashImage != null) StartCoroutine(FlashRoutine(healFlashImage, Color.green));

        // --- YENİ: İYİLEŞME EFEKTİ ---
        if (generalHealVFXPrefab != null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            GameObject healFX = Instantiate(generalHealVFXPrefab, parent);

            if (AudioManager.instance != null) AudioManager.instance.PlayHeal();

            // Ekranın ortasında veya oyuncu profilinde çıksın
            healFX.transform.position = healthFillBar.transform.position;
            healFX.transform.localScale = Vector3.one;
            healFX.transform.SetAsLastSibling();
            Destroy(healFX, 2f);
        }
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
        if (goldText != null)
        {
            goldText.transform.DOKill();
            goldText.transform.localScale = Vector3.one;
            goldText.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f, 5, 0.5f);
        }
    }

   public  void UpdateUI()
    {
        if (goldText != null) goldText.text = $"{currentGold} G";
        if (shieldText != null) shieldText.gameObject.SetActive(false);

        // --- YENİ: MAX CAN YAZISI (Sabit Arka Plan Yazısı) ---
        if (maxHealthText != null)
        {
            maxHealthText.text = maxHealth.ToString();
        }

        // --- BARLAR VE KAYAN YAZI MANTIĞI ---
        // --- BARLAR VE KAYAN YAZI MANTIĞI ---
        if (healthFillBar != null)
        {
            // 1. Can Barını Doldur
            healthFillBar.fillAmount = (float)currentHealth / maxHealth;

            RectTransform healthRect = healthFillBar.GetComponent<RectTransform>();
            float barWidth = healthRect.rect.width;

            // YENİ: Barın kendi "X" pozisyonunu da hesaba katarak GÜNCEL sol ucunu buluyoruz!
            float startX = healthRect.anchoredPosition.x - (barWidth * healthRect.pivot.x);

            // Sol uçtan başla ve doluluk oranına göre ilerle
            float currentHealthEndX = startX + (barWidth * healthFillBar.fillAmount);
            // --- DÜZELTME BİTİŞ ---

            // 2. Kayan Yazıyı (Mevcut Can) Güncelle ve Pozisyonunu Ayarla
            if (healthText != null)
            {
                string shieldInfo = currentShield > 0 ? $" <color=#00FFFF>(+{currentShield})</color>" : "";
                healthText.text = $"{currentHealth}{shieldInfo}";

                RectTransform textRect = healthText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // Artık X pozisyonuna düzeltilmiş değeri veriyoruz
                    textRect.anchoredPosition = new Vector2(currentHealthEndX, textRect.anchoredPosition.y);
                }
            }

            // 3. Kalkan Barını Canın Bittiği Yere Ekle
            if (shieldFillBar != null)
            {
                if (currentShield > 0)
                {
                    shieldFillBar.gameObject.SetActive(true);
                    shieldFillBar.fillAmount = (float)currentShield / maxHealth;

                    RectTransform shieldRect = shieldFillBar.GetComponent<RectTransform>();
                    if (shieldRect != null)
                    {
                        // Kalkan da tam olarak canın bittiği o düzeltilmiş noktadan başlayacak
                        shieldRect.anchoredPosition = new Vector2(currentHealthEndX, shieldRect.anchoredPosition.y);
                    }
                }
                else
                {
                    shieldFillBar.gameObject.SetActive(false);
                }
            }
        }
    }

    IEnumerator FlashRoutine(Image imageToFlash, Color targetColor)
    {
        Color flashColor = targetColor;
        flashColor.a = 0.5f; // Yarım şeffaflık
        float t = 0;

        // Parlat
        while (t < 1) { t += Time.deltaTime / 0.1f; imageToFlash.color = Color.Lerp(Color.clear, flashColor, t); yield return null; }

        t = 0;
        // Söndür
        while (t < 1) { t += Time.deltaTime / 0.1f; imageToFlash.color = Color.Lerp(flashColor, Color.clear, t); yield return null; }

        imageToFlash.color = Color.clear;
    }
}