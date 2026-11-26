using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Varsayılan Unity UI sprite'lı (UISprite / Background / InputFieldBackground / Knob) panelleri,
/// slider'ları, scrollbar'ları ve butonları oyunun temasına çevirir (koyu taş + altın, UI_STIL_REHBERI.md).
/// Sprite'lar: Assets/Sprites/UI Theme (Tools/ui_theme_sprites.py üretir).
/// Menü: Path of Elements > UI Theme
///   1 - Preview (Dry Run) : hangi Image'ın neye dönüşeceğini Console'a yazar, hiçbir şey kaydetmez
///   2 - Apply             : prefab'ları ve Build Settings'teki sahneleri günceller ve kaydeder
/// Dokunulmayanlar: görünmez (alpha≈0) veya kapalı Image'lar, Mask grafikleri, oyun script'lerinin
/// referans tuttuğu Image'lar (sprite'ı runtime'da atanıyor demektir, ör. kart/deste görselleri).
/// </summary>
public static class UIThemeTool
{
    const string Dir = "Assets/Sprites/UI Theme/";

    // Sprite adı -> 9-slice border (sol, alt, sağ, üst). Vector4.zero = dilimlenmez.
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "UI_PanelFrame", new Vector4(64, 64, 64, 64) },
        { "UI_PanelPlain", new Vector4(20, 20, 20, 20) },
        { "UI_Backdrop", Vector4.zero },
        { "UI_Button", new Vector4(36, 30, 36, 30) },
        { "UI_Button_Hover", new Vector4(36, 30, 36, 30) },
        { "UI_Button_Pressed", new Vector4(36, 30, 36, 30) },
        { "UI_Button_Disabled", new Vector4(36, 30, 36, 30) },
        { "UI_BarTrack", new Vector4(18, 18, 18, 18) },
        { "UI_BarFill", new Vector4(18, 18, 18, 18) },
        { "UI_ScrollHandle", new Vector4(18, 18, 18, 18) },
        { "UI_Knob", Vector4.zero },
        { "UI_Divider", Vector4.zero },
    };

    static readonly Color Gold = new Color32(0xD4, 0xAF, 0x37, 0xFF);
    static readonly Color Ivory = new Color32(0xED, 0xE6, 0xD6, 0xFF);

    const float FramePanelMinSize = 260f; // daha küçük paneller süssüz (Plain) olur
    const float PanelMinAlpha = 0.92f;

    enum Kind { Backdrop, Frame, Plain, Button, BarTrack, BarFill, Knob, ScrollTrack, ScrollHandle }

    static Dictionary<string, Sprite> sprites;

    // ---------------------------------------------------------------- menu
    [MenuItem("Path of Elements/UI Theme/1 - Preview (Dry Run)")]
    public static void DryRun() => Run(true);

    [MenuItem("Path of Elements/UI Theme/2 - Apply")]
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog("UI Tema",
                "Varsayılan Unity sprite'lı paneller, slider'lar, scrollbar'lar ve butonlar tüm prefab'larda ve " +
                "Build Settings'teki sahnelerde tema sprite'larına çevrilip kaydedilecek. Devam?",
                "Uygula", "İptal")) return;
        Run(false);
    }

    // ---------------------------------------------------------------- import ayarları
    static bool LoadSprites()
    {
        sprites = new Dictionary<string, Sprite>();
        foreach (var kv in Borders)
        {
            string path = Dir + kv.Key + ".png";
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError($"[UITheme] Sprite yok: {path}. Önce 'python Tools/ui_theme_sprites.py' çalıştırın.");
                return false;
            }
            bool changed = false;
            if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
            if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (ti.spriteBorder != kv.Value) { ti.spriteBorder = kv.Value; changed = true; }
            if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed)
            { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (changed) ti.SaveAndReimport();

            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) { Debug.LogError($"[UITheme] Sprite yüklenemedi: {path}"); return false; }
            sprites[kv.Key] = sp;
        }
        return true;
    }

    // ---------------------------------------------------------------- çalıştır
    static void Run(bool dryRun)
    {
        if (!LoadSprites()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string startScene = SceneManager.GetActiveScene().path;
        var log = new StringBuilder();
        int total = 0;

        // Önce prefab'lar: sahnelerdeki instance'lar yeni görünümü miras alsın.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Plugins") || path.Contains("TextMesh Pro")) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int n = Theme(new[] { root }, dryRun, path, log);
            if (n > 0 && !dryRun) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            total += n;
        }

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled) continue;
            Scene scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            int n = Theme(scene.GetRootGameObjects(), dryRun, s.path, log);
            if (n > 0 && !dryRun)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            total += n;
        }

        if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene);
        Debug.Log(log.ToString());
        Debug.Log(dryRun
            ? $"[UITheme] ÖNİZLEME: {total} Image değişecek. Hiçbir şey kaydedilmedi (sadece sprite import ayarları yapıldı)."
            : $"[UITheme] TAMAM: {total} Image temalandı ve kaydedildi.");
    }

    static int Theme(GameObject[] roots, bool dryRun, string where, StringBuilder log)
    {
        // Oyun script'lerinin (UnityEngine/TMPro dışı) tuttuğu Image referansları: runtime'da sprite alıyorlar, dokunma.
        var scriptOwned = new HashSet<Object>();
        foreach (var mb in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
        {
            if (mb == null) continue; // Missing Script
            string ns = mb.GetType().Namespace ?? "";
            if (ns.StartsWith("UnityEngine") || ns.StartsWith("TMPro") || mb is UIButtonFx) continue;
            var it = new SerializedObject(mb).GetIterator();
            while (it.Next(true))
                if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is Image)
                    scriptOwned.Add(it.objectReferenceValue);
        }

        var images = roots.SelectMany(r => r.GetComponentsInChildren<Image>(true))
                          .OrderBy(i => Depth(i.transform)).ToList();
        var panels = new Dictionary<Transform, Kind>(); // temalanan panel/backdrop'lar
        int n = 0;

        foreach (Image img in images)
        {
            if (!IsBuiltin(img.sprite) || !img.enabled || img.color.a < 0.02f) continue;
            if (img.GetComponent<Mask>() != null) continue;
            if (scriptOwned.Contains(img)) continue;

            Kind kind = Classify(img, panels);
            log.AppendLine($"[UITheme] {where} :: {PathOf(img.transform)}  ({img.sprite.name}) -> {kind}");
            n++;
            if (kind == Kind.Backdrop || kind == Kind.Frame || kind == Kind.Plain) panels[img.transform] = kind;
            if (!dryRun) ApplyKind(img, kind);
        }

        // Koyu panele dönen yerlerde koyu metinleri fildişine çevir (yoksa okunmaz).
        if (panels.Count > 0)
        {
            foreach (var g in roots.SelectMany(r => r.GetComponentsInChildren<Graphic>(true)))
            {
                if (!(g is TMP_Text) && !(g is Text)) continue;
                if (Luminance(g.color) >= 0.4f) continue;
                Image bg = NearestVisibleImage(g.transform.parent);
                if (bg == null || !panels.ContainsKey(bg.transform)) continue;
                log.AppendLine($"[UITheme] {where} :: {PathOf(g.transform)}  metin rengi -> fildişi");
                if (!dryRun)
                {
                    Undo.RecordObject(g, "UI Theme");
                    g.color = new Color(Ivory.r, Ivory.g, Ivory.b, g.color.a);
                    EditorUtility.SetDirty(g);
                }
            }
        }
        return n;
    }

    // ---------------------------------------------------------------- sınıflandırma
    static Kind Classify(Image img, Dictionary<Transform, Kind> panels)
    {
        RectTransform rt = img.rectTransform;

        Slider slider = img.GetComponentInParent<Slider>(true);
        if (slider != null)
        {
            if (slider.fillRect == rt) return Kind.BarFill;
            if (slider.handleRect == rt) return Kind.Knob;
            return Kind.BarTrack;
        }

        if (img.GetComponent<Scrollbar>() != null) return Kind.ScrollTrack;
        Scrollbar sb = img.GetComponentInParent<Scrollbar>(true);
        if (sb != null && sb.handleRect == rt) return Kind.ScrollHandle;

        if (img.GetComponent<Selectable>() != null) return Kind.Button;

        // Ebeveynini tamamen dolduran Image: en üstteyse ekran arka planı (Backdrop),
        // bir panelin içindeyse o panelin yerine geçen ekran (ör. Journal) -> süslü çerçeve.
        bool fillsParent = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one
                           && Mathf.Abs(rt.sizeDelta.x) < 80f && Mathf.Abs(rt.sizeDelta.y) < 80f;
        if (fillsParent)
        {
            Image above = rt.parent != null ? NearestVisibleImage(rt.parent) : null;
            if (above == null) return Kind.Backdrop;
            if (panels.ContainsKey(above.transform)) return Kind.Frame;
        }

        // Süslü çerçeve içinde ya da küçükse sade panel
        for (Transform p = rt.parent; p != null; p = p.parent)
            if (panels.TryGetValue(p, out Kind k) && k != Kind.Backdrop) return Kind.Plain;
        Vector2 size = rt.rect.size;
        if (size.x > 1f && size.y > 1f && Mathf.Min(size.x, size.y) < FramePanelMinSize) return Kind.Plain;
        return Kind.Frame;
    }

    // ---------------------------------------------------------------- uygula
    static void ApplyKind(Image img, Kind kind)
    {
        Undo.RecordObject(img, "UI Theme");
        Color c = img.color;
        switch (kind)
        {
            case Kind.Backdrop:
                Set(img, "UI_Backdrop", Image.Type.Simple, new Color(1, 1, 1, c.a));
                break;
            case Kind.Frame:
                Set(img, "UI_PanelFrame", Image.Type.Sliced, new Color(1, 1, 1, Mathf.Max(c.a, PanelMinAlpha)));
                break;
            case Kind.Plain:
                Set(img, "UI_PanelPlain", Image.Type.Sliced, new Color(1, 1, 1, Mathf.Max(c.a, PanelMinAlpha)));
                break;
            case Kind.BarTrack:
            case Kind.ScrollTrack:
                Set(img, "UI_BarTrack", Image.Type.Sliced, new Color(1, 1, 1, c.a));
                FitBorders(img, 40f);
                break;
            case Kind.ScrollHandle:
                Set(img, "UI_ScrollHandle", Image.Type.Sliced, new Color(1, 1, 1, c.a));
                FitBorders(img, 40f);
                break;
            case Kind.BarFill:
                // Beyaz/griyse altın; özel bir renk verilmişse (enerji, can vb.) o renk korunur.
                bool plain = Mathf.Abs(c.r - c.g) < 0.05f && Mathf.Abs(c.g - c.b) < 0.05f;
                Set(img, "UI_BarFill", Image.Type.Sliced, plain ? new Color(Gold.r, Gold.g, Gold.b, c.a) : c);
                FitBorders(img, 40f);
                break;
            case Kind.Knob:
                Set(img, "UI_Knob", Image.Type.Simple, new Color(1, 1, 1, c.a));
                img.preserveAspect = true;
                break;
            case Kind.Button:
                Set(img, "UI_Button", Image.Type.Sliced, new Color(1, 1, 1, c.a));
                var sel = img.GetComponent<Selectable>();
                Undo.RecordObject(sel, "UI Theme");
                sel.transition = Selectable.Transition.SpriteSwap;
                sel.spriteState = new SpriteState
                {
                    highlightedSprite = sprites["UI_Button_Hover"],
                    selectedSprite = sprites["UI_Button_Hover"],
                    pressedSprite = sprites["UI_Button_Pressed"],
                    disabledSprite = sprites["UI_Button_Disabled"],
                };
                EditorUtility.SetDirty(sel);
                if (img.GetComponent<UIButtonFx>() == null) Undo.AddComponent<UIButtonFx>(img.gameObject);
                foreach (var t in img.GetComponentsInChildren<TMP_Text>(true))
                    if (Luminance(t.color) < 0.4f) { Undo.RecordObject(t, "UI Theme"); t.color = Ivory; EditorUtility.SetDirty(t); }
                break;
        }
        EditorUtility.SetDirty(img);
    }

    static void Set(Image img, string sprite, Image.Type type, Color color)
    {
        img.sprite = sprites[sprite];
        img.type = type;
        img.fillCenter = true;
        img.color = color;
        img.pixelsPerUnitMultiplier = 1f;
    }

    // İnce bar'larda (ör. 20 px yüksek slider) border'lar sığsın diye sprite'ı küçült.
    static void FitBorders(Image img, float spriteSize)
    {
        Vector2 s = img.rectTransform.rect.size;
        float minDim = Mathf.Min(s.x, s.y);
        if (minDim > 1f && minDim < spriteSize) img.pixelsPerUnitMultiplier = spriteSize / minDim;
    }

    // ---------------------------------------------------------------- yardımcılar
    static bool IsBuiltin(Sprite s) =>
        s != null && AssetDatabase.GetAssetPath(s) == "Resources/unity_builtin_extra";

    static Image NearestVisibleImage(Transform t)
    {
        for (; t != null; t = t.parent)
        {
            var img = t.GetComponent<Image>();
            if (img != null && img.enabled && img.color.a > 0.05f && img.GetComponent<Mask>() == null) return img;
        }
        return null;
    }

    static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    static int Depth(Transform t)
    {
        int d = 0;
        for (; t.parent != null; t = t.parent) d++;
        return d;
    }

    static string PathOf(Transform t)
    {
        var parts = new List<string>();
        for (; t != null; t = t.parent) parts.Add(t.name);
        parts.Reverse();
        return string.Join("/", parts);
    }
}
