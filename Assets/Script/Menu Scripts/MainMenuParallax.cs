using UnityEngine;

[System.Serializable]
public class ParallaxLayer
{
    public RectTransform layerRect;
    [Tooltip("Görselin fareye göre ne kadar kayacaðý. Arka plan için küçük (örn: 20), ön plan için büyük (örn: 100) verin.")]
    public float moveMultiplier;
}

public class MainMenuParallax : MonoBehaviour
{
    public ParallaxLayer[] layers;
    public float smoothSpeed = 5f; // Kaymanýn yumuþaklýk hissi

    private Vector2[] startPositions;

    void Start()
    {
        // Oyun baþladýðýnda her katmanýn orijinal merkez pozisyonunu hafýzaya al
        startPositions = new Vector2[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerRect != null)
            {
                startPositions[i] = layers[i].layerRect.anchoredPosition;
            }
        }
    }

    void Update()
    {
        // Fare pozisyonunu ekranýn merkezine göre -0.5 ile 0.5 arasýna normalize et
        float mouseX = (Input.mousePosition.x / Screen.width) - 0.5f;
        float mouseY = (Input.mousePosition.y / Screen.height) - 0.5f;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerRect == null) continue;

            // Farenin tersine doðru kaydýrmak için eksiyi (-) kullanýyoruz. 
            // (Fare saða giderse görsel hafifçe sola kayar ki 3D hissi versin)
            float targetX = startPositions[i].x - (mouseX * layers[i].moveMultiplier);
            float targetY = startPositions[i].y - (mouseY * layers[i].moveMultiplier);

            Vector2 targetPos = new Vector2(targetX, targetY);

            // Hedef pozisyona yumuþak (Lerp) bir geçiþ yap
            layers[i].layerRect.anchoredPosition = Vector2.Lerp(layers[i].layerRect.anchoredPosition, targetPos, Time.deltaTime * smoothSpeed);
        }
    }
}