using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Font change: Comfortaa / LiberationSans / Quintessential -> Cinzel (titles) + Alegreya (body text).
/// Menu: Path of Elements > Fonts
///   1 - Create TMP Font Assets    : creates SDF assets from the .ttf files (once)
///   2 - Preview (Dry Run)         : only counts and lists in the Console which texts will change
///   3 - Apply                     : updates prefabs and the scenes in Build Settings, then saves them
/// Rule (UI_STIL_REHBERI.md): if the font size is >= TitleMinSize, the old font is Quintessential,
/// or the object name contains a title hint -> Cinzel; everything else -> Alegreya.
/// </summary>
public static class FontSwapTool
{
    const string CinzelTtf = "Assets/Font/Cinzel/Cinzel-Variable.ttf";
    const string AlegreyaTtf = "Assets/Font/Alegreya/Alegreya-Variable.ttf";
    const string CinzelSdf = "Assets/Font/Cinzel/Cinzel SDF.asset";
    const string AlegreyaSdf = "Assets/Font/Alegreya/Alegreya SDF.asset";

    const float TitleMinSize = 40f;
    static readonly string[] TitleNameHints = { "title", "header", "name", "baslik" };

    const string Preload =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "çğıöşüÇĞİÖŞÜâÂîÎûÛ’‘“”–—…•·×";

    // ---------------------------------------------------------------- 1
    [MenuItem("Path of Elements/Fonts/1 - Create TMP Font Assets")]
    public static void CreateFontAssets()
    {
        Create(CinzelTtf, CinzelSdf);
        Create(AlegreyaTtf, AlegreyaSdf);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void Create(string ttfPath, string outPath)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath) != null)
        {
            Debug.Log($"[FontSwap] Already exists, skipped: {outPath}");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null) { Debug.LogError($"[FontSwap] Font not found: {ttfPath}"); return; }

        // Single 2048 atlas (no multi-atlas: extra atlases would not be saved as sub-assets).
        var fa = TMP_FontAsset.CreateFontAsset(font, 72, 7, GlyphRenderMode.SDFAA, 2048, 2048,
                                               AtlasPopulationMode.Dynamic, false);
        fa.name = Path.GetFileNameWithoutExtension(outPath);
        AssetDatabase.CreateAsset(fa, outPath);

        fa.atlasTextures[0].name = fa.name + " Atlas";
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        AssetDatabase.AddObjectToAsset(fa.material, fa);

        // Store the source font reference so dynamic glyphs also work in the build
        var so = new SerializedObject(fa);
        var p1 = so.FindProperty("m_SourceFontFile_EditorRef");
        var p2 = so.FindProperty("m_SourceFontFile");
        if (p1 != null) p1.objectReferenceValue = font;
        if (p2 != null) p2.objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();

        fa.TryAddCharacters(Preload, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[FontSwap] {fa.name} missing characters: {missing}");

        EditorUtility.SetDirty(fa);
        Debug.Log($"[FontSwap] Created: {outPath}");
    }

    // ---------------------------------------------------------------- 2 / 3
    [MenuItem("Path of Elements/Fonts/2 - Preview (Dry Run)")]
    public static void DryRun() => Run(true);

    [MenuItem("Path of Elements/Fonts/3 - Apply")]
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Change fonts",
                "Fonts in all prefabs and in the scenes in Build Settings will be changed to Cinzel/Alegreya and the files saved. Continue?",
                "Change", "Cancel")) return;
        Run(false);
    }

    static void Run(bool dryRun)
    {
        var title = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelSdf);
        var body = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AlegreyaSdf);
        if (title == null || body == null)
        {
            Debug.LogError("[FontSwap] Run step '1 - Create TMP Font Assets' first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string startScene = SceneManager.GetActiveScene().path;
        int total = 0;

        // Prefabs first, so prefab instances in scenes inherit the new font.
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Assets/Plugins") || path.Contains("TextMesh Pro")) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int n = Swap(root.GetComponentsInChildren<TMP_Text>(true), title, body, dryRun, path);
            if (n > 0 && !dryRun) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            total += n;
        }

        foreach (var s in EditorBuildSettings.scenes)
        {
            if (!s.enabled) continue;
            Scene scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);
            var texts = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            int n = Swap(texts, title, body, dryRun, s.path);
            if (n > 0 && !dryRun)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            total += n;
        }

        if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene);
        Debug.Log(dryRun
            ? $"[FontSwap] PREVIEW: {total} texts would change. Nothing was saved."
            : $"[FontSwap] DONE: {total} texts changed and saved.");
    }

    static int Swap(TMP_Text[] texts, TMP_FontAsset title, TMP_FontAsset body, bool dryRun, string where)
    {
        int n = 0, toTitle = 0;
        foreach (TMP_Text t in texts)
        {
            if (t.font == title || t.font == body) continue;

            string oldName = t.font != null ? t.font.name : "(none)";
            string goName = t.gameObject.name.ToLowerInvariant();
            bool isTitle = t.fontSize >= TitleMinSize
                           || oldName.Contains("Quintessential")
                           || TitleNameHints.Any(h => goName.Contains(h));
            TMP_FontAsset target = isTitle ? title : body;

            if (!dryRun)
            {
                Undo.RecordObject(t, "Font Swap");
                t.font = target;
                t.fontSharedMaterial = target.material;
                if (PrefabUtility.IsPartOfPrefabInstance(t)) PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                EditorUtility.SetDirty(t);
            }
            n++;
            if (isTitle) toTitle++;
        }
        if (n > 0) Debug.Log($"[FontSwap] {where}: {n} texts ({toTitle} Cinzel, {n - toTitle} Alegreya)");
        return n;
    }
}
