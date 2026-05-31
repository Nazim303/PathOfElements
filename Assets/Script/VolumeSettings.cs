using UnityEngine;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    void Start()
    {
        // Kayýtlý ses deðerlerini yükle (Daha önce kaydedilmemiþse varsayýlan 0.75f yani %75 yap)
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        // Slider'larýn pozisyonunu kayýtlý deðere getir
        if (musicSlider != null) musicSlider.value = savedMusic;
        if (sfxSlider != null) sfxSlider.value = savedSFX;

        // Baþlangýçta ses seviyelerini AudioManager'a uygula
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);

        // Oyuncu Slider'ý saða sola çektikçe (onValueChanged) fonksiyonlarý otomatik tetikle
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(value);
        }
        PlayerPrefs.SetFloat("MusicVolume", value); // Ayarý hafýzaya kaydet
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(value);
        }
        PlayerPrefs.SetFloat("SFXVolume", value); // Ayarý hafýzaya kaydet
    }
}
