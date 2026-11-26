# Path of Elements

2D, element-based roguelike deckbuilder made in Unity (URP 2D, uGUI/TextMeshPro, DOTween). The code comments and debug logs are in Turkish.

## Project
- Unity version: `ProjectSettings/ProjectVersion.txt`. The working copy has been upgraded from 6000.0.59f2 to 6000.5.9f1, but this is not committed.
- Code: `Assets/Script/` (~45 files, ~5.8k lines, single assembly with no asmdef). Menu: `Menu Scripts/`. Tutorial: `Tutorial Scripts/`.
- Data: ScriptableObject-based: `CardData`, `EnemyData`, `ItemData`, `FusionRecipe` (in `Assets/CardData/`, `Assets/EnemyData/`).
- Build scenes: `Menu Scene` → `NewTutorialScene` → `SampleScene` (the main game). `TutorialScene` and `VFXScene` are old/test scenes.
- Architecture: managers are singletons (`instance`). Flow: `PathManager` (map node) → `TurnManager` (round loop) → `CombatManager` (attack, fusion, ultimates, 956 lines) / `DeckManager` (draw/discard). Element multipliers live in `ElementSystem.GetDamageMultiplier`.

## Why it stalled
The project stalled because the artist could not finish the drawings, not because of bugs in the code. Polishing mostly means filling in missing visuals.

## Version control pitfalls
- The repo is used with both **git (GitHub)** and **Plastic/Unity VCS** (`.plastic/`). Pick one.
- Never move or rename assets in Explorer/git. Do it only inside the Unity Editor, and always commit the `.meta` file together with its asset. Otherwise the GUID changes and scene/prefab references break silently.
- There is no LFS. Large PNG/MP4 files are added directly into git (`.git` ~420 MB).
- `.gitattributes` only contains `* text=auto`. There is no UnityYAMLMerge setup.
- `Assets/Plugins/` (DOTween) and `Assets/Resources/` are not tracked yet, but the working copy code uses `DG.Tweening`.

## Checking references
Collect the GUIDs from all `.meta` files under `Assets/` and `Library/PackageCache/`, then look for `guid:` references in `.unity`/`.prefab`/`.asset`/`.mat` files that are not in that set.
