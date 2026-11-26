# Path of Elements

2D, element-based roguelike deckbuilder made in Unity (URP 2D, uGUI/TextMeshPro, DOTween). The code comments and debug logs are in Turkish.

## Project
- Unity version: `ProjectSettings/ProjectVersion.txt`. 6000.5.9f1 (upgraded from 6000.0.59f2 in commit 5684854).
- Code: `Assets/Script/` (~45 files, ~5.8k lines, single assembly with no asmdef). Menu: `Menu Scripts/`. Tutorial: `Tutorial Scripts/`.
- Data: ScriptableObject-based: `CardData`, `EnemyData`, `ItemData` (in `Assets/CardData/`, `Assets/EnemyData/`).
- Build scenes: `Menu Scene` → `NewTutorialScene` → `SampleScene` (the main game). `TutorialScene` and `VFXScene` are old/test scenes.
- Architecture: managers are singletons (`instance`). Flow: `PathManager` (map node) → `TurnManager` (round loop) → `CombatManager` (attack, combos, modifiers, ~770 lines) / `DeckManager` (draw/discard). Element multipliers live in `ElementSystem.GetDamageMultiplier`.
- **Removed (2026-11-26):** Hexodia/Ultimate combo system and terrain-changing cards (`TerrainManager`), fusion (`FusionRecipe`) and the fusion elements (Magma/Steam/Ice/Dust). Do not bring them back. Decisions: `docs/MEKANIK_DENETIMI.md`. The Hexodia VFX/SFX assets are still in the project, unused.

## Design documents (`docs/`)
- `CILALAMA_PLANI.md`: overall polish plan · `GORSEL_DURUMU.md`: art status
- `MEKANIK_DENETIMI.md`: keep/simplify/cut decisions per system (the user marks them)
- `UI_STIL_REHBERI.md`: font, color and panel rules (draft)
- `DENGELEME_PLANI.md`: balance numbers (cards, enemies, items, energy, map odds) and the simulation results behind them
- Direction: **simplify and polish**, not rebuild. Prefer removing a mechanic over adding one.

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
- Unity menu **Path of Elements > Fonts** (`Assets/Editor/FontSwapTool.cs`): font change Comfortaa → Cinzel/Alegreya. Do scene/prefab changes in the Editor with this kind of tool, not by editing YAML.
- `python Tools/ui_theme_sprites.py [--preview OUT.png]`: generates the themed UI sprites (dark stone + gold) into `Assets/Sprites/UI Theme/`. Deterministic, safe to rerun. Unity menu **Path of Elements > UI Theme** (`Assets/Editor/UIThemeTool.cs`) swaps default Unity UI sprites (panels, sliders, scrollbars, buttons) to them. Details: `docs/UI_STIL_REHBERI.md` §3.
- `python Tools/balance_sim.py [--preset mevcut|onerilen] [--deck tipik|guclu|ates] [--runs N]`: Monte Carlo run simulation. Card/enemy numbers are typed into the script by hand; update them when assets change. Unity menu **Path of Elements > Balance** (`Assets/Editor/BalanceTool.cs`) writes the scene-serialized balance values (HP, energy, hand, map odds, enemy pools) into SampleScene.
- `python Tools/colorize_sketches.py --preview OUT.png | --apply BACKUP_DIR`: colorizes the sketches in the element color, writing in place. Already applied to 17 sketches (2026-11-26). **Do not run it again on the same files**, or the result is colorized twice. Art status: `docs/GORSEL_DURUMU.md`.

Known broken references to leave alone (as of 2026-11-26): `enemyDeathEffectPrefab` and the `0dbca…` material in EnemyPref/SupportPrefab are effect references. Do not touch them without asking the user. The `327bc461…` Missing Script in SampleScene was never committed. The 7 URP references in Renderer2D refresh when the project is opened.
