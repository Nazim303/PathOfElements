using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum PathType
{
    Combat, Elite, Diviner, Shop, Boss, Companion, Mystery // --- YENÝ TÜR ---
}

public class MapNode : MonoBehaviour
{
    [Header("Node Settings")]
    public PathType pathType;       // Görünen Tür (Mystery olabilir)
    public PathType realPathType;   // --- YENÝ: Gerçek Tür (Týklanýnca çalýþacak olan) ---

    public Button nodeButton;
    public Image nodeIcon;

    // Haritadaki konumu
    public int gridLayer;

    [Header("Connections")]
    public List<MapNode> nextNodes = new List<MapNode>();
    public List<MapNode> parentNodes = new List<MapNode>();

    [Header("Status")]
    public bool isCompleted = false;
    public bool isLocked = true;
    public bool isSkipped = false;

    void Start()
    {
        if (nodeButton != null) nodeButton.onClick.AddListener(OnNodeClicked);
        // Start'ta visual update gerekmez, generator yapýyor.
    }

    public void SetIcon(Sprite icon)
    {
        if (nodeIcon != null && icon != null)
        {
            nodeIcon.sprite = icon;
        }
    }

    // --- YENÝ: Gizemli Düðümü Ayarlama ---
    public void SetupMystery(PathType hiddenType, Sprite mysterySprite)
    {
        pathType = PathType.Mystery;
        realPathType = hiddenType;
        SetIcon(mysterySprite);
    }
    // -------------------------------------

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
                if (nextNode != null)
                {
                    nextNode.UnlockNode();
                }
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
        if (isLocked || isCompleted || isSkipped) return;

        // PathManager'a GÖRÜNEN türü deðil, GERÇEK türü gönderiyoruz!
        // Eðer Mystery ise, realPathType çalýþacak (örn: Shop).
        // Eðer normal bir düðümse, pathType ile realPathType aynýdýr zaten.
        PathManager.instance.OnNodeSelected(this);
    }

    public void ChangeType(PathType newType, Sprite newIcon)
    {
        pathType = newType;
        realPathType = newType;
        SetIcon(newIcon);

        // Eðer kilitliyse rengini güncelle (Koyu versiyonu)
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (nodeButton == null || nodeIcon == null) return;

        // Görsel güncelleme (Renk tonlarý)
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
        else if (isLocked)
        {
            nodeButton.interactable = false;
            nodeIcon.color = Color.black;
        }
        else
        {
            nodeButton.interactable = true;
            nodeIcon.color = Color.white;
            nodeButton.transform.localScale = Vector3.one * 1.2f;
        }
    }
}