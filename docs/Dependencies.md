# Dependencies

## External

```text
BepInEx 5 (BaseUnityPlugin, ConfigFile, Logging)
  → Harmony (PatchAll, AccessTools, Priority)
    → assembly_valheim (InventoryGui, Player, Recipe, CraftingStation, …)
    → UnityEngine / UI / TextMeshPro
```

No Jotunn, no Newtonsoft, no networking libs.

Build: `Directory.Build.props` → `ValheimDir`, `BepInExProfile` (Solo Gameplay).

## Internal graph

```text
Plugin.Awake
  → ModConfig
  → Harmony.PatchAll

InventoryGui.UpdateCraftingPanel
  → CategoryBar (station change / Show)
  → CraftMultiplierBar.Show
  → DismantleTab.Show
  → InventoryRefreshHook.EnsureBound

InventoryGui.UpdateRecipeList
  → [First] DismantleMode.BuildList  (if Active)
  → [Last]  RecipeSort.Apply
              → StationFilter.Apply / BelongsToStation
              → CategoryBar.SyncAvailable + Matches
              → RecipeCategories.Classify
              → Craftability.ScoreBucket / MaterialPercent
              → *SetDetector + MaterialNameBucket + MaterialProgression
              → DismantleMode.SortPaired
  → vanilla list build
  → RecipeSort.ReorderGui
              → MaterialSectionHeaders.Apply
              → Craftability.ScoreBucket (again, for header craft tiers)
  → CraftabilityIndicators.Apply
              → Craftability.ScoreBucket

InventoryGui.UpdateRecipe
  → CraftMultiplierBar.Sync* → Player.HaveRequirements(..., amount)
  → DismantleMode UI postfix (if Active)

OnCraftPressed / DoCrafting
  → DismantleMode.TryBeginProgress / TryCompleteProgress
              → StationFilter.BelongsToStation
              → ObjectDB.GetRecipe
              → Inventory remove/add

CategoryBar.SetActive
  → AccessToolsExt.RebuildCraftingPanel
      → InventoryGui.UpdateCraftingPanel  (guarded by _rebuildDepth)
```

## Hub methods (many callers)

| Hub | Callers |
|---|---|
| `StationFilter.BelongsToStation` | `StationFilter.Apply`, `DismantleMode.TryGetDismantleRecipe`, `DismantleMode.FindRecipe` |
| `Craftability.ScoreBucket` | `RecipeSort`, `MaterialSectionHeaders`, `CraftabilityIndicators` |
| `AccessToolsExt.RebuildCraftingPanel` | Category click, multiplier arrows, dismantle enter/complete |
| `DismantleMode.Active` | Most dismantle patches + Category/multiplier hide |

## Shared mutable state

See `docs/Architecture.md`. Changing `StationFilter` or `DismantleMode.Active` without checking both list prefixes will empty or desync the GUI.

## Config is live

Every `Plugin.Settings.X.Value` read is a dependency on that entry. There is no cached snapshot after Awake.
