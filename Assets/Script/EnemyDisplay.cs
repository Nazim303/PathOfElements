using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // YENİ: List yapısı için eklendi

public class EnemyDisplay : MonoBehaviour
{
    [Header("Data")]
    public EnemyData enemyData;
    public int currentHealth;
    public ElementType currentElement;

    [Header("UI Components")]
    public Transform visualRoot;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI healthText;
    public Image artImage;
    public Image banderimage;
    public Image elementIcon;

    public void SetupEnemy(EnemyData data, int maxHealth)
    {
        this.enemyData = data;
        this.currentHealth = maxHealth;
        this.currentElement = data != null ? data.element : ElementType.None;

        if (nameText != null) nameText.text = data.enemyName;
        if (banderimage != null)
        {
            banderimage.sprite = data.enemybander;
            banderimage.preserveAspect = true;
        }
        if (artImage != null)
        {
            artImage.sprite = data.enemyArt;
            artImage.preserveAspect = true;
        }

        if (visualRoot != null) visualRoot.localPosition = Vector3.zero;

        UpdateElementVisual(this.currentElement);
        UpdateHealthUI(currentHealth);

        TooltipTrigger tooltip = GetComponent<TooltipTrigger>();
        if (tooltip == null && visualRoot != null) tooltip = visualRoot.GetComponent<TooltipTrigger>();

        if (tooltip != null)
        {
            string info = $"DMG: {data.damage}\nGain: {data.goldReward} G";
            tooltip.SetTooltipText(info);
        }
    }

    public void UpdateHealthUI(int healthVal)
    {
        int displayVal = Mathf.Max(0, healthVal);
        if (healthText != null) healthText.text = displayVal.ToString();
    }

    public void UpdateElementVisual(ElementType type)
    {
        if (elementIcon == null) return;

        switch (type)
        {
            case ElementType.Fire: elementIcon.color = new Color(1f, 0.25f, 0.25f); break;
            case ElementType.Water: elementIcon.color = new Color(0.25f, 0.6f, 1f); break;
            case ElementType.Earth: elementIcon.color = new Color(0.6f, 0.4f, 0.2f); break;
            case ElementType.Air: elementIcon.color = new Color(0.3f, 0.85f, 0.7f); break;
            case ElementType.Magma: elementIcon.color = new Color(1f, 0.28f, 0f); break;
            case ElementType.Steam: elementIcon.color = new Color(0.75f, 0.85f, 0.95f); break;
            case ElementType.Ice: elementIcon.color = new Color(0.4f, 0.85f, 1f); break;
            case ElementType.Dust: elementIcon.color = new Color(0.8f, 0.7f, 0.45f); break;
            default: elementIcon.color = Color.white; break;
        }
    }

    public void ChangeElement(ElementType newElement)
    {
        this.currentElement = newElement;
        UpdateElementVisual(newElement);
    }

    public IEnumerator TakeDamageEffect()
    {
        if (artImage != null) artImage.color = Color.red;

        Transform target = (visualRoot != null) ? visualRoot : transform;

        Vector3 originalPos = Vector3.zero;
        float elapsed = 0.0f;
        float duration = 0.2f;
        float magnitude = 10f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            target.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
        if (artImage != null) artImage.color = Color.white;
    }

    public IEnumerator AttackAnimation()
    {
        Transform target = (visualRoot != null) ? visualRoot : transform;
        Vector3 originalPos = Vector3.zero;
        Vector3 attackPos = originalPos + new Vector3(0, -50f, 0);

        float speed = 10f;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            target.localPosition = Vector3.Lerp(originalPos, attackPos, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * speed;
            target.localPosition = Vector3.Lerp(attackPos, originalPos, t);
            yield return null;
        }

        target.localPosition = originalPos;
    }

    // --- YENİ: DISSOLVE SHADER İLE ERİYEREK ÖLME MANTIĞI ---
    public IEnumerator DieAnimationAndDestroy()
    {
        // Yazıları gizle
        if (healthText != null) healthText.gameObject.SetActive(false);
        if (nameText != null) nameText.gameObject.SetActive(false);

        Transform target = (visualRoot != null) ? visualRoot : transform;

        // Düşmanın içindeki tüm UI görsellerini (Image) bul
        Image[] allImages = target.GetComponentsInChildren<Image>(true);
        List<Material> dissolveMats = new List<Material>();

        // Her görsel için materyalin KOPYASINI oluştur (Tüm düşmanlar aynı anda erimesin diye)
        foreach (Image img in allImages)
        {
            Material matInstance = new Material(img.material);
            img.material = matInstance;
            dissolveMats.Add(matInstance);
        }

        float t = 0;
        float duration = 1.0f; // Erime süresi (1 saniye)
        Vector3 startPos = target.localPosition;
        if (AudioManager.instance != null) AudioManager.instance.PlayEnemyDissolve();

        while (t < 1)
        {
            t += Time.deltaTime / duration;

            // Yanarken hafif titreme efekti (Kül oluyormuş hissi verir)
            target.localPosition = startPos + new Vector3(Mathf.Sin(t * 50f) * 5f, 0, 0);

            // Tüm materyallerdeki _DissolveAmount parametresini artır
            foreach (Material mat in dissolveMats)
            {
                if (mat != null)
                {
                    mat.SetFloat("_DissolveAmount", t);
                }
            }

            yield return null;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}