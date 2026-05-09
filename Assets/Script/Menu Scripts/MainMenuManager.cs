using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Oyunun oynandýðý ana sahnenin tam adý (Örn: GameScene)")]
    public string gameSceneName = "Sample Scene";

    [Header("Video Settings")]
    public VideoPlayer backgroundVideoPlayer;
    public VideoClip introVideoClip;
    public VideoClip loopVideoClip;

    // --- YENÝ EKLENEN KISIM ---
    [Header("UI Settings")]
    [Tooltip("Ýntro oynarken gizlenecek olan menü butonlarýnýn bulunduðu ana obje (Örn: Buttons Panel)")]
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

        // 1. DURUM: Oyun yeni açýldýysa ve intro henüz oynamadýysa
        if (!hasPlayedIntro && introVideoClip != null)
        {
            // --- YENÝ: Ýntro oynarken butonlarý gizle ---
            if (menuButtonsPanel != null) menuButtonsPanel.SetActive(false);

            backgroundVideoPlayer.clip = introVideoClip;
            backgroundVideoPlayer.isLooping = false;

            backgroundVideoPlayer.loopPointReached += OnIntroFinished;

            backgroundVideoPlayer.Play();
        }
        // 2. DURUM: Oyuncu oyun içinden ana menüye döndüyse (Ýntro zaten oynadýysa)
        else if (loopVideoClip != null)
        {
            // --- YENÝ: Ýntro daha önce izlendiyse butonlarý direkt göster ---
            if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);

            PlayLoopVideo();
        }
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        hasPlayedIntro = true;
        vp.loopPointReached -= OnIntroFinished;

        // --- YENÝ: Ýntro bittiðinde butonlarý görünür yap ---
        if (menuButtonsPanel != null) menuButtonsPanel.SetActive(true);

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
        if(hasPlayedTutorial == false)
        {
            SceneManager.LoadScene(gameSceneName);
            hasPlayedTutorial = true;
        }
        else
        {
            SceneManager.LoadScene("SampleScene");

        }
       

        
    }

    public void OpenJournal()
    {
        Debug.Log("Günlük açýlýyor...");
        if (JournalManager.instance != null)
        {
            JournalManager.instance.OpenJournal();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Oyun Kapatýlýyor...");
        Application.Quit();
    }
}