using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("Defeat UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI statsText;

    [Header("Victory UI References")]
    public GameObject victoryPanel;       // --- YENİ: Kazanma Paneli ---
    public TextMeshProUGUI victoryStatsText; // --- YENİ: Kazanma İstatistikleri ---

    [Header("Scene Settings")]
    public string mainMenuSceneName = "Menu Scene";

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false); // YENİ
    }

    // --- KAYBETME DURUMU ---
    public void TriggerGameOver()
    {
        Time.timeScale = 0f;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
            gameOverPanel.transform.localScale = Vector3.one * 0.85f;
            gameOverPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

            CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = gameOverPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.3f).SetUpdate(true);
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

    // --- YENİ: KAZANMA DURUMU ---
    public void TriggerVictory()
    {
        Time.timeScale = 0f; // Zamanı durdur

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
            victoryPanel.transform.localScale = Vector3.one * 0.85f;
            victoryPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

            CanvasGroup cg = victoryPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = victoryPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.3f).SetUpdate(true);
        }

        if (AudioManager.instance != null) AudioManager.instance.StopBGM();
        // İstersen buraya AudioManager.instance.PlayVictorySFX(); gibi kutlama sesi ekleyebilirsin

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