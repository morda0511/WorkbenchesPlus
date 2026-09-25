# Workbenches and range

## Finding (this repo)

Vanilla still owns `m_rangeBuild` + extensions. Workbenches+ can **override** the result of `GetStationBuildRange` with an absolute radius (50 / 100 / 150 / 200 m) via `BuildRangeBonus`. Value `0` keeps vanilla. Not a second range formula.

Cycle: look at station, Shift+E (vanilla `AltPlace` + `Use`): default → 50 → 100 → 150 → 200 → default. ZDO key `WBP_buildExtra`. Stations with `m_rangeBuild <= 0` are skipped (no hammer radius).


---

## How WB+ “uses” workbenches

Stations are **not** identified by prefab name in a whitelist. The mod reads whatever `Player.GetCurrentCraftingStation()` returns (vanilla `m_currentStation`, set in `CraftingStation.Interact` via `SetCraftingStation`).

Recipe membership uses **`CraftingStation.m_name`** (localization token such as `$piece_workbench`), compared to `recipe.m_craftingStation.m_name`.

Vanilla stations that show a crafting GUI all go through the same `InventoryGui` path. They are **not** identical:

| Kind | Vanilla behaviour WB+ relies on | WB+ extra |
|---|---|---|
| Normal station (`m_hasCraftTab` true) | Craft + Upgrade tabs; `RequiredCraftingStation` matches `m_name` + level | `StationFilter` by `m_name` |
| No basic recipes (`m_showBasicRecipies` false) | Hand recipes omitted by vanilla and by `StationFilter` | Same |
| Forge of Potential (`m_upgrader`, typically `m_hasCraftTab` false) | `RequiredCraftingStation` always true; upgrade tab only; idol resources | Must **not** filter by `m_name` (empty list). Dismantle hidden |
| World vs built piece | Same `CraftingStation` component | No distinction in code |

Prefab names (e.g. `piece_workbench`) appear only as **heuristics** in `RecipeCategories` (building/furniture chips) and in item-set detectors - not as a station registry.

**Workbench prefabs shipped by this mod:** none.

---

## Vanilla range (not modified by WB+)

Decompiled from `assembly_valheim.dll` `CraftingStation` (Valheim 1.0 install used for analysis). Serialized **numeric defaults on each prefab are UNKNOWN - VERIFY** (not in C#).

| Field / method | Role |
|---|---|
| `m_useDistance` | `InUseDistance`: player must be closer than this to `Interact` / open GUI |
| `m_rangeBuild` | Base build radius |
| `m_extraRangePerLevel` | Added per attached `StationExtension` |
| `m_buildRange` | Runtime: `m_rangeBuild + GetExtentionCount(false) * m_extraRangePerLevel` inside `GetExtensions` (refreshed when `m_updateExtensionTimer >= 2f`) |
| `GetStationBuildRange()` | Calls `GetExtensions()`, returns `m_buildRange` |
| `HaveBuildStationInRange(name, point)` | Hammer/build: station of that `m_name` with flattened Y distance `< GetStationBuildRange()` |
| `m_discoverRange` | Field exists. **Usage in WB+: none. Full vanilla callers: UNKNOWN - VERIFY** |
| `m_areaMarker` / `m_effectAreaCollider` | Vanilla updates radius to `m_buildRange` in `GetExtensions` |

Interact path (vanilla, unpatched): `InUseDistance` → `CheckUsable` (roof / fire) → `SetCraftingStation` → `InventoryGui.Show(null, 3)`.

Repair and crafting **button enable** use `HaveRequirements` / `CheckUsable` / inventory, not `m_rangeBuild`. Build radius is separate from use distance.

Comfort is **not** referenced in Workbenches+ source.

---

## When vanilla range is applied

Vanilla: on the **station instance** (prefab serialized values + runtime extension recount). Not on plugin Awake. WB+ does not hook spawn.

---

## Indirect influence

Because WB+ adds extra only through `GetStationBuildRange`, hammer/build checks that call that method pick up the bonus. Craft GUI use-distance (`m_useDistance`), roof, and fire are unchanged. Extension *attachment* still uses each extension’s own `m_maxStationDistance`, not the extra metres.
