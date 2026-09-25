# Config

File: `BepInEx/config/com.morda.workbenchesplus.cfg`  
Loaded in `Plugin.Awake` via `new ModConfig(Config)` then `Config.Save()`.

No `SettingChanged` handler, no custom reload, no server sync. Values are read from `ConfigEntry.Value` at use time (typical BepInEx behaviour: Configuration Manager / file edit updates the entry). **File-watch reload without BepInEx: UNKNOWN - VERIFY.**

No `AcceptableValueRange` / slider attributes on any bind. `MaxCraftMultiplier` is an unbound `int` (code clamps `< 1` to 1 when used).

`SortMode` is a free string; unknown values fall through compare flags (not Vanilla/Alphabetical/Progression/CategoryThenCraftable) and still use craft buckets if `EnableCraftableFirst` and mode is empty or `CraftableFirst`.

---

## Entries

### Section `1 - General`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `EnableMod` | bool | true | Almost every public UI/sort/dismantle entry | Master switch. Harmony still applied; logic returns early |
| `EnableCategories` | bool | true | `CategoryBar.Show`, `RecipeSort.Apply` | Chips + category strip of recipe list |

### Section `2 - Sorting`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `EnableCraftableFirst` | bool | true | `ModConfig.UseCraftBuckets` | If false, craft buckets off even in CraftableFirst modes |
| `EnableArmorSetGrouping` | bool | true | `RecipeSort` | Armor set keys + piece order |
| `EnableWeaponSetGrouping` | bool | true | `RecipeSort` | Weapon material groups; also triggers bucket compare path with other grouping flags |
| `EnableToolSetGrouping` | bool | true | `RecipeSort` | Tool groups |
| `EnableShieldSetGrouping` | bool | true | `RecipeSort` | Shield groups |
| `EnableMaterialSectionHeaders` | bool | true | `MaterialSectionHeaders.Apply`, `RecipeSort.Compare` (`useBuckets`) | IRON / Bronze labels in the list |
| `SortMode` | string | `CraftableFirst` | `Mode()`, `IsVanillaOrder`, `IsAlphabetical`, `IsProgression`, `IsCategoryThenCraftable`, `UseCraftBuckets` | Sort policy. Allowed (documented): `CraftableFirst`, `CategoryThenCraftable`, `Progression`, `Alphabetical`, `Vanilla` |

`UseCraftBuckets()` is true when `EnableCraftableFirst` and mode is `CraftableFirst`, `CategoryThenCraftable`, or empty.

### Section `3 - Armor`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `ArmorPieceOrder` | string | `Chest,Helmet,Legs,Cape,Other` | `ArmorSetDetector.ParseOrder` | Order inside a set |
| `GroupModdedArmorSets` | bool | true | `ArmorSetDetector.SetKey` | Heuristics for non-vanilla prefabs |

### Section `3b - Weapons`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `WeaponPieceOrder` | string | Arrow,Bolt,Knife,… (see `ModConfig.cs`) | `WeaponSetDetector.ParseOrder` | Type order inside a material group |
| `GroupModdedWeaponSets` | bool | true | Weapon **and** tool/shield detectors (`RecipeSort` passes this flag into tool/shield `SetKey`) | Modded material heuristics |

### Section `3c - Tools`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `ToolPieceOrder` | string | `Hammer,Hoe,Cultivator,Pickaxe,Scythe,Other` | `ToolSetDetector.ParseOrder` | Inner-set order |

### Section `3d - Shields`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `ShieldPieceOrder` | string | `Shield,Tower,Other` | `ShieldSetDetector.ParseOrder` | Inner-set order |

### Section `4 - Refresh`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `RefreshOnInventoryChange` | bool | true | `InventoryRefreshHook` | Re-run `UpdateCraftingPanel` after inventory debounce |
| `RefreshOnStationChange` | bool | true | `UpdateCraftingPanelCategoryPatch` Prefix | Reset category to All when `GetCurrentCraftingStation` instance changes |

### Section `5 - UI`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `ShowCraftabilityIndicators` | bool | **false** | `CraftabilityIndicators.Apply` | Green check on fully craftable rows |
| `EnableCraftMultiplier` | bool | true | `CraftMultiplierBar` | Arrows beside Craft |
| `MaxCraftMultiplier` | int | 99 | `CraftMultiplierBar.MaxAllowed` | Cap on arrow amount |
| `EnableDismantle` | bool | true | Dismantle tab, list patch, craft press | Dismantle feature |

### Section `9 - Debug`

| Name | Type | Default | Used by | Effect |
|---|---|---|---|---|
| `DebugLogging` | bool | false | `RecipeSort.ReorderGui`, `Craftability` catch, `CraftMultiplierBar` missing arrows | Extra `LogInfo` / `LogWarning` |

---

## Config → method → effect (short map)

```text
EnableMod                    → every Show/Apply early-out
EnableCategories             → CategoryBar.Show / RecipeSort category strip
SortMode + EnableCraftableFirst → RecipeSort.Compare / UseCraftBuckets
Enable*SetGrouping + *PieceOrder + GroupModded* → RecipeSort.BuildEntries
EnableMaterialSectionHeaders → MaterialSectionHeaders.Apply
RefreshOnInventoryChange     → Inventory.m_onChanged → UpdateCraftingPanel
RefreshOnStationChange       → CategoryBar.SetActive(All, rebuild: false)
ShowCraftabilityIndicators   → CraftabilityIndicators.Apply
EnableCraftMultiplier        → CraftMultiplierBar → m_multiCraftAmount / m_touchMultiCrafting
MaxCraftMultiplier           → clamp arrow amount
EnableDismantle              → DismantleTab + DismantleMode
DebugLogging                 → sort dump / warnings
```

There is **no range config**.
