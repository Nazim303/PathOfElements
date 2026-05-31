using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Açýlýp kapanacak olan Ayarlar/Duraklatma Paneli")]
    public GameObject pausePanel;

    [Header("Scene Settings")]
    [Tooltip("Ana menü sahnesinin tam adý (Örn: MainMenu)")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool wasCombatActiveBeforePause = false; // --- YENÝ ---

    void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
            if (isPaused) pausePanel.transform.SetAsLastSibling(); // Paneli en öne al
        }

        // Oyunu duraklat (Zamaný dondurur)
        Time.timeScale = isPaused ? 0f : 1f;

        // --- YENÝ: Combat Panelini Geçici Kapatma ---
        if (PathManager.instance != null && PathManager.instance.combatPanel != null)
        {
            if (isPaused)
            {
                // Menü açýlýrken savaþ panelinin durumunu kaydet ve kapat
                wasCombatActiveBeforePause = PathManager.instance.combatPanel.activeSelf;
                if (wasCombatActiveBeforePause)
                {
                    PathManager.instance.combatPanel.SetActive(false);
                }
            }
            else
            {
                // Menü kapanýrken savaþ paneli önceden açýksa geri aç
                if (wasCombatActiveBeforePause)
                {
                    PathManager.instance.combatPanel.SetActive(true);
                }
            }
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        // --- YENÝ EKLENEN SATIR: Ana menüye dönerken savaþ müziðini kapat ---
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();

        Debug.Log("Ana Menüye dönülüyor...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}