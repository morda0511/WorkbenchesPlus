using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Dismantle mode: destroy a station-crafted inventory item and refund its materials.
    /// Uses the same craft timer / sounds as vanilla crafting.
    /// </summary>
    internal static class DismantleMode
    {
        public static bool Active { get; private set; }

        /// <summary>Parallel to the recipe list rows while Active (same index).</summary>
        public static readonly List<ItemDrop.ItemData> Items = new List<ItemDrop.ItemData>();

        /// <summary>The recipe of each entry in Items (same index): lets the row builder match by recipe, not by position.</summary>
        public static readonly List<Recipe> Recipes = new List<Recipe>();

        /// <summary>The inventory item behind each row that was really created (same index as the GUI rows).</summary>
        public static readonly List<ItemDrop.ItemData> RowItems = new List<ItemDrop.ItemData>();
        private static int _nextEntry;

        /// <summary>Start of a list rebuild: forget the previous rows.</summary>
        public static void BeginRows()
        {
            RowItems.Clear();
            _nextEntry = 0;
        }

        /// <summary>
        /// Called for every row the game creates. Vanilla skips some recipes (upgrade-only ones) without telling us,
        /// so the item is looked up by recipe, walking forward through our list, never by counting rows.
        /// </summary>
        public static ItemDrop.ItemData ClaimItemForRow(Recipe recipe, ItemDrop.ItemData fallback)
        {
            int i = _nextEntry;
            while (i < Recipes.Count && Recipes[i] != recipe)
                i++;
            ItemDrop.ItemData item = fallback;
            if (i < Recipes.Count && i < Items.Count)
            {
                item = Items[i];
                _nextEntry = i + 1;
            }
            RowItems.Add(item);
            return item;
        }

        private static void SnapshotRecipes(List<Recipe> recipes)
        {
            Recipes.Clear();
            if (recipes != null)
                Recipes.AddRange(recipes);
        }

        private static ItemDrop.ItemData _progressItem;
        private static Recipe _progressRecipe;

        private static readonly FieldInfo CraftRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_craftRecipe");
        private static readonly FieldInfo CraftUpgradeItemField =
            AccessTools.Field(typeof(InventoryGui), "m_craftUpgradeItem");
        private static readonly FieldInfo CraftTimerField =
            AccessTools.Field(typeof(InventoryGui), "m_craftTimer");
        private static readonly FieldInfo CraftItemEffectsField =
            AccessTools.Field(typeof(InventoryGui), "m_craftItemEffects");
        private static readonly FieldInfo CraftItemDoneEffectsField =
            AccessTools.Field(typeof(InventoryGui), "m_craftItemDoneEffects");
        private static readonly FieldInfo CraftingVibrationField =
            AccessTools.Field(typeof(InventoryGui), "CraftingVibration");

        public static bool HasProgress => _progressItem != null;

        public static void SetActive(bool on)
        {
            Active = on;
            if (!on)
            {
                Items.Clear();
                CancelProgress(null);
            }
        }

        public static void EnterFromUi(InventoryGui gui)
        {
            if (gui == null)
                return;

            // Do NOT call OnTabCraftPressed - that marks Craft as selected
            // (interactable=false) and blocks clicking Craft until Upgrade is pressed.
            SetActive(true);

            Button craft = AccessToolsExt.TabCraft(gui);
            Button upgrade = AccessToolsExt.TabUpgrade(gui);
            if (craft != null)
                craft.interactable = true;
            if (upgrade != null)
                upgrade.interactable = true;

            CraftMultiplierBar.ForceHideArrows();
            DismantleTab.RefreshVisuals(gui);
            AccessToolsExt.RebuildCraftingPanel();
            DismantleTab.RefreshVisuals(gui);
        }

        public static void ClearIfNotSuppressed()
        {
            if (Active)
            {
                SetActive(false);
                DismantleTab.RefreshVisuals(InventoryGui.instance);
            }
        }

        public static void BuildList(List<Recipe> recipes)
        {
            Items.Clear();
            Recipes.Clear();
            if (recipes == null)
                return;
            recipes.Clear();

            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            Inventory inv = player.GetInventory();
            if (inv == null)
                return;

            CraftingStation station = player.GetCurrentCraftingStation();
            List<ItemDrop.ItemData> all = inv.GetAllItems();
            if (all == null || all.Count == 0)
                return;

            for (int i = 0; i < all.Count; i++)
            {
                ItemDrop.ItemData item = all[i];
                if (item == null || item.m_shared == null)
                    continue;
                if (item.m_equipped)
                    continue;

                Recipe recipe;
                if (!TryGetDismantleRecipe(player, station, item, out recipe))
                    continue;

                // The game builds no row for upgrade-only recipes; keep them out so list and rows stay aligned.
                if (recipe.m_noCraftOnlyUpgrade)
                    continue;

                recipes.Add(recipe);
                Items.Add(item);
            }
            SnapshotRecipes(recipes);
        }

        /// <summary>What dismantling this item gives back (empty when it cannot be dismantled here).</summary>
        public static List<KeyValuePair<ItemDrop, int>> RefundFor(Player player, CraftingStation station, ItemDrop.ItemData item)
        {
            Recipe recipe;
            if (!TryGetDismantleRecipe(player, station, item, out recipe))
                return new List<KeyValuePair<ItemDrop, int>>();
            return BuildRefund(recipe, item.m_quality);
        }

        public static bool CanDismantle(Player player, CraftingStation station, ItemDrop.ItemData item)
        {
            Recipe recipe;
            return TryGetDismantleRecipe(player, station, item, out recipe);
        }

        private static bool TryGetDismantleRecipe(
            Player player,
            CraftingStation station,
            ItemDrop.ItemData item,
            out Recipe recipe)
        {
            recipe = null;
            if (player == null || item?.m_shared == null)
                return false;
            if (item.m_equipped)
                return false;

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
                return false;

            recipe = FindRecipe(item, station);
            if (recipe == null || recipe.m_item == null)
                return false;
            // "Any one of these" recipes consume only one ingredient; which one is unknown afterwards.
            if (recipe.m_requireOnlyOneIngredient)
                return false;
            // Forge of Potential is upgrade-only; do not treat every item as dismantleable.
            if (StationFilter.IsUpgrader(station))
                return false;
            if (!StationFilter.BelongsToStation(recipe, station))
                return false;

            int need = Mathf.Max(1, recipe.m_amount);
            if (item.m_stack < need)
                return false;

            return true;
        }

        public static void SortPaired(List<Recipe> recipes)
        {
            if (recipes == null || Items.Count != recipes.Count)
                return;

            var order = new List<int>(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
                order.Add(i);

            order.Sort((a, b) =>
            {
                int c = SortEngine.Compare(recipes[a], recipes[b]);
                if (c != 0)
                    return c;
                // Same recipe: best quality first, then stable.
                c = Items[b].m_quality.CompareTo(Items[a].m_quality);
                return c != 0 ? c : a.CompareTo(b);
            });

            var newRecipes = new List<Recipe>(recipes.Count);
            var newItems = new List<ItemDrop.ItemData>(Items.Count);
            for (int i = 0; i < order.Count; i++)
            {
                int idx = order[i];
                newRecipes.Add(recipes[idx]);
                newItems.Add(Items[idx]);
            }

            recipes.Clear();
            Items.Clear();
            for (int i = 0; i < newRecipes.Count; i++)
            {
                recipes.Add(newRecipes[i]);
                Items.Add(newItems[i]);
            }
            SnapshotRecipes(recipes);
        }

        public static Recipe FindRecipe(ItemDrop.ItemData item, CraftingStation station)
        {
            if (item?.m_shared == null || ObjectDB.instance == null)
                return null;

            // Prefer vanilla lookup - exact item → recipe, no shared-name collisions.
            try
            {
                Recipe direct = ObjectDB.instance.GetRecipe(item);
                if (direct != null && StationFilter.BelongsToStation(direct, station))
                    return direct;
            }
            catch
            {
            }

            List<Recipe> recipes = ObjectDB.instance.m_recipes;
            if (recipes == null)
                return null;

            string prefab = ResolvePrefabName(item);
            if (string.IsNullOrEmpty(prefab))
                return null;

            Recipe prefabHit = null;
            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe r = recipes[i];
                if (r == null || r.m_item == null)
                    continue;
                try
                {
                    if (!r.m_enabled)
                        continue;
                }
                catch
                {
                }

                if (!r.m_item.name.Equals(prefab, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                if (StationFilter.BelongsToStation(r, station))
                    return r;
                if (prefabHit == null)
                    prefabHit = r;
            }

            if (prefabHit != null && StationFilter.BelongsToStation(prefabHit, station))
                return prefabHit;
            return null;
        }

        private static string ResolvePrefabName(ItemDrop.ItemData item)
        {
            if (item == null)
                return null;
            if (item.m_dropPrefab != null && !string.IsNullOrEmpty(item.m_dropPrefab.name))
                return StripClone(item.m_dropPrefab.name);

            if (ObjectDB.instance == null || item.m_shared == null)
                return null;

            try
            {
                GameObject go = ObjectDB.instance.GetItemPrefab(item.m_shared);
                if (go != null)
                    return StripClone(go.name);
            }
            catch
            {
            }
            return null;
        }

        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            const string clone = "(Clone)";
            if (name.EndsWith(clone, System.StringComparison.Ordinal))
                return name.Substring(0, name.Length - clone.Length).Trim();
            return name;
        }

        /// <summary>
        /// Own refund math for workbench craft + normal upgrades only.
        /// Skips Forge-of-Potential idol rows (m_upgraderResource / Upgrader* prefabs).
        /// Does not use piece-recover or GetAmount for upgrader mats.
        /// </summary>
        public static List<KeyValuePair<ItemDrop, int>> BuildRefund(Recipe recipe, int quality)
        {
            var list = new List<KeyValuePair<ItemDrop, int>>();
            if (recipe?.m_resources == null)
                return list;

            if (quality < 1)
                quality = 1;

            var totals = new Dictionary<string, ItemDrop>(System.StringComparer.OrdinalIgnoreCase);
            var amounts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);

            Piece.Requirement[] res = recipe.m_resources;
            for (int i = 0; i < res.Length; i++)
            {
                Piece.Requirement req = res[i];
                if (req?.m_resItem == null)
                    continue;
                if (IsIdolOrUpgraderResource(req))
                    continue;

                int amount = ScaleRefund(CraftMaterialAmount(req, quality));
                if (amount <= 0)
                    continue;

                ItemDrop drop = req.m_resItem;
                string key = drop.name;
                if (string.IsNullOrEmpty(key))
                    continue;

                totals[key] = drop;
                int have;
                amounts.TryGetValue(key, out have);
                amounts[key] = have + amount;
            }

            foreach (KeyValuePair<string, int> kv in amounts)
            {
                ItemDrop drop;
                if (!totals.TryGetValue(kv.Key, out drop) || drop == null || kv.Value <= 0)
                    continue;
                list.Add(new KeyValuePair<ItemDrop, int>(drop, kv.Value));
            }
            return list;
        }

        /// <summary>Applies DismantleRefundPercent to one refund line.</summary>
        private static int ScaleRefund(int amount)
        {
            ModConfig cfg = Plugin.Settings;
            if (cfg == null || amount <= 0)
                return amount;

            float factor = Mathf.Clamp(cfg.DismantleRefundPercent.Value, 0, 100) / 100f;
            if (factor <= 0f)
                return 0;

            // Any refund the config allows gives at least one piece.
            return Mathf.Max(1, Mathf.FloorToInt(amount * factor));
        }

        /// <summary>
        /// Idol / Forge-of-Potential rows must never be refunded at a normal station.
        /// </summary>
        private static bool IsIdolOrUpgraderResource(Piece.Requirement req)
        {
            if (req == null)
                return true;
            try
            {
                if (req.m_upgraderResource)
                    return true;
            }
            catch
            {
            }

            ItemDrop drop = req.m_resItem;
            if (drop == null)
                return true;

            string prefab = drop.name ?? "";
            if (prefab.StartsWith("Upgrader", System.StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                string shared = drop.m_itemData?.m_shared?.m_name ?? "";
                if (shared.IndexOf("idol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Materials paid at a normal craft station to reach this quality:
        /// base m_amount + upgrade steps via m_amountPerLevel (same curve as GetAmount, without idol add-on).
        /// </summary>
        private static int CraftMaterialAmount(Piece.Requirement req, int quality)
        {
            if (req == null || quality < 1)
                return 0;

            int total = 0;
            for (int q = 1; q <= quality; q++)
            {
                if (q <= 1)
                {
                    total += Mathf.Max(0, req.m_amount);
                    continue;
                }

                // Mirror Piece.Requirement.GetAmount for non-upgrader rows (no m_amount re-add).
                float factor;
                if (q < 4)
                    factor = q - 1;
                else
                    factor = 4f + (q - 4) / 2f;

                total += Mathf.FloorToInt(factor * req.m_amountPerLevel);
            }
            return total;
        }

        private static readonly FieldInfo SelectedRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");
        private static readonly FieldInfo AvailableRecipesField =
            AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");

        /// <summary>
        /// The inventory item a GUI row stands for. The game sorts the rows again after we built them,
        /// so a row index says nothing about our own lists: the row itself (RecipeDataPair.ItemData,
        /// which we filled in AddRecipeToList) is the only reliable link.
        /// </summary>
        private static ItemDrop.ItemData ItemOfPair(object pair)
        {
            if (pair == null)
                return null;
            PropertyInfo p = pair.GetType().GetProperty("ItemData");
            return p != null ? p.GetValue(pair, null) as ItemDrop.ItemData : null;
        }

        private static ItemDrop.ItemData ItemOfRow(InventoryGui gui, int index)
        {
            if (gui == null || AvailableRecipesField == null || index < 0)
                return null;
            System.Collections.IList rows = AvailableRecipesField.GetValue(gui) as System.Collections.IList;
            if (rows == null || index >= rows.Count)
                return null;
            return ItemOfPair(rows[index]);
        }

        public static void BindSelectedItem(InventoryGui gui, int index)
        {
            if (!Active || gui == null)
                return;
            ItemDrop.ItemData item = ItemOfRow(gui, index);
            if (item == null)
                return;

            if (CraftUpgradeItemField != null)
                CraftUpgradeItemField.SetValue(gui, item);
        }

        public static ItemDrop.ItemData ResolveSelectedItem(InventoryGui gui)
        {
            if (gui == null)
                return null;

            // The selected row of the list as the game shows it right now.
            if (SelectedRecipeField != null)
            {
                ItemDrop.ItemData fromRow = ItemOfPair(SelectedRecipeField.GetValue(gui));
                if (fromRow != null)
                    return fromRow;
            }

            return CraftUpgradeItemField != null ? CraftUpgradeItemField.GetValue(gui) as ItemDrop.ItemData : null;
        }

        /// <summary>Start vanilla-style craft timer for dismantle (no material consume).</summary>
        public static bool TryBeginProgress(InventoryGui gui)
        {
            if (!Active || gui == null || HasProgress)
                return false;

            Player player = Player.m_localPlayer;
            if (player == null)
                return false;

            ItemDrop.ItemData item = ResolveSelectedItem(gui);
            if (item == null)
                return false;

            // Never dismantle the recipe template - only a live inventory stack.
            Inventory inv = player.GetInventory();
            item = ResolveLiveInventoryItem(inv, item);
            if (item == null)
                return false;

            CraftingStation station = player.GetCurrentCraftingStation();
            Recipe recipe;
            if (!TryGetDismantleRecipe(player, station, item, out recipe))
                return false;

            List<KeyValuePair<ItemDrop, int>> refund = BuildRefund(recipe, item.m_quality);
            if (!CanFitRefund(player.GetInventory(), refund, FreedSlots(item, recipe)))
            {
                player.Message(MessageHud.MessageType.Center, "$inventory_full");
                return true;
            }

            _progressItem = item;
            _progressRecipe = recipe;

            if (CraftRecipeField != null)
                CraftRecipeField.SetValue(gui, recipe);
            // Keep upgrade-item null so duration uses normal craft timing, not upgrade timing.
            if (CraftUpgradeItemField != null)
                CraftUpgradeItemField.SetValue(gui, null);
            if (CraftTimerField != null)
                CraftTimerField.SetValue(gui, 0f);

            TryInstantiateVibration(gui);
            PlayCraftEffects(gui, player, station, done: false);
            return true;
        }

        public static void CancelProgress(InventoryGui gui)
        {
            _progressItem = null;
            _progressRecipe = null;
            if (gui == null)
                return;
            if (CraftRecipeField != null)
                CraftRecipeField.SetValue(gui, null);
            if (CraftTimerField != null)
                CraftTimerField.SetValue(gui, -1f);
        }

        /// <summary>Called from DoCrafting Prefix when our progress finishes.</summary>
        public static bool TryCompleteProgress(InventoryGui gui)
        {
            if (!Active || _progressItem == null || _progressRecipe == null)
                return false;

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                CancelProgress(gui);
                return true;
            }

            ItemDrop.ItemData snapshot = _progressItem;
            Recipe recipe = _progressRecipe;
            int quality = snapshot != null ? snapshot.m_quality : 1;
            _progressItem = null;
            _progressRecipe = null;

            // Clear craft state before mutating inventory.
            if (CraftRecipeField != null)
                CraftRecipeField.SetValue(gui, null);
            if (CraftTimerField != null)
                CraftTimerField.SetValue(gui, -1f);

            CraftingStation station = player.GetCurrentCraftingStation();
            Inventory inv = player.GetInventory();
            if (inv == null || snapshot == null)
                return true;

            // Inventory may have rebuilt during the craft timer - resolve a live slot ref.
            ItemDrop.ItemData item = ResolveLiveInventoryItem(inv, snapshot);
            if (item == null || !CanDismantle(player, station, item))
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("dismantle_failed"));
                return true;
            }

            int remove = Mathf.Max(1, recipe.m_amount);
            if (item.m_stack < remove)
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("dismantle_failed"));
                return true;
            }

            List<KeyValuePair<ItemDrop, int>> refund = BuildRefund(recipe, quality);
            if (!CanFitRefund(inv, refund, FreedSlots(item, recipe)))
            {
                player.Message(MessageHud.MessageType.Center, "$inventory_full");
                return true;
            }

            // Destroy first; only refund if the item actually left / stack dropped.
            int stackBefore = item.m_stack;
            bool removed;
            if (remove >= item.m_stack)
                removed = inv.RemoveItem(item);
            else
                removed = inv.RemoveItem(item, remove);

            if (!removed)
                removed = TryForceRemove(inv, snapshot, remove);

            // Confirm the inventory changed for THIS item (by reference). Looking it up by name again would
            // find a second, identical copy and wrongly report "not removed" - item gone, no refund.
            if (removed)
            {
                bool wholeStack = remove >= stackBefore;
                removed = wholeStack ? !inv.ContainsItem(item) : item.m_stack < stackBefore;
            }

            if (!removed)
            {
                player.Message(MessageHud.MessageType.Center, Loc.T("dismantle_failed"));
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Dismantle: could not remove selected item from inventory");
                return true;
            }

            for (int i = 0; i < refund.Count; i++)
            {
                ItemDrop drop = refund[i].Key;
                int amount = refund[i].Value;
                if (drop == null || amount <= 0)
                    continue;

                string prefabName = drop.name;
                if (string.IsNullOrEmpty(prefabName))
                    continue;

                int left = amount;
                while (left > 0)
                {
                    int stack = left;
                    try
                    {
                        int max = drop.m_itemData?.m_shared != null
                            ? Mathf.Max(1, drop.m_itemData.m_shared.m_maxStackSize)
                            : left;
                        stack = Mathf.Min(left, max);
                    }
                    catch
                    {
                    }

                    ItemDrop.ItemData added = inv.AddItem(prefabName, stack, 1, 0, 0L, "", false, false);
                    if (added == null)
                    {
                        if (!inv.AddItem(drop.gameObject, stack))
                        {
                            if (Plugin.Log != null)
                                Plugin.Log.LogWarning("Dismantle could not add " + prefabName + " x" + stack);
                            break;
                        }
                    }
                    left -= stack;
                }
            }

            PlayCraftEffects(gui, player, station, done: true);
            player.Message(MessageHud.MessageType.Center, Loc.T("dismantled"));
            return true;
        }

        private static ItemDrop.ItemData ResolveLiveInventoryItem(
            Inventory inv,
            ItemDrop.ItemData snapshot)
        {
            if (inv == null || snapshot?.m_shared == null)
                return null;

            if (inv.ContainsItem(snapshot))
                return snapshot;

            try
            {
                ItemDrop.ItemData at = inv.GetItemAt(snapshot.m_gridPos.x, snapshot.m_gridPos.y);
                if (at != null && SameDismantleTarget(at, snapshot))
                    return at;
            }
            catch
            {
            }

            List<ItemDrop.ItemData> all = inv.GetAllItems();
            if (all == null)
                return null;

            for (int i = 0; i < all.Count; i++)
            {
                ItemDrop.ItemData it = all[i];
                if (SameDismantleTarget(it, snapshot))
                    return it;
            }
            return null;
        }

        private static bool SameDismantleTarget(ItemDrop.ItemData a, ItemDrop.ItemData b)
        {
            if (a?.m_shared == null || b?.m_shared == null)
                return false;
            if (a.m_equipped || b.m_equipped)
                return false;
            if (!string.Equals(a.m_shared.m_name, b.m_shared.m_name, System.StringComparison.Ordinal))
                return false;
            if (a.m_quality != b.m_quality)
                return false;
            if (a.m_variant != b.m_variant)
                return false;
            return true;
        }

        private static bool TryForceRemove(Inventory inv, ItemDrop.ItemData snapshot, int amount)
        {
            if (inv == null || snapshot?.m_shared == null || amount <= 0)
                return false;

            ItemDrop.ItemData live = ResolveLiveInventoryItem(inv, snapshot);
            if (live == null)
                return false;

            if (amount >= live.m_stack)
                return inv.RemoveItem(live);

            return inv.RemoveItem(live, amount);
        }

        private static void TryInstantiateVibration(InventoryGui gui)
        {
            if (gui == null || CraftingVibrationField == null)
                return;
            try
            {
                GameObject vib = CraftingVibrationField.GetValue(gui) as GameObject;
                if (vib != null)
                    Object.Instantiate(vib);
            }
            catch
            {
            }
        }

        private static void PlayCraftEffects(
            InventoryGui gui,
            Player player,
            CraftingStation station,
            bool done)
        {
            if (player == null)
                return;

            EffectList effects = null;
            try
            {
                if (station != null)
                {
                    effects = done
                        ? station.m_craftItemDoneEffects
                        : station.m_craftItemEffects;
                }
            }
            catch
            {
            }

            if (effects == null && gui != null)
            {
                FieldInfo field = done ? CraftItemDoneEffectsField : CraftItemEffectsField;
                if (field != null)
                    effects = field.GetValue(gui) as EffectList;
            }

            if (effects == null)
                return;

            try
            {
                effects.Create(
                    player.transform.position,
                    Quaternion.identity,
                    null,
                    1f,
                    -1,
                    default(ZDOID));
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Dismantle effects: " + ex.Message);
            }
        }

        /// <summary>
        /// Exact fit check (vanilla Inventory.CanAddItem math, summed over all refund lines):
        /// free space in partial stacks first, the rest needs whole empty slots.
        /// <paramref name="freedSlots"/> = slots the dismantled item releases.
        /// </summary>
        private static bool CanFitRefund(Inventory inv, List<KeyValuePair<ItemDrop, int>> refund, int freedSlots)
        {
            if (inv == null || refund == null || refund.Count == 0)
                return true;

            int slotsNeeded = 0;
            for (int i = 0; i < refund.Count; i++)
            {
                ItemDrop drop = refund[i].Key;
                int amount = refund[i].Value;
                if (amount <= 0)
                    continue;
                if (drop?.m_itemData?.m_shared == null)
                    return false;

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                int max = Mathf.Max(1, shared.m_maxStackSize);
                int rest = amount - inv.FindFreeStackSpace(shared.m_name, Game.m_worldLevel);
                if (rest > 0)
                    slotsNeeded += (rest + max - 1) / max;
            }

            return slotsNeeded <= inv.GetEmptySlots() + freedSlots;
        }

        private static int FreedSlots(ItemDrop.ItemData item, Recipe recipe)
        {
            return item != null && recipe != null && Mathf.Max(1, recipe.m_amount) >= item.m_stack ? 1 : 0;
        }
    }
}
