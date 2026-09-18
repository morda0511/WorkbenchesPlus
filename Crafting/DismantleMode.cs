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

            // Do NOT call OnTabCraftPressed — that marks Craft as selected
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

                recipes.Add(recipe);
                Items.Add(item);
            }
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

            recipe = FindRecipe(item, station);
            if (recipe == null || recipe.m_item == null)
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
                string na = ItemSortName(Items[a]);
                string nb = ItemSortName(Items[b]);
                int c = string.Compare(na, nb, System.StringComparison.OrdinalIgnoreCase);
                if (c != 0)
                    return c;
                return a.CompareTo(b);
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
        }

        private static string ItemSortName(ItemDrop.ItemData item)
        {
            try
            {
                if (item?.m_shared != null)
                    return item.m_shared.m_name ?? "";
            }
            catch
            {
            }
            return "";
        }

        public static Recipe FindRecipe(ItemDrop.ItemData item, CraftingStation station)
        {
            if (item?.m_shared == null || ObjectDB.instance == null)
                return null;

            // Prefer vanilla lookup — exact item → recipe, no shared-name collisions.
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

                int amount = CraftMaterialAmount(req, quality);
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

        public static void BindSelectedItem(InventoryGui gui, int index)
        {
            if (!Active || gui == null)
                return;
            if (index < 0 || index >= Items.Count)
                return;

            if (CraftUpgradeItemField != null)
                CraftUpgradeItemField.SetValue(gui, Items[index]);
        }

        public static ItemDrop.ItemData ResolveSelectedItem(InventoryGui gui)
        {
            if (gui == null)
                return null;

            int idx = -1;
            try
            {
                MethodInfo getIdx = AccessTools.Method(typeof(InventoryGui), "GetSelectedRecipeIndex",
                    new[] { typeof(bool) });
                if (getIdx != null)
                    idx = (int)getIdx.Invoke(gui, new object[] { false });
            }
            catch
            {
                idx = -1;
            }

            // Prefer the paired inventory item from our dismantle list.
            if (idx >= 0 && idx < Items.Count && Items[idx] != null)
                return Items[idx];

            ItemDrop.ItemData item = null;
            if (CraftUpgradeItemField != null)
                item = CraftUpgradeItemField.GetValue(gui) as ItemDrop.ItemData;

            return item;
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

            // Never dismantle the recipe template — only a live inventory stack.
            Inventory inv = player.GetInventory();
            item = ResolveLiveInventoryItem(inv, item);
            if (item == null)
                return false;

            CraftingStation station = player.GetCurrentCraftingStation();
            Recipe recipe;
            if (!TryGetDismantleRecipe(player, station, item, out recipe))
                return false;

            List<KeyValuePair<ItemDrop, int>> refund = BuildRefund(recipe, item.m_quality);
            if (!CanFitRefund(player.GetInventory(), refund))
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

            // Inventory may have rebuilt during the craft timer — resolve a live slot ref.
            ItemDrop.ItemData item = ResolveLiveInventoryItem(inv, snapshot);
            if (item == null || !CanDismantle(player, station, item))
            {
                player.Message(MessageHud.MessageType.Center, "Dismantle failed");
                return true;
            }

            int remove = Mathf.Max(1, recipe.m_amount);
            if (item.m_stack < remove)
            {
                player.Message(MessageHud.MessageType.Center, "Dismantle failed");
                return true;
            }

            List<KeyValuePair<ItemDrop, int>> refund = BuildRefund(recipe, quality);
            if (!CanFitRefund(inv, refund))
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

            // Confirm inventory actually changed for this target.
            if (removed)
            {
                ItemDrop.ItemData still = ResolveLiveInventoryItem(inv, snapshot);
                if (still != null && still.m_stack >= stackBefore)
                    removed = false;
            }

            if (!removed)
            {
                player.Message(MessageHud.MessageType.Center, "Dismantle failed");
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
            player.Message(MessageHud.MessageType.Center, "Dismantled");
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

        private static bool CanFitRefund(Inventory inv, List<KeyValuePair<ItemDrop, int>> refund)
        {
            if (inv == null || refund == null || refund.Count == 0)
                return true;
            if (inv.GetEmptySlots() > 0)
                return true;

            for (int i = 0; i < refund.Count; i++)
            {
                ItemDrop drop = refund[i].Key;
                if (drop?.m_itemData?.m_shared == null)
                    return false;
                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                if (inv.CountItems(shared.m_name) <= 0 && shared.m_maxStackSize <= 1)
                    return false;
            }
            return true;
        }
    }
}
