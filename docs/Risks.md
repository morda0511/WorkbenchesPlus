# Regression risks

WB+ does **not** own vanilla range. Do not treat “workbench range still 20m” as a WB+ regression unless you added range code.

---

## InventoryGui.UpdateRecipeList (Harmony First + Last)

**Why critical:** Entire recipe UI. Empty list = “mod broke the station”.

**Affected:** Craft, Upgrade, Forge of Potential, Dismantle, sort, categories.

**Test:** Open workbench, forge, cauldron, black forge, galdr, artisan, **Forge of Potential**. Confirm rows exist. Switch Craft/Upgrade. Enable dismantle and leave it.

---

## StationFilter

**Why critical:** `Apply` mutates the list **before** vanilla upgrade expansion. `BelongsToStation` also gates dismantle.

**Affected:** What you can see and what you can dismantle.

**Test:** Same-station recipes only on a workbench (no forge-only swords). Potential forge still shows upgradeable inventory items. Dismantle at Potential forge stays unavailable.

---

## Dismantle OnCraftPressed / DoCrafting Prefix `return false`

**Why critical:** Skips vanilla craft. Bugs eat items or skip crafts.

**Affected:** Crafting, upgrade, inventory, refunds.

**Test:** Normal craft still works with dismantle **off**. Dismantle removes the item and adds mats. Cancel mid-timer. Full inventory → `$inventory_full`. Equipped items not listed.

---

## AccessToolsExt.RebuildCraftingPanel

**Why critical:** Nested `UpdateCraftingPanel` historically froze the crafting UI (comment in Prefix). Guard `_rebuildDepth`.

**Affected:** Category clicks, multi-craft arrows, dismantle enter.

**Test:** Spam category chips and multiplier arrows; UI stays responsive.

---

## RecipeSort.ReorderGui vs vanilla List.Sort

**Why critical:** Vanilla sorts `m_availableRecipes` at the end of `UpdateRecipeList`. Without postfix reorder, sort looks like a no-op.

**Affected:** All sort modes, section headers, scroll height.

**Test:** CraftableFirst moves craftable rows; headers appear; scrolling reaches last row.

---

## CraftMultiplierBar vanilla fields

**Why critical:** Writes `m_multiCraftAmount` / `m_touchMultiCrafting`. Wrong values craft N items or break Shift multi-craft.

**Affected:** Craft button, requirement counts.

**Test:** Arrow 3× crafts three; at 1×, vanilla modifier multi-craft still works. Upgrade tab hides arrows.

---

## Inventory m_onChanged refresh

**Why critical:** Rebuild during multi-craft desyncs lists. Debounce + `IsCraftInProgress` exist for that.

**Affected:** Sort while looting/crafting.

**Test:** Craft a stack; list updates after finish, not mid-timer.

---

## InCraftTab / InUpradeTab postfixes

**Why critical:** Dismantle must look like Craft tab to vanilla. Wrong path uses upgrade `ItemData` and breaks `RemoveItem`.

**Affected:** Dismantle only.

**Test:** Dismantle the selected row, not a different stack.

---

## Config EnableMod / EnableDismantle

**Why critical:** Players disable features via config.

**Test:** `EnableMod` false → vanilla list, no chips. `EnableDismantle` false → no tab.

---

## Multiplayer / dedicated

**Why critical:** Dismantle mutates **local** `Inventory` only. No RPCs.

**Affected:** Other clients see inventory via vanilla sync **if** Valheim replicates that inventory (normal player inventory).

**Test:** Host + client: dismantle on one client; other sees items. Dedicated: **UNKNOWN - VERIFY** whether `InventoryGui` types exist; README says client-side only.

---

## Other mods

**Risk:** Another mod Prefixes `UpdateRecipeList` or `DoCrafting`. No explicit compat.

**Test:** If using craft-from-chest mods, confirm WB+ list still fills (WB+ does not count chests).

---

## Prefab / range / CraftingStation fields

**Not currently sensitive in WB+** because unused. Becomes critical the moment someone patches them - then roof, fire, hammer radius, and all stations diverge.
