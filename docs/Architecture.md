# Architecture

## What this mod is

Workbenches+ (GUID `com.morda.workbenchesplus`, current source version **1.0.3**) is a **BepInEx + Harmony client plugin**. It changes how the **crafting GUI** lists, filters, sorts, and (optionally) dismantles items.

It does **not** replace vanilla recipes, station prefabs, or `CraftingStation` range/level/roof/fire logic.

README claim: “Does not change recipes or crafting logic.” Dismantle is an extra inventory action that **does** remove an item and add refund stacks; normal craft/upgrade still goes through vanilla `InventoryGui.DoCrafting` unless dismantle mode is active.

## Layers

```text
Plugin.Awake
  → ModConfig (BepInEx ConfigFile)
  → Harmony.PatchAll (this assembly)

Runtime (InventoryGui open)
  → vanilla GetAvailableRecipes / UpdateRecipeList / UpdateRecipe
  → Harmony prefixes/postfixes
  → StationFilter, RecipeSort, CategoryBar, Craftability, DismantleMode
  → extra UI GameObjects parented under vanilla rects
```

## Project layout

| Path | Role |
|---|---|
| `Plugin.cs` | BepInEx entry, Harmony lifetime |
| `Configuration/ModConfig.cs` | All config entries |
| `Patches/InventoryGuiPatches.cs` | Sort, category reset, inventory refresh, multiplier sync, Show/Hide |
| `Patches/DismantlePatches.cs` | Dismantle Harmony (tabs, list, craft button, DoCrafting) |
| `Crafting/*` | Filter, sort, categories, set detectors, dismantle refund |
| `UI/*` | Category chips, headers, multiplier, dismantle tab, checkmarks |
| `AccessToolsExt` | Lives at bottom of `UI/CategoryBar.cs` - vanilla field/method accessors |
| `thunderstore/WorkbenchesPlus/` | Pack root (README, CHANGELOG, icon, DLL copy) |
| `_stations.json`, `_workbench_recipes.json`, `_workbench_label_map.txt` | **Untracked dumps. Not loaded by the plugin.** |

## No Jotunn

The `.csproj` does not reference Jotunn. There is no custom piece/prefab registration.

## Harmony as the only Valheim hook

All Valheim integration is `HarmonyPatch` on `InventoryGui` (plus `AccessTools` field/method access). No `CraftingStation`, `Player`, `Piece`, or `StationExtension` patches exist.

## Shared state (process-wide)

| State | Owner | Notes |
|---|---|---|
| `Plugin.Settings` | Plugin | Config |
| `CategoryBar.Active` | CategoryBar | Current chip |
| `DismantleMode.Active` / `Items` / progress fields | DismantleMode | Must stay aligned with recipe rows |
| `InventoryRefreshHook` bind/pending | InventoryGuiPatches | Inventory `m_onChanged` |
| `CraftMultiplierBar` amount | CraftMultiplierBar | Written into vanilla multi-craft fields |
| `AccessToolsExt._rebuildDepth` | AccessToolsExt | Nested rebuild guard |
| `StationHover`-style chest counts | **N/A** | StoreAndCraft, not this mod |

## Data flow (recipe list)

```text
Player opens station
  InventoryGui.Show → SetupCrafting → UpdateCraftingPanel
    vanilla: GetAvailableRecipes → UpdateRecipeList(recipes)

  DismantleRecipeListPatch Prefix [Priority.First]
    if dismantle: replace `recipes` with inventory-backed list

  UpdateRecipeListSortPatch Prefix [Priority.Last]
    StationFilter.Apply (skipped entirely if current.m_upgrader)
    CategoryBar.SyncAvailable + optional category strip
    RecipeSort.SortInPlace (or DismantleMode.SortPaired)

  vanilla UpdateRecipeList
    craft tab vs upgrade tab (InCraftTab / inventory items)

  UpdateRecipeListSortPatch Postfix
    RecipeSort.ReorderGui (re-sort m_availableRecipes + Y positions)
    CraftabilityIndicators.Apply
```

Vanilla **also** sorts `m_availableRecipes` at the end of `UpdateRecipeList`. `ReorderGui` exists because that vanilla sort would otherwise undo `SortInPlace`.

## UI overlay

Extra objects are instantiated at runtime from vanilla buttons (Craft, Repair, Upgrade, quality arrows). They are destroyed or hidden on `InventoryGui.Hide`. No Addressables, no embedded prefab assets in the DLL (no `.png` EmbeddedResource in the csproj).
