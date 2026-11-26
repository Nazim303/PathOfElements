using UnityEngine;
using System.Collections;

public class UIShake : MonoBehaviour
{
    public static UIShake instance;

    [Header("Target")]
    public RectTransform uiElementToShake; // Sallanacak Ana Panel

    [Header("Settings")]
    public bool enableScreenShake = true; // Ayarlar için toggle

    private Vector2 originalPos;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (uiElementToShake != null)
        {
            originalPos = uiElementToShake.anchoredPosition;
        }
    }

    public void TriggerShake(float duration, float magnitude)
    {
        if (!enableScreenShake || uiElementToShake == null) return;

        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // UI için X ve Y ekseninde rastgele kayma
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            uiElementToShake.anchoredPosition = originalPos + new Vector2(x, y);

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Yerine geri dön
        uiElementToShake.anchoredPosition = originalPos;
    }

    // Ayarlar menüsü için
    public void ToggleShake(bool status)
    {
        enableScreenShake = status;
    }
}