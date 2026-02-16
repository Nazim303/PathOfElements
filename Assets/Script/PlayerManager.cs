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
        if (healthText != null)
        {
            string shieldInfo = currentShield > 0 ? $" <color=#00FFFF>(+{currentShield})</color>" : "";
            healthText.text = $"{currentHealth}/{maxHealth}{shieldInfo}";
        }

        if (goldText != null) goldText.text = $"{currentGold} G";

        if (shieldText != null) shieldText.gameObject.SetActive(false);
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