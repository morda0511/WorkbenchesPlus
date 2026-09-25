# Workbenches+ - agent rules

Workbenches+ is a **working client-side crafting-UI mod**. Stability beats new architecture.

> Understand first → search existing code → check dependencies → change the minimum → build → regression-check.

> Do **not** invent a second solution when one already exists.

This is **not** a replacement for vanilla range math. Extra metres wrap **one** API: `CraftingStation.GetStationBuildRange` (`BuildRangeBonus`). Do not add a second calculator.

---

## 1. Understand before changing

Before any edit:

1. Find the relevant `.cs` files and methods.
2. Read the existing implementation (callers too).
3. Trace config → method → visible effect.
4. Check vanilla Valheim (`InventoryGui`, `Player`, `Recipe`, `CraftingStation`) by decompile or existing usage - do not guess.
5. Check git history when `.git` exists (`git log`, `git log -p -- path`).

---

## 2. Reuse existing code

Search before adding logic.

| Need | Existing owner |
|---|---|
| Keep recipes for the current station | `StationFilter` |
| Sort recipe list + GUI rows | `RecipeSort` |
| Category chips | `CategoryBar` + `RecipeCategories` |
| Craftable / partial buckets | `Craftability` |
| Material section labels | `MaterialNameBucket` + `MaterialSectionHeaders` |
| Armor / weapon / tool / shield groups | `*SetDetector` + `MaterialProgression` |
| Multi-craft arrows | `CraftMultiplierBar` (writes vanilla `m_multiCraftAmount`) |
| Dismantle | `DismantleMode` + `DismantlePatches` + `DismantleTab` |
| Reflect vanilla GUI fields | `AccessToolsExt` (in `UI/CategoryBar.cs`) |
| Extra hammer range | `BuildRangeBonus` + `Patches/BuildRangePatches.cs` (`GetStationBuildRange` postfix only) |

No parallel station filter, no second sort path, no second dismantle refund.

---

## 3. Minimal changes

Change only what the current task needs.

Do not do unsolicited refactors, renames, architecture moves, cleanup, or performance work.

---

## 4. Do not reimplement vanilla

Use Valheim APIs already in use:

- `Player.HaveRequirements`, `Player.GetCurrentCraftingStation`
- `InventoryGui.UpdateCraftingPanel` / `UpdateRecipeList` / `UpdateRecipe`
- `ObjectDB.GetRecipe` / `m_recipes`
- `Inventory` add/remove for dismantle
- Vanilla Craft/Upgrade tab `interactable` convention

Do not reimplement vanilla `GetStationBuildRange` math (`m_rangeBuild` + extensions). Extra metres go through `BuildRangeBonus.AddExtra` only.

---

## 5. No unsolicited bugfixes

If you find an unrelated bug, document it and leave it:

> Additional issue found: X  
> Affected system: Y  
> Not changed (out of scope).

---

## 6. No guessing

If a Valheim / Unity / Harmony API is unclear:

1. Search this repo.
2. Decompile `assembly_valheim.dll` if needed.
3. Check git history.

If still unclear: write `UNKNOWN - VERIFY`. Do not invent field defaults, prefab colliders, or network behaviour.

---

## 7. Trace dependencies before editing hubs

These methods affect several systems:

- `StationFilter.Apply` / `BelongsToStation` - recipe list **and** dismantle eligibility
- `RecipeSort.Apply` / `ReorderGui` - list contents + on-screen order
- `AccessToolsExt.RebuildCraftingPanel` - nested rebuild / freeze risk
- `DismantleMode.Active` - Harmony prefixes skip vanilla craft
- `Craftability.ScoreBucket` - sort buckets, headers, optional checkmarks
- `CategoryBar.Active` - filters `RecipeSort.Apply`

---

## 8. Build after code changes

```text
dotnet build -c Release
```

`Deploy` copies the DLL to the Thunderstore **Solo Gameplay** profile (`Directory.Build.props` → `BepInExDir`).

Do **not** pack Thunderstore/Hexium/Nexus zips unless the user asks (`-p:Pack=true`).

In-game test: open a station, confirm list + sort + craft still work.

---

## 9. Regression after changes

At least:

- Workbench / forge / cauldron recipe list still appears
- Forge of Potential upgrade list is **not** empty
- Craft / Upgrade tabs still switch
- Dismantle only at normal stations, not Potential forge
- Multi-craft arrows still drive vanilla `HaveRequirements(..., amount)`
- Config master switch `EnableMod` still disables extra UI

This mod does **not** own workbench range, building radius, or repair radius. Do not “regression-test range” as if WB+ changed it - vanilla still owns those.

---

## 10. Git diff

If git exists: review `git diff`. Only intended files. No debug leftovers, temp dumps, or accidental zips.

---

## Change protocol

```text
REQUEST
→ UNDERSTAND
→ SEARCH EXISTING IMPLEMENTATION
→ CHECK VANILLA
→ TRACE DEPENDENCIES
→ CHECK GIT HISTORY
→ ROOT CAUSE
→ AFFECTED SYSTEMS
→ MINIMAL PLAN
→ IMPLEMENT
→ BUILD
→ TEST
→ REGRESSION
→ REVIEW DIFF
→ REPORT
```

### Report format

**Changed** - file, method, what  
**Cause** - root cause  
**Affected systems**  
**Tests** - build + function  
**Regression** - what still works  
**Additionally found** - issues **not** fixed
