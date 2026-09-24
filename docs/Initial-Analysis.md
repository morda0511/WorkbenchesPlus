# Initial analysis (2026-09-24)

**Source was not modified for this analysis.** Documentation only.

## Product

Workbenches+ is a **client crafting-menu** mod: filter to the current station, sort/group recipes, category chips, optional multi-craft arrows, optional dismantle tab. Version in source: **1.0.3** (uncommitted vs tagged 1.0.2 on `origin/main`).

It is **not** a workbench-range, prefab, or station-stat overhaul.

## Code inventory

22 plugin `.cs` files (excluding `obj/`). One BepInEx `Plugin`. Harmony **only** on `InventoryGui`. Config: 22 `ConfigEntry` fields, **zero range entries**. No hotkeys. No Jotunn. No embedded UI assets. No ZNet/RPC/ZDO writes (dismantle uses local `Inventory` + `EffectList.Create` with `default(ZDOID)`).

## Workbench system

Stations = `Player.GetCurrentCraftingStation()`. Filter = `recipe.m_craftingStation.m_name == current.m_name`, except Forge of Potential (`m_upgrader`) where vanilla accepts all recipes and WB+ must not strip the list. No prefab whitelist. No spawn-time setup. Vanilla `Interact` / `CheckUsable` / extensions unpatched.

## Range system

**Absent in WB+.** Vanilla (decompile): `m_useDistance` for GUI interact; `m_buildRange = m_rangeBuild + extensions * m_extraRangePerLevel` for hammer `HaveBuildStationInRange`. Prefab numeric defaults: **UNKNOWN – VERIFY**.

Do not add a second range implementation later; wrap vanilla once if a future task requires range.

## Config

See `docs/Config.md`. No validation sliders. No dedicated reload API. `EnableMod` is the master switch.

## Vanilla / Harmony / Unity

See `docs/Valheim-Modding.md`. Patch order on `UpdateRecipeList` is load-bearing (Dismantle **First**, sort **Last**). Vanilla method `InUpradeTab` spelling is upstream.

## Prefabs / assets

None for stations. Thunderstore `icon.png` only. Root JSON dumps are **not** loaded.

## Initialization

1. BepInEx loads plugin  
2. `Awake`: Instance, Log, `ModConfig`, `Config.Save`, `PatchAll`  
3. No Jotunn  
4. No prefab mutation  
5. First GUI: `Show` postfix builds overlays when the player opens inventory/crafting  

Nothing must run before the first workbench **spawn**; first **GUI open** is enough.

## Multiplayer

Documented as client-side. Dismantle changes the local player inventory (vanilla replication). No dedicated-server branch. Whether the DLL is safe/useless on dedicated: **UNKNOWN – VERIFY** (InventoryGui may be missing or unused).

## Compatibility

No compat code. Conflicts likely with other `InventoryGui.UpdateRecipeList` / `DoCrafting` patches. Range mods do not share state with WB+ (different APIs).

## Git

Available. No historical range feature. 1.0.2 = Dismantle. Dirty tree includes Potential-forge filter fix.

## Dependencies / risks

See `docs/Dependencies.md` and `docs/Risks.md`. Hottest hubs: `StationFilter`, dismantle Prefix skip-vanilla, `RebuildCraftingPanel` nesting.

## Additionally found (not fixed — out of scope)

- `CraftMultiplierBar.CanAfford` uses upgrade item **current** quality with `HaveRequirements`, not always `quality + 1` (vanilla upgrade uses next quality). May mis-clamp arrows on Upgrade tab if arrows were shown.  
- `GroupModdedWeaponSets` is also passed into tool/shield detectors.  
- `RecipeCategories` building/furniture heuristics are prefab-string based; hammer pieces in the **crafting** list are uncommon.  
- Dismantle refund is custom, not vanilla recover; upgraded items may not match player expectation vs Potential-forge economics.  
- Pack target now requires `-p:Pack=true` (working tree); easy to forget when releasing.  
- Analysis dumps `_stations.json` etc. are untracked clutter.

## Docs added

`AGENTS.md`, `docs/Architecture.md`, `docs/Behavior.md`, `docs/Workbenches.md`, `docs/Config.md`, `docs/Valheim-Modding.md`, `docs/Dependencies.md`, `docs/Risks.md`, `docs/Git-History.md`, `docs/Change-Protocol.md`, this file.
