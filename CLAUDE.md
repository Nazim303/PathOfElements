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

## Token rules
- **Never read `.unity` scene files whole.** `SampleScene.unity` is ~470 KB (≈150k tokens). Search only with `grep -n` in Bash. The Read tool is closed to them in `.claude/settings.json`.
- `.prefab` files: grep first, then read only the relevant line range.
- Do not open the `Library/`, `Temp/`, `Logs/`, `obj/` or `UserSettings/` folders.
- Run the asset checks with the ready-made scripts instead of re-exploring (see below).

## Scan scripts
- `python Tools/guidscan.py`: missing .meta files, duplicate GUIDs, broken GUID references (file:line).
- `python Tools/artscan.py [--missing]`: visual assignments of cards/enemies/items. With `--missing`, only the missing ones.

Known broken references to leave alone (as of 2026-11-26): `enemyDeathEffectPrefab` and the `0dbca…` material in EnemyPref/SupportPrefab are effect references. Do not touch them without asking the user. The `327bc461…` Missing Script in SampleScene was never committed. The 7 URP references in Renderer2D refresh when the project is opened.
