using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // YENÝ: List yapýsý için eklendi

public class EnemyDisplay : MonoBehaviour
{
    [Header("Data")]
    public EnemyData enemyData;
    public int currentHealth;

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

        UpdateElementVisual(data.element);
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
        if (healthText != null) healthText.text = healthVal.ToString();
    }

    public void UpdateElementVisual(ElementType type)
    {
        if (elementIcon == null) return;

        switch (type)
        {
            case ElementType.Fire: elementIcon.color = Color.red; break;
            case ElementType.Water: elementIcon.color = Color.blue; break;
            case ElementType.Earth: elementIcon.color = new Color(0.6f, 0.4f, 0.2f); break;
            case ElementType.Air: elementIcon.color = Color.cyan; break;
            default: elementIcon.color = Color.white; break;
        }
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

    // --- YENÝ: DISSOLVE SHADER ÝLE ERÝYEREK ÖLME MANTIÐI ---
    public IEnumerator DieAnimationAndDestroy()
    {
        // Yazýlarý gizle
        if (healthText != null) healthText.gameObject.SetActive(false);
        if (nameText != null) nameText.gameObject.SetActive(false);

        Transform target = (visualRoot != null) ? visualRoot : transform;

        // Düþmanýn içindeki tüm UI görsellerini (Image) bul
        Image[] allImages = target.GetComponentsInChildren<Image>(true);
        List<Material> dissolveMats = new List<Material>();

        // Her görsel için materyalin KOPYASINI oluþtur (Tüm düþmanlar ayný anda erimesin diye)
        foreach (Image img in allImages)
        {
            Material matInstance = new Material(img.material);
            img.material = matInstance;
            dissolveMats.Add(matInstance);
        }

        float t = 0;
        float duration = 1.0f; // Erime süresi (1 saniye)
        Vector3 startPos = target.localPosition;

        while (t < 1)
        {
            t += Time.deltaTime / duration;

            // Yanarken hafif titreme efekti (Kül oluyormuþ hissi verir)
            target.localPosition = startPos + new Vector3(Mathf.Sin(t * 50f) * 5f, 0, 0);

            // Tüm materyallerdeki _DissolveAmount parametresini artýr
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