using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum PathType
{
    Combat, Elite, Diviner, Shop, Boss, Companion, Mystery
}

public class MapNode : MonoBehaviour
{
    [Header("Node Settings")]
    public PathType pathType;
    public PathType realPathType;

    public bool isPreviewVisible = false;

    public Button nodeButton;
    public Image nodeIcon;

    public int gridLayer;

    [Header("Connections")]
    public List<MapNode> nextNodes = new List<MapNode>();
    public List<MapNode> parentNodes = new List<MapNode>();

    // --- YENÝ: Bu düðümden çýkan noktalarýn (çizgilerin) referanslarý ---
    public Dictionary<MapNode, List<Image>> lineDots = new Dictionary<MapNode, List<Image>>();

    [Header("Status")]
    public bool isCompleted = false;
    public bool isLocked = true;
    public bool isSkipped = false;

    // --- YENÝ: Zýplama Animasyonu için ---
    private Coroutine bounceCoroutine;

    void Start()
    {
        if (nodeButton != null) nodeButton.onClick.AddListener(OnNodeClicked);
    }

    void OnEnable()
    {
        // Obje (Harita) her görünür olduðunda animasyonun çökmemesi ve devam etmesi için kendini yenile
        UpdateVisuals();
    }

    public void SetIcon(Sprite icon)
    {
        if (nodeIcon != null && icon != null) nodeIcon.sprite = icon;
    }

    public void SetupMystery(PathType hiddenType, Sprite mysterySprite)
    {
        pathType = PathType.Mystery;
        realPathType = hiddenType;
        SetIcon(mysterySprite);
    }

    public void UnlockNode()
    {
        if (isSkipped) return;
        isLocked = false;
        UpdateVisuals();
    }

    public void SetCompleted()
    {
        isCompleted = true;
        isLocked = true;

        if (nextNodes != null)
        {
            foreach (MapNode nextNode in nextNodes)
            {
                if (nextNode != null) nextNode.UnlockNode();
            }
        }
        UpdateVisuals();
    }

    public void SetSkipped()
    {
        isSkipped = true;
        isLocked = true;
        UpdateVisuals();
    }

    void OnNodeClicked()
    {
        if (PathManager.instance != null && PathManager.instance.isPeekingMap) return;
        if (isLocked || isCompleted || isSkipped) return;

        PathManager.instance.OnNodeSelected(this);
    }

    public void ChangeType(PathType newType, Sprite newIcon)
    {
        pathType = newType;
        realPathType = newType;
        SetIcon(newIcon);
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nodeButton == null || nodeIcon == null) return;

        // --- YENÝ: Animasyon Temizliði ---
        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
            bounceCoroutine = null;
            nodeButton.transform.localScale = Vector3.one; // Boyutu normale döndür
        }

        if (isCompleted)
        {
            nodeButton.interactable = false;
            nodeIcon.color = Color.gray;
        }
        else if (isSkipped)
        {
            nodeButton.interactable = false;
            nodeIcon.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        }
        else if (isPreviewVisible)
        {
            nodeButton.interactable = true;
            nodeIcon.color = Color.white;

            if (gameObject.activeInHierarchy) bounceCoroutine = StartCoroutine(BounceRoutine());
        }
        else if (isLocked)
        {
            nodeButton.interactable = false;
            nodeIcon.color = Color.black;
        }
        else // Tamamen açýk ve gidilebilir durum
        { 
            nodeButton.interactable = true;
            nodeIcon.color = Color.white;

            if (gameObject.activeInHierarchy) bounceCoroutine = StartCoroutine(BounceRoutine());
        } 


        // --- YENÝ: Nefes Alma / Zýplama Animasyonu ---
        System.Collections.IEnumerator BounceRoutine()
        {
            float speed = 2f; // Ne kadar hýzlý nefes alacaðý
            float amount = 0.05f; // Ne kadar büyüyeceði (%5)
            Vector3 baseScale = Vector3.one * 1.1f; // Ana büyüklük

            while (true)
            {
                // Sinüs dalgasý ile yumuþak büyüme küçülme
                float scale = 1f + Mathf.Sin(Time.time * Mathf.PI * speed) * amount;
                nodeButton.transform.localScale = baseScale * scale;
                yield return null;
            }
        }
    }
}