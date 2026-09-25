# Valheim / BepInEx / Harmony notes

Facts from this repo plus decompile of `assembly_valheim.dll` where cited. Jotunn is **not used**.

---

## BepInEx

- `[BepInPlugin("com.morda.workbenchesplus", "Workbenches+", version)]`
- `BaseUnityPlugin`: `Awake` / `OnDestroy` only (no `Update` on Plugin; inventory debounce uses `InventoryGui.Update` postfix)
- Config: `Config.Bind` in `ModConfig`
- Logging: `ManualLogSource` `Plugin.Log`
- Target: `net472`, Valheim 1.0, dependency `denikson-BepInExPack_Valheim-5.4.2350` (Thunderstore manifest)
- Deploy: `Directory.Build.props` `BepInExProfile` defaults to Thunderstore **Solo Gameplay**

No `[BepInDependency]` on other mods. No Jotunn. No soft Epic Loot API.

---

## Harmony

`new Harmony(ModGuid); PatchAll(executing assembly);` in Awake. `UnpatchSelf` in OnDestroy.

All patches are Prefix/Postfix. **No Transpiler.**

### `InventoryGui` - list / overlay

| Target | Type | Priority | Class |
|---|---|---|---|
| `UpdateRecipeList` | Prefix + Postfix | **Last** | `UpdateRecipeListSortPatch` |
| `UpdateCraftingPanel` | Prefix + Postfix | default | `UpdateCraftingPanelCategoryPatch` |
| `Update` | Postfix | default | `InventoryGuiUpdateRefreshPatch` |
| `Show` | Postfix | default | `InventoryGuiShowBindPatch` |
| `Hide` | Prefix | default | `InventoryGuiHideUnbindPatch` |
| `UpdateRecipe` | Prefix + Postfix | default | `UpdateRecipeMultiplierPatch` |

### `InventoryGui` - dismantle

| Target | Type | Priority | Class |
|---|---|---|---|
| `UpdateRecipeList` | Prefix | **First** | `DismantleRecipeListPatch` |
| `InCraftTab` | Postfix | default | Force true if dismantle active |
| `InUpradeTab` | Postfix | default | Force false if dismantle (vanilla **method name spelling**) |
| `AddRecipeToList` | Prefix | default | Inject live `ItemData`, `canCraft = true` |
| `SetupRequirement` | Postfix | default | White requirement colors |
| `SetupRequirementList` | Postfix | default | Strip idol requirement rows |
| `SetRecipe` | Postfix | default | Bind selected inventory item |
| `UpdateRecipe` | Postfix | default | DISMANTLE labels, button interactable |
| `OnCraftPressed` | Prefix | default | `return false` - skip vanilla |
| `DoCrafting` | Prefix | default | If dismantle progress: complete, `return false` |
| `OnCraftCancelPressed` | Prefix | default | Cancel progress |
| `OnTabCraftPressed` | Prefix + Postfix | default | Leave dismantle |
| `OnTabUpgradePressed` | Prefix + Postfix | default | Leave dismantle |

Two Prefixes on `UpdateRecipeList`: **First** (dismantle rebuilds list) then **Last** (filter/sort). Vanilla runs between Last prefix and Last postfix.

Harmony vs other list-sorting mods: last postfix wins for row positions. **No compatibility layer exists.**

`AccessTools.Field` / `Method` / `Property` used widely; missing members fail soft (null checks) or log warnings.

---

## Unity

Runtime UI only: `GameObject`, `RectTransform`, `Button`, `Image`, `TMP_Text`, `VerticalLayoutGroup`, `Object.Instantiate` / `Destroy`. Clones vanilla Craft/Repair/Upgrade/quality-arrow objects.

No custom MonoBehaviour types besides BepInEx `Plugin`. No physics. No `Input` / hotkeys.

---

## Vanilla types touched (read or Harmony)

| Type | How |
|---|---|
| `InventoryGui` | Patched; fields `m_availableRecipes`, `m_recipeListSpace`, tabs, craft button, multi-craft, craft timer, requirement list |
| `Player` | `m_localPlayer`, `GetCurrentCraftingStation`, `HaveRequirements(Recipe,…)`, `GetInventory`, `Message` |
| `CraftingStation` | **Read only**: `m_name`, `m_showBasicRecipies`, `m_upgrader`, craft effect lists |
| `Recipe` | `m_craftingStation`, `m_resources`, `m_item`, `m_amount`, `m_enabled`, `m_minStationLevel` |
| `Piece.Requirement` | `GetAmount`, `m_amount`, `m_amountPerLevel`, `m_upgraderResource`, `m_resItem` |
| `Inventory` | Count/add/remove; `m_onChanged` |
| `ObjectDB` | `GetRecipe`, `m_recipes`, `GetItemPrefab` |
| `ItemDrop` / `ItemData` | Prefab names, quality, shared type |
| `EffectList` | Dismantle SFX via station or GUI fields |
| `MessageHud` | Center messages |

**Not patched:** `CraftingStation.Interact`, `CheckUsable`, `HaveBuildStationInRange`, `Player.HaveRequirementItems` (called only through vanilla `HaveRequirements`), `Player.ConsumeResources`.

---

## Vanilla list vs WB+ filter

`Player.RequiredCraftingStation` (decompile): if `m_currentStation.m_upgrader` → **true** (ignore recipe station name). Else match `m_name` and optional level. Else if no required station and current `!m_showBasicRecipies` → false.

`StationFilter` originally matched only `m_name`, which **disagrees** with the upgrader special case. 1.0.3 skips filter on upgrader.

`InventoryGui.InCraftTab` (decompile): if `m_tabCraft.interactable` is false and the object is active → in craft tab. Upgrader hides craft tab (`m_hasCraftTab` false) so upgrade path runs. Dismantle postfix forces `InCraftTab` true so vanilla does not take the upgrade-item path (would desync `DismantleMode.Items`).

---

## Prefabs / assets

**This plugin ships no workbench prefabs.** Thunderstore `icon.png` is packaging only.

Vanilla station prefab internals (colliders, triggers, `CraftingStation` serialized floats): **not inspected in Unity**. Do not copy numbers from other mods. **UNKNOWN - VERIFY** if a task needs exact `m_useDistance` per piece.

Untracked JSON dumps in the repo root look like AssetStudio recipe/station path-id maps. They are **not** referenced in `.cs`.
