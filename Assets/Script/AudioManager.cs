using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

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

    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            DontDestroyOnLoad(gameObject); // Sahneler arasý silinmesin
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayClick() { if (clickSound != null) audioSource.PlayOneShot(clickSound); }
    public void PlayPurchase() { if (purchaseSound != null) audioSource.PlayOneShot(purchaseSound); }
    public void PlayCardPaper() { if (cardPaper != null) audioSource.PlayOneShot(cardPaper); }
    public void PlayEnemyDissolve() { if (enemyDissolve != null) audioSource.PlayOneShot(enemyDissolve); }
    public void PlayCardDissolve() { if (cardDissolve != null) audioSource.PlayOneShot(cardDissolve); }

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

        if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
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

        if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
    }
}