using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI statsText; // Ekranda yazacak skor yazýsý

    [Header("Scene Settings")]
    public string mainMenuSceneName = "Menu Scene"; // Kendi ana menü sahnenin adýný yaz

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void TriggerGameOver()
    {
        // 1. Oyunu durdur (Zamaný dondur)
        Time.timeScale = 0f;

        // 2. Paneli aç ve en öne al
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        // 3. Müzikleri sustur (Ýsteðe baðlý olarak buraya üzücü bir Game Over müziði koyabilirsin)
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();

        // 4. Ýstatistikleri Çek ve Yazdýr
        int enemies = PlayerManager.instance != null ? PlayerManager.instance.enemiesDefeated : 0;
        int paths = PlayerManager.instance != null ? PlayerManager.instance.pathsCompleted : 0;

        if (statsText != null)
        {
            statsText.text = $"The Journey Ends...\n\n" +
                              $"Paths Cleared: <color=#FFD700>{paths}</color>\n" +
                              $"Enemies Defeated: <color=#FF4500>{enemies}</color>";
        }
    }

    // "Patikanýn Baþýna Dön" (Restart) Butonu Ýçin
    public void RestartRun()
    {
        Time.timeScale = 1f; // Zamaný düzeltmeyi unutma!

        // Þu an aktif olan sahneyi (Oyun sahnesini) tamamen baþtan yükler.
        // Bu iþlem haritayý, caný ve desteyi otomatik olarak sýfýrlar.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // "Ana Menüye Dön" Butonu Ýçin
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}