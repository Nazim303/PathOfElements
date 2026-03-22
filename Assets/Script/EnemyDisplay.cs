using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyDisplay : MonoBehaviour
{
    [Header("Data")]
    public EnemyData enemyData;
    public int currentHealth;

    [Header("UI Components")]
    public Transform visualRoot; // --- YENÝ: Her þeyin içinde olduðu obje ---
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
        if(banderimage != null)
        {
            banderimage.sprite = data.enemybander;
            banderimage.preserveAspect = true;

        }
        if (artImage != null)
        {
            artImage.sprite = data.enemyArt;
            artImage.preserveAspect = true;
        }

        // Görsel kökün pozisyonunu sýfýrla
        if (visualRoot != null) visualRoot.localPosition = Vector3.zero;

        UpdateElementVisual(data.element);
        UpdateHealthUI(currentHealth);


        // Tooltip verisini hazýrla
        TooltipTrigger tooltip = GetComponent<TooltipTrigger>();
        // Eðer ana objede yoksa VisualRoot içinde ara (Çünkü raycast hedefi orasý olabilir)
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

    // --- HASAR EFEKTÝ (Visual Root Titrer) ---
    public IEnumerator TakeDamageEffect()
    {
        if (artImage != null) artImage.color = Color.red;

        // Eðer VisualRoot atanmadýysa hata vermesin diye kendisini kullanmayý deneriz
        // ama layout sorunu olmamasý için VisualRoot þart.
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

    // --- SALDIRI ANÝMASYONU (Visual Root Hareket Eder) ---
    public IEnumerator AttackAnimation()
    {
        // Layout Group ana objeyi (transform) tutar.
        // Biz içindeki VisualRoot'u hareket ettirirsek, Layout bozulmadan animasyon oynar.

        Transform target = (visualRoot != null) ? visualRoot : transform;
        Vector3 originalPos = Vector3.zero;
        Vector3 attackPos = originalPos + new Vector3(0, -50f, 0);

        float speed = 10f;
        float t = 0;

        // Ýleri
        while (t < 1)
        {
            t += Time.deltaTime * speed;
            target.localPosition = Vector3.Lerp(originalPos, attackPos, t);
            yield return null;
        }

        // Geri
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * speed;
            target.localPosition = Vector3.Lerp(attackPos, originalPos, t);
            yield return null;
        }

        target.localPosition = originalPos;
    }

    public IEnumerator DieAnimationAndDestroy()
    {
        if (healthText != null) healthText.gameObject.SetActive(false);

        // Visual Root'u küçült
        Transform target = (visualRoot != null) ? visualRoot : transform;

        float t = 0;
        Vector3 startScale = target.localScale;

        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}