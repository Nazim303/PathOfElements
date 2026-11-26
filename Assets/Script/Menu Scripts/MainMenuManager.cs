using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Oyunun oynandığı ana sahnenin tam adı (Örn: SampleScene)")]
    public string gameSceneName = "SampleScene";
    public string tutorialSceneName = "NewTutorialScene";

    [Header("Video Settings")]
    public VideoPlayer backgroundVideoPlayer;
    public VideoClip introVideoClip;
    public VideoClip loopVideoClip;

    // --- YENİ EKLENEN KISIM ---
    [Header("UI Settings")]
    [Tooltip("İntro oynarken gizlenecek olan menü butonlarının bulunduğu ana obje (Örn: Buttons Panel)")]
    public GameObject menuButtonsPanel;

    private static bool hasPlayedIntro = false;
    private static bool hasPlayedTutorial = false;

    void Start()
    {
        SetupVideoPlayer();
    }

    void SetupVideoPlayer()
    {
        if (backgroundVideoPlayer == null) return;

        // 1. DURUM: Oyun yeni açıldıysa ve intro henüz oynamadıysa
        if (!hasPlayedIntro && introVideoClip != null)
        {
            // --- YENİ: İntro oynarken butonları gizle ---
            if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);

            backgroundVideoPlayer.clip = introVideoClip;
            backgroundVideoPlayer.isLooping = false;

            backgroundVideoPlayer.loopPointReached += OnIntroFinished;

            backgroundVideoPlayer.Play();
        }
        // 2. DURUM: Oyuncu oyun içinden ana menüye döndüyse (İntro zaten oynadıysa)
        else if (loopVideoClip != null)
        {
            // --- YENİ: İntro daha önce izlendiyse butonları direkt göster ---
            if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);

            PlayLoopVideo();
        }
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        hasPlayedIntro = true;
        vp.loopPointReached -= OnIntroFinished;

        // --- YENİ: İntro bittiğinde butonları yumuşakça görünür yap ---
        if (menuButtonsPanel != null)
        {
            menuButtonsPanel.SetActive(true);
            CanvasGroup cg = menuButtonsPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = menuButtonsPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.4f);
        }

        PlayLoopVideo();
    }

    void PlayLoopVideo()
    {
        if (backgroundVideoPlayer != null && loopVideoClip != null)
        {
            backgroundVideoPlayer.clip = loopVideoClip;
            backgroundVideoPlayer.isLooping = true;
            backgroundVideoPlayer.Play();
        }
    }

    public void StartGame()
    {
        Debug.Log("Oyun sahnesine geçiliyor...");
        if (!hasPlayedTutorial)
        {
            hasPlayedTutorial = true;
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OpenJournal()
    {
        Debug.Log("Günlük açılıyor...");
        if (JournalManager.instance != null)
        {
            JournalManager.instance.OpenJournal();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Oyun Kapatılıyor...");
        Application.Quit();
    }
}