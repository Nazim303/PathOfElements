using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        // Kayıtlı ses değerlerini yükle (Daha önce kaydedilmemişse varsayılan 0.75f yani %75 yap)
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // Slider'ların pozisyonunu kayıtlı değere getir
        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // Başlangıçta ses seviyelerini AudioManager'a uygula
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        // Oyuncu Slider'ı sağa sola çektikçe (onValueChanged) fonksiyonları otomatik tetikle
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(value);
        }
        PlayerPrefs.SetFloat("MusicVolume", value); // Ayarı hafızaya kaydet
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
        PlayerPrefs.SetFloat("SFXVolume", value); // Ayarı hafızaya kaydet
    }
}
