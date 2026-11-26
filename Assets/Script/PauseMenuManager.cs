using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Açılıp kapanacak olan Ayarlar/Duraklatma Paneli")]
    public GameObject pausePanel;

    [Header("Scene Settings")]
    [Tooltip("Ana menü sahnesinin tam adı (Örn: Menu Scene)")]
    public string mainMenuSceneName = "Menu Scene";

    private bool isPaused = false;
    private bool wasCombatActiveBeforePause = false; // --- YENİ ---

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
            if (isPaused)
            {
                pausePanel.SetActive(true);
                pausePanel.transform.SetAsLastSibling();
                pausePanel.transform.localScale = Vector3.one * 0.85f;
                pausePanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

                CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = pausePanel.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.DOFade(1f, 0.2f).SetUpdate(true);
            }
            else
            {
                CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() =>
                    {
                        pausePanel.SetActive(false);
                    });
                }
                else
                {
                    pausePanel.SetActive(false);
                }
            }
        }

        // Oyunu duraklat (Zamanı dondurur)
        Time.timeScale = isPaused ? 0f : 1f;

        // --- YENİ: Combat Panelini Geçici Kapatma ---
        if (PathManager.instance != null && PathManager.instance.combatPanel != null)
        {
            if (isPaused)
            {
                // Menü açılırken savaş panelinin durumunu kaydet ve kapat
                wasCombatActiveBeforePause = PathManager.instance.combatPanel.activeSelf;
                if (wasCombatActiveBeforePause)
                {
                    PathManager.instance.combatPanel.SetActive(false);
                }
            }
            else
            {
                // Menü kapanırken savaş paneli önceden açıksa geri aç
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

        // --- YENİ EKLENEN SATIR: Ana menüye dönerken savaş müziğini kapat ---
        if (AudioManager.instance != null) AudioManager.instance.StopBGM();

        Debug.Log("Ana Menüye dönülüyor...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}