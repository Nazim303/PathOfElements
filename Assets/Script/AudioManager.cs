using UnityEngine;
using UnityEngine.Audio; // Audio Mixer için gerekli

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Mixer Settings")]
    public AudioMixer mainMixer; // Inspector'dan MainMixer'ý buraya sürükle

    [Header("Background Musics (BGM)")]
    public AudioClip mapBGM;
    public AudioClip normalCombatBGM;
    public AudioClip eliteCombatBGM;
    public AudioClip bossCombatBGM;

    [Header("Elemental SFX (0=Lvl1, 1=Lvl2, 2=Lvl3)")]
    public AudioClip[] fireTrails;
    public AudioClip[] fireImpacts;
    public AudioClip[] waterTrails;
    public AudioClip[] waterImpacts;
    public AudioClip[] earthTrails;
    public AudioClip[] earthImpacts;
    public AudioClip[] airTrails;
    public AudioClip[] airImpacts;

    [Header("General SFX")]
    public AudioClip enemyDissolve;
    public AudioClip cardDissolve;
    public AudioClip cardPaper;
    public AudioClip purchaseSound;
    public AudioClip clickSound;

    [Header("Combat & Special SFX")]
    public AudioClip greedSFX;
    public AudioClip gamblingSFX;
    public AudioClip exchangeSFX;
    public AudioClip enemyAttackSFX;
    public AudioClip hexodiaAttackSFX;
    public AudioClip healSFX;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // SFX ve Müzik için iki ayrý kaynak oluþturuyoruz
            sfxSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();

            // --- ÇÖZÜM: Yaratýlan kaynaklarý Mixer kanallarýna baðla ---
            if (mainMixer != null)
            {
                // Unity'nin Mixer içindeki "Music" adlý grubu bulup kaynaða atamasý
                AudioMixerGroup[] musicGroups = mainMixer.FindMatchingGroups("Music");
                if (musicGroups.Length > 0) musicSource.outputAudioMixerGroup = musicGroups[0];

                // Unity'nin Mixer içindeki "SFX" adlý grubu bulup kaynaða atamasý
                AudioMixerGroup[] sfxGroups = mainMixer.FindMatchingGroups("SFX");
                if (sfxGroups.Length > 0) sfxSource.outputAudioMixerGroup = sfxGroups[0];
            }

            DontDestroyOnLoad(gameObject); // Sahneler arasý silinmesin
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- SES AYAR (MIXER) KONTROLLERÝ ---
    public void SetMusicVolume(float volume)
    {
        if (mainMixer != null) mainMixer.SetFloat("MusicVol", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        if (mainMixer != null) mainMixer.SetFloat("SFXVol", Mathf.Log10(volume) * 20);
    }

    // --- MÜZÝK (BGM) KONTROLÜ ---
    // --- GÜNCELLENEN MÜZÝK (BGM) KONTROLÜ ---
    // (float volume = 1f) yazarak bu deðeri opsiyonel yaptýk. 
    // Sen bir deðer girmezsen %100 (1f) çalacak, girersen senin deðerinde çalacak.
    public void PlayBGM(AudioClip bgmClip, float volume = 1f)
    {
        if (bgmClip == null) return;

        // Eðer istenen müzik zaten çalýyorsa baþa sarmamasý için iptal et
        if (musicSource.clip == bgmClip && musicSource.isPlaying)
        {
            // Müzik zaten çalýyorsa sadece ses seviyesini güncelle ve çýk
            musicSource.volume = volume;
            return;
        }

        musicSource.clip = bgmClip;
        musicSource.volume = volume; // YENÝ: Ses seviyesini kaynaða ata
        musicSource.loop = true;
        musicSource.Play();
    }

    // Bu fonksiyon çalan BGM'yi (müziði) tamamen kapatýr
    public void StopBGM()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null; // Hafýzayý temizle
        }
    }

    // --- GENEL EFEKTLER ---
    public void PlayClick() { if (clickSound != null) sfxSource.PlayOneShot(clickSound); }
    public void PlayPurchase() { if (purchaseSound != null) sfxSource.PlayOneShot(purchaseSound); }
    public void PlayCardPaper() { if (cardPaper != null) sfxSource.PlayOneShot(cardPaper); }
    public void PlayEnemyDissolve() { if (enemyDissolve != null) sfxSource.PlayOneShot(enemyDissolve); }
    public void PlayCardDissolve() { if (cardDissolve != null) sfxSource.PlayOneShot(cardDissolve); }

    // --- YENÝ EFEKTLER ---
    public void PlayGreed() { if (greedSFX != null) sfxSource.PlayOneShot(greedSFX); }
    public void PlayGambling() { if (gamblingSFX != null) sfxSource.PlayOneShot(gamblingSFX); }
    public void PlayExchange() { if (exchangeSFX != null) sfxSource.PlayOneShot(exchangeSFX); }
    public void PlayEnemyAttack() { if (enemyAttackSFX != null) sfxSource.PlayOneShot(enemyAttackSFX); }
    public void PlayHexodiaAttack() { if (hexodiaAttackSFX != null) sfxSource.PlayOneShot(hexodiaAttackSFX); }
    public void PlayHeal() { if (healSFX != null) sfxSource.PlayOneShot(healSFX); }

    // --- ELEMENT EFEKTLERÝ ---
    public void PlayTrailSound(ElementType element, int powerLevel)
    {
        int index = Mathf.Clamp(powerLevel - 1, 0, 2);
        AudioClip clipToPlay = null;

        switch (element)
        {
            case ElementType.Fire: if (fireTrails.Length > index) clipToPlay = fireTrails[index]; break;
            case ElementType.Water: if (waterTrails.Length > index) clipToPlay = waterTrails[index]; break;
            case ElementType.Earth: if (earthTrails.Length > index) clipToPlay = earthTrails[index]; break;
            case ElementType.Air: if (airTrails.Length > index) clipToPlay = airTrails[index]; break;
        }

        if (clipToPlay != null) sfxSource.PlayOneShot(clipToPlay);
    }

    public void PlayImpactSound(ElementType element, int powerLevel)
    {
        int index = Mathf.Clamp(powerLevel - 1, 0, 2);
        AudioClip clipToPlay = null;

        switch (element)
        {
            case ElementType.Fire: if (fireImpacts.Length > index) clipToPlay = fireImpacts[index]; break;
            case ElementType.Water: if (waterImpacts.Length > index) clipToPlay = waterImpacts[index]; break;
            case ElementType.Earth: if (earthImpacts.Length > index) clipToPlay = earthImpacts[index]; break;
            case ElementType.Air: if (airImpacts.Length > index) clipToPlay = airImpacts[index]; break;
        }

        if (clipToPlay != null) sfxSource.PlayOneShot(clipToPlay);
    }
}