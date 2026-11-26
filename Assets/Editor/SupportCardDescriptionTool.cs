using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// SupportPrefab'a (eldeki destek kartı) açıklama metni ekler ve CardDisplay.descriptionText'e bağlar.
/// İsim metninin (nameText) kopyasıyla aynı font/renk kullanılır; görselin altına, çerçevenin içine yerleşir.
/// Menü: Path of Elements > Support Card Description
/// Tekrar çalıştırmak güvenli: açıklama zaten bağlıysa hiçbir şey yapmaz.
/// </summary>
public static class SupportCardDescriptionTool
{
    const string PrefabPath = "Assets/SupportPrefab.prefab";
    const string ObjName = "Description (TMP)";

    [MenuItem("Path of Elements/Support Card Description")]
    public static void Apply()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            CardDisplay display = root.GetComponent<CardDisplay>();
            if (display == null) { Debug.LogError("[SupportDesc] SupportPrefab'da CardDisplay yok."); return; }
            if (display.descriptionText != null) { Debug.Log("[SupportDesc] Açıklama zaten bağlı, değişiklik yok."); return; }
            if (display.nameText == null) { Debug.LogError("[SupportDesc] nameText bağlı değil, kopyalanacak stil yok."); return; }

            GameObject descObj = Object.Instantiate(display.nameText.gameObject, root.transform);
            descObj.name = ObjName;
            descObj.transform.SetAsLastSibling();

            RectTransform rt = (RectTransform)descObj.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            // Kart görseli ~y -80..+82, çerçevenin alt kenarı ~y -171 → aradaki boşluk
            rt.anchoredPosition = new Vector2(5f, -122f);
            rt.sizeDelta = new Vector2(150f, 62f);

            TextMeshProUGUI tmp = descObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "Description";
            tmp.fontSize = 13f;
            tmp.enableAutoSizing = false; // CardDisplay.FitText runtime'da ayarlar
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;

            display.descriptionText = tmp;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[SupportDesc] SupportPrefab'a açıklama metni eklendi ve bağlandı.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
