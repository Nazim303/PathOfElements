using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("Defeat UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI statsText;

    [Header("Victory UI References")]
    public GameObject victoryPanel;       // --- YENÝ: Kazanma Paneli ---
    public TextMeshProUGUI victoryStatsText; // --- YENÝ: Kazanma Ýstatistikleri ---

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false); // YENÝ
    }

    // --- KAYBETME DURUMU ---
    public void TriggerGameOver()
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        if (AudioManager.instance != null) AudioManager.instance.StopBGM();

        int enemies = PlayerManager.instance != null ? PlayerManager.instance.enemiesDefeated : 0;
        int paths = PlayerManager.instance != null ? PlayerManager.instance.pathsCompleted : 0;

        if (statsText != null)
        {
            statsText.text = $"The Journey Ends...\n\n" +
                             $"Paths Cleared: <color=#FFD700>{paths}</color>\n" +
                             $"Enemies Defeated: <color=#FF4500>{enemies}</color>";
        }
    }

    // --- YENÝ: KAZANMA DURUMU ---
    public void TriggerVictory()
    {
        Time.timeScale = 0f; // Zamaný durdur

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
        }

        if (AudioManager.instance != null) AudioManager.instance.StopBGM();
        // Ýstersen buraya AudioManager.instance.PlayVictorySFX(); gibi kutlama sesi ekleyebilirsin

        int enemies = PlayerManager.instance != null ? PlayerManager.instance.enemiesDefeated : 0;
        int paths = PlayerManager.instance != null ? PlayerManager.instance.pathsCompleted : 0;

        if (victoryStatsText != null)
        {
            victoryStatsText.text = $"You Survived and Won!\n\n" +
                                    $"Paths Cleared: <color=#FFD700>{paths}</color>\n" +
                                    $"Enemies Defeated: <color=#FF4500>{enemies}</color>";
        }
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}