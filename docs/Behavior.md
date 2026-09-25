# Behavior

Observed from source. Player-facing copy lives in `README.md`.

## When the mod does nothing

- `EnableMod` is false: patches still run but early-return on settings checks; extra UI is hidden.
- Crafting GUI is closed: `Hide` detaches inventory hook and hides overlays.
- `SortMode` is `Vanilla`: `RecipeSort` does not reorder rows (`IsVanillaOrder`). Station filter and categories can still run if enabled.

## Station recipe list (Craft tab)

Vanilla `GetAvailableRecipes` already limits by `RequiredCraftingStation` (name + optional level). Workbenches+ **additionally** strips the list in `StationFilter.Apply` to recipes whose `recipe.m_craftingStation.m_name` equals `player.GetCurrentCraftingStation().m_name` (or hand recipes if `m_showBasicRecipies`).

“All” means **all recipes for this station**, not every station in the game.

## Upgrade tab

Vanilla `UpdateRecipeList` uses inventory items with `maxQuality > 1`. WB+ Prefix still filters the **recipe** list first. That is enough for normal forges (recipe station name matches).

## Forge of Potential (`CraftingStation.m_upgrader`)

Vanilla `Player.RequiredCraftingStation` returns **true for every recipe** at an upgrader. The upgrade tab then keeps recipes that have an `m_upgraderResource` row and lists **inventory** gear.

WB+ 1.0.3: `StationFilter.Apply` **returns immediately** when `current.m_upgrader` so that list is not emptied. `BelongsToStation` also returns true at an upgrader (used by dismantle lookup - dismantle itself is blocked separately).

Dismantle tab is **hidden** at upgrader stations. `TryGetDismantleRecipe` returns false if `station.m_upgrader`.

## Sorting

`RecipeSort.Compare` combines, depending on config:

- Category order (`CategoryThenCraftable`)
- Craft buckets (`Craftability.ScoreBucket`: fully / partial / none / blocked)
- Material buckets (`MaterialNameBucket`) and biome tier (`MaterialProgression`)
- Armor/weapon/tool/shield set keys and piece order
- Station min level (`Progression` mode)
- Localized/shared item name
- Original index (stable)

`ReorderGui` rewrites `m_availableRecipes` order and `anchoredPosition.y`. Optional `MaterialSectionHeaders` insert extra rows and grow the list height.

## Categories

Chips under Repair. Clicking a chip sets `CategoryBar.Active` and calls `RebuildCraftingPanel`. Chips with no recipes on the current station list are hidden after `SyncAvailable`.

Switching stations (`RefreshOnStationChange`): reset to All **without** rebuild in `UpdateCraftingPanel` Prefix (nested rebuild was found to freeze the UI).

## Craftability checkmarks

Off by default (`ShowCraftabilityIndicators`). Green “✓” on rows where `ScoreBucket == BucketFully`.

`ScoreBucket` calls `HaveRequirements(recipe, false, qualityLevel: 1)` first. At the Potential forge, vanilla `HaveRequirementItems` only counts idol (`m_upgraderResource`) rows. `CountsTowardCraft` mirrors that skip for the material fallback loop.

## Multi-craft

Arrows clone vanilla quality up/down buttons. When amount > 1, WB+ sets:

- `InventoryGui.m_multiCraftAmount` = amount
- `InventoryGui.m_touchMultiCrafting` = true

Vanilla `UpdateRecipe` already uses those for requirement counts and the Craft label. Amount 1 restores vanilla default 5 + `m_touchMultiCrafting` false so Shift/L2 multi-craft stays vanilla.

Hidden in dismantle mode and on Upgrade tab (arrow hide from dismantle/upgrade patches).

## Dismantle

Third tab cloned from Upgrade. Entering does **not** call `OnTabCraftPressed` (that would lock Craft as selected).

List = unequipped inventory items whose `ObjectDB.GetRecipe` / prefab recipe **belongs to the current station** and stack ≥ `recipe.m_amount`.

Confirm uses vanilla craft timer (`m_craftTimer = 0`) and station craft effects. `OnCraftPressed` Prefix returns false (skips vanilla). `DoCrafting` Prefix completes refund and returns false.

Refund: custom sum of non-idol `m_amount` / `m_amountPerLevel` over quality 1..item quality (`CraftMaterialAmount`). **Not** vanilla `m_recover` / Potential-forge break returns. Idol rows (`m_upgraderResource`, prefab `Upgrader*`, shared name containing `idol`) are skipped. `SetupRequirementList` postfix also strips those rows from the requirement display while dismantle is active.

English center messages: `Dismantled`, `Dismantle failed`, plus vanilla `$inventory_full`.

## Inventory refresh

If `RefreshOnInventoryChange`, subscribe to local inventory `m_onChanged`. Debounce 0.2s; skip while `m_craftRecipe` is set or craft timer > 0.01. Then invoke `UpdateCraftingPanel(false)`.

## What does **not** happen

- No change to `m_useDistance`, `m_rangeBuild`, `m_buildRange`, extensions, roof, fire.
- No hammer/build piece list patches (`HaveRequirements(Piece)` is unused).
- No hotkeys / Input system.
- No ZDO writes, RPCs, or config sync.
- No Jotunn prefabs.
