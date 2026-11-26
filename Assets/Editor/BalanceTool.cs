using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dengeleme planının (docs/DENGELEME_PLANI.md) SAHNEDE kayıtlı değerlerini uygular.
/// Kart/düşman/eşya sayıları asset'lerde; bu araç yalnızca SampleScene'deki manager'ların
/// Inspector değerlerini değiştirir (kodda varsayılanı değiştirmek, sahnede kayıtlı değeri ezmez).
/// Ayrıca yeni kartları başlangıç destesi havuzuna (DeckManager.allCardsInDeck, SampleScene)
/// ve Günlüğe (JournalManager.allCards, Menu Scene) ekler.
/// Menü: Path of Elements > Balance
///   1 - Preview (Dry Run) : şu anki ve yeni değerleri Console'a yazar, hiçbir şey değiştirmez
///   2 - Apply             : SampleScene'i açar, değerleri yazar ve sahneyi kaydeder
/// </summary>
public static class BalanceTool
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string MenuScenePath = "Assets/Scenes/Menu Scene.unity";
    const string CardDir = "Assets/CardData/";

    // Havuza ve Günlüğe eklenecek kartlar (yoksa sona eklenir, var olanlara dokunulmaz)
    static readonly string[] NewCardFiles = { "Water Supportive", "Earth Supportive", "Air Supportive" };
    const string EnemyDir = "Assets/EnemyData/";

    // Haritanın ilk yarısında çıkan zayıf düşmanlar (dosya adları; oyundaki isimler yorumda)
    static readonly string[] EarlyEnemyFiles =
    {
        "Wolf",        // Bloom Sprout
        "Magma Worm",  // Magma Worm
        "Angry Bat",   // Watcher
        "Green Slime", // Drowned
        "White Wolf",  // Marauders
    };

    // Normal savaş havuzunda olması gereken tüm düşmanlar (TrainingDummy ve bosslar hariç)
    static readonly string[] AllEnemyFiles =
    {
        "Wolf", "Magma Worm", "Angry Bat", "Green Slime", "White Wolf",
        "Coal Imp", "Toxic Mushroom", "Harpy", "Mini Golem", "Water Spirit", "Ice Witch",
    };

    const string MinionFile = "Wolf"; // Protector'ın çağırdığı minyon: Bloom Sprout

    // component tipi -> (alan, yeni değer)
    static readonly Dictionary<System.Type, (string field, object value)[]> Values =
        new Dictionary<System.Type, (string, object)[]>
        {
            { typeof(PlayerManager), new (string, object)[] { ("maxHealth", 80) } },
            { typeof(EnergyManager), new (string, object)[] { ("maxEnergy", 6) } },
            { typeof(DeckManager), new (string, object)[] { ("maxHandSize", 8), ("drawPerTurn", 2) } },
            { typeof(CardExchangeSlot), new (string, object)[] { ("hpCost", 8) } },
            { typeof(MapGenerator), new (string, object)[] {
                ("mapLength", 10),
                ("combatChance", 45), ("eliteChance", 12), ("shopChance", 12),
                ("divinerChance", 8), ("companionChance", 10), ("mysteryChance", 13),
                ("eliteMinLayer", 3), ("shopMinLayer", 2) } },
            { typeof(TurnManager), new (string, object)[] {
                ("openingDraw", 5), ("eliteHealthMult", 1.3f), ("eliteDamageMult", 1.2f),
                ("eliteGoldMult", 2.0f), ("eliteEnemyCount", 2) } },
            { typeof(ShopManager), new (string, object)[] { ("rerollCost", 10) } },
        };

    [MenuItem("Path of Elements/Balance/1 - Preview (Dry Run)")]
    public static void DryRun() => Run(true);

    [MenuItem("Path of Elements/Balance/2 - Apply")]
    public static void Apply()
    {
        if (!EditorUtility.DisplayDialog("Dengeleme",
                "SampleScene'deki manager değerleri (can, enerji, el, harita oranları, düşman havuzları) " +
                "DENGELEME_PLANI.md'deki değerlere çekilecek ve sahne kaydedilecek. Devam?",
                "Uygula", "Vazgeç")) return;
        Run(false);
    }

    static void Run(bool dryRun)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var log = new StringBuilder(dryRun ? "[Balance] ÖNİZLEME (değişiklik yok)\n" : "[Balance] UYGULANDI\n");
        int changes = 0;

        foreach (var kv in Values)
        {
            var comps = Object.FindObjectsByType(kv.Key, FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (comps.Length == 0) { log.AppendLine($"  ! {kv.Key.Name} sahnede yok"); continue; }
            foreach (var c in comps)
            {
                var so = new SerializedObject(c);
                foreach (var (field, value) in kv.Value)
                {
                    var p = so.FindProperty(field);
                    if (p == null) { log.AppendLine($"  ! {kv.Key.Name}.{field} bulunamadı (script derlendi mi?)"); continue; }
                    string oldV = PropString(p);
                    string newV = System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                    if (oldV == newV) continue;
                    log.AppendLine($"  {c.name}/{kv.Key.Name}.{field}: {oldV} -> {newV}");
                    changes++;
                    if (value is int i) p.intValue = i;
                    else if (value is float f) p.floatValue = f;
                }
                if (!dryRun) so.ApplyModifiedProperties();
            }
        }

        // Düşman havuzları
        foreach (TurnManager tm in Object.FindObjectsByType<TurnManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(tm);
            changes += SyncArray(so.FindProperty("allEnemies"), AllEnemyFiles, false, "allEnemies", log);
            changes += SyncArray(so.FindProperty("earlyEnemies"), EarlyEnemyFiles, true, "earlyEnemies", log);
            if (!dryRun) so.ApplyModifiedProperties();
        }

        // Protector minyonu
        var minion = LoadEnemy(MinionFile);
        foreach (CombatManager cm in Object.FindObjectsByType<CombatManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(cm);
            var p = so.FindProperty("defaultMinion");
            if (p != null && minion != null && p.objectReferenceValue != minion)
            {
                log.AppendLine($"  CombatManager.defaultMinion: {(p.objectReferenceValue ? p.objectReferenceValue.name : "boş")} -> {minion.name}");
                p.objectReferenceValue = minion;
                changes++;
                if (!dryRun) so.ApplyModifiedProperties();
            }
        }

        // Başlangıç destesi havuzu
        foreach (DeckManager dm in Object.FindObjectsByType<DeckManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(dm);
            changes += AddCards(so.FindProperty("allCardsInDeck"), "DeckManager.allCardsInDeck", log);
            if (!dryRun) so.ApplyModifiedProperties();
        }

        if (changes == 0) log.AppendLine("  SampleScene zaten güncel.");

        if (!dryRun && changes > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Günlük (Menu Scene)
        Scene menu = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        int menuChanges = 0;
        var journals = Object.FindObjectsByType<JournalManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (journals.Length == 0) log.AppendLine("  ! JournalManager Menu Scene'de yok");
        foreach (JournalManager jm in journals)
        {
            var so = new SerializedObject(jm);
            menuChanges += AddCards(so.FindProperty("allCards"), "JournalManager.allCards (Menu Scene)", log);
            if (!dryRun) so.ApplyModifiedProperties();
        }
        if (!dryRun && menuChanges > 0)
        {
            EditorSceneManager.MarkSceneDirty(menu);
            EditorSceneManager.SaveScene(menu);
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log(log.ToString());
    }

    static int AddCards(SerializedProperty arr, string label, StringBuilder log)
    {
        if (arr == null) { log.AppendLine($"  ! {label} bulunamadı"); return 0; }
        var current = new List<Object>();
        for (int i = 0; i < arr.arraySize; i++) current.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);
        int added = 0;
        foreach (string f in NewCardFiles)
        {
            var card = AssetDatabase.LoadAssetAtPath<CardData>(CardDir + f + ".asset");
            if (card == null) { log.AppendLine($"  ! Kart bulunamadı: {CardDir}{f}.asset"); continue; }
            if (current.Contains(card)) continue;
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = card;
            current.Add(card);
            log.AppendLine($"  {label} += {card.cardName}");
            added++;
        }
        return added > 0 ? 1 : 0;
    }

    // exact=true: dizi tam olarak bu listeye eşitlenir. false: eksikler eklenir, fazlalar bırakılır.
    static int SyncArray(SerializedProperty arr, string[] files, bool exact, string label, StringBuilder log)
    {
        if (arr == null) { log.AppendLine($"  ! TurnManager.{label} bulunamadı"); return 0; }
        var current = new List<Object>();
        for (int i = 0; i < arr.arraySize; i++) current.Add(arr.GetArrayElementAtIndex(i).objectReferenceValue);

        var wanted = files.Select(LoadEnemy).Where(e => e != null).Cast<Object>().ToList();
        List<Object> result = exact ? wanted : current.Where(o => o != null).Concat(wanted.Where(w => !current.Contains(w))).ToList();

        if (result.SequenceEqual(current)) return 0;
        log.AppendLine($"  TurnManager.{label}: [{string.Join(", ", current.Select(o => o ? o.name : "boş"))}] -> " +
                       $"[{string.Join(", ", result.Select(o => o.name))}]");
        arr.arraySize = result.Count;
        for (int i = 0; i < result.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = result[i];
        return 1;
    }

    static EnemyData LoadEnemy(string file)
    {
        var e = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDir + file + ".asset");
        if (e == null) Debug.LogWarning($"[Balance] Düşman asset'i bulunamadı: {EnemyDir}{file}.asset");
        return e;
    }

    static string PropString(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.intValue.ToString();
            case SerializedPropertyType.Float: return p.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            default: return p.propertyType.ToString();
        }
    }
}
