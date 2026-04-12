using UnityEngine;
using UnityEngine.SceneManagement; // --- YENÝ: Sahne geçiþleri için gerekli kütüphane ---

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Oyunun oynandýðý ana sahnenin tam adý (Örn: GameScene)")]
    public string gameSceneName = "Sample Scene";

    public void StartGame()
    {
        // Paneli kapatmak yerine direkt ana oyun sahnesini yüklüyoruz
        Debug.Log("Oyun sahnesine geçiliyor...");
        SceneManager.LoadScene(gameSceneName);
    }

    // --- ÝLERÝDE EKLENECEK GÜNLÜK (JOURNAL) ÝÇÝN HAZIRLIK ---
    public void OpenJournal()
    {
        // Bu fonksiyonu þimdiden yazdýk, ileride Journal panelini buraya baðlayacaðýz.
        Debug.Log("Günlük (Journal) sayfasý açýlýyor... (Sistem hazýrlandýðýnda çalýþacak)");
    }

    public void QuitGame()
    {
        Debug.Log("Oyun Kapatýlýyor...");
        Application.Quit();
    }
}