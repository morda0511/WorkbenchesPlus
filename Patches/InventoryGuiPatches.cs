using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WorkbenchesPlus
{
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
    [HarmonyPriority(Priority.Last)]
    internal static class UpdateRecipeListSortPatch
    {
        private static void Prefix(List<Recipe> recipes)
        {
            try
            {
                if (recipes == null)
                    return;
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                    return;

                RecipeSort.Apply(recipes);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("UpdateRecipeList Prefix: " + ex.Message);
            }
        }

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                    return;

                RecipeSort.ReorderGui(__instance);
                CraftabilityIndicators.Apply(__instance);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("UpdateRecipeList Postfix: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel")]
    internal static class UpdateCraftingPanelCategoryPatch
    {
        private static CraftingStation _lastStation;

        /// <summary>
        /// Reset category before the recipe list is built. Never rebuild from Postfix
        /// (nested UpdateCraftingPanel could freeze the crafting UI).
        /// </summary>
        private static void Prefix()
        {
            try
            {
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                    return;
                if (!Plugin.Settings.RefreshOnStationChange.Value)
                    return;

                Player player = Player.m_localPlayer;
                CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
                if (station == _lastStation)
                    return;

                _lastStation = station;
                if (CategoryBar.Active != CraftCategory.All)
                    CategoryBar.SetActive(CraftCategory.All, rebuild: false);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("UpdateCraftingPanel Prefix: " + ex.Message);
            }
        }

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                    return;

                CategoryBar.Show(__instance);
                CraftMultiplierBar.Show(__instance);
                DismantleTab.Show(__instance);
                InventoryRefreshHook.EnsureBound();
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("UpdateCraftingPanel Postfix: " + ex.Message);
            }
        }
    }

    internal static class InventoryRefreshHook
    {
        private static readonly MethodInfo UpdateCraftingPanel =
            AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });
        private static readonly FieldInfo CraftTimerField =
            AccessTools.Field(typeof(InventoryGui), "m_craftTimer");
        private static readonly FieldInfo CraftRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_craftRecipe");

        private const float DebounceSeconds = 0.2f;

        private static Inventory _bound;
        private static bool _hooked;
        private static bool _refreshing;
        private static bool _pending;
        private static float _pendingAt;

        public static void EnsureBound()
        {
            if (Plugin.Settings == null || !Plugin.Settings.RefreshOnInventoryChange.Value)
                return;

            Player player = Player.m_localPlayer;
            if (player == null)
                return;
            Inventory inv = player.GetInventory();
            if (inv == null || inv == _bound)
                return;

            Detach();
            _bound = inv;
            inv.m_onChanged += OnInventoryChanged;
            _hooked = true;
        }

        public static void Detach()
        {
            if (_hooked && _bound != null)
                _bound.m_onChanged -= OnInventoryChanged;
            _bound = null;
            _hooked = false;
            _pending = false;
        }

        /// <summary>
        /// Coalesce inventory churn (multi-craft adds items one-by-one) into one panel rebuild
        /// after crafting finishes / inventory goes quiet.
        /// </summary>
        public static void Tick(InventoryGui gui)
        {
            if (!_pending || gui == null)
                return;
            if (_refreshing || AccessToolsExt.IsRebuilding)
                return;
            if (IsCraftInProgress(gui))
            {
                // Keep deferring until the craft batch is done.
                _pendingAt = UnityEngine.Time.unscaledTime + DebounceSeconds;
                return;
            }
            if (UnityEngine.Time.unscaledTime < _pendingAt)
                return;

            _pending = false;
            DoRefresh(gui);
        }

        private static void OnInventoryChanged()
        {
            if (_refreshing || AccessToolsExt.IsRebuilding)
                return;
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;
            if (!Plugin.Settings.RefreshOnInventoryChange.Value)
                return;

            InventoryGui gui = InventoryGui.instance;
            if (gui == null || !gui.isActiveAndEnabled)
                return;

            // Never rebuild mid multi-craft — schedule one refresh after it settles.
            _pending = true;
            _pendingAt = UnityEngine.Time.unscaledTime + DebounceSeconds;
        }

        private static bool IsCraftInProgress(InventoryGui gui)
        {
            try
            {
                if (CraftRecipeField != null && CraftRecipeField.GetValue(gui) != null)
                    return true;
                if (CraftTimerField != null)
                {
                    object t = CraftTimerField.GetValue(gui);
                    if (t is float f && f > 0.01f)
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private static void DoRefresh(InventoryGui gui)
        {
            if (UpdateCraftingPanel == null || gui == null)
                return;

            _refreshing = true;
            try
            {
                UpdateCraftingPanel.Invoke(gui, new object[] { false });
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Inventory refresh: " + ex.Message);
            }
            finally
            {
                _refreshing = false;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "Update")]
    internal static class InventoryGuiUpdateRefreshPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                InventoryRefreshHook.Tick(__instance);
                if (DismantleMode.Active)
                    DismantleTab.RefreshVisuals(__instance);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class InventoryGuiShowBindPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                InventoryRefreshHook.EnsureBound();
                CategoryBar.Show(__instance);
                CraftMultiplierBar.Show(__instance);
                DismantleTab.Show(__instance);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("InventoryGui.Show: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class UpdateRecipeMultiplierPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            try
            {
                CraftMultiplierBar.SyncBeforeUpdateRecipe(__instance);
            }
            catch
            {
            }
        }

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                CraftMultiplierBar.SyncAfterUpdateRecipe(__instance);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class InventoryGuiHideUnbindPatch
    {
        private static void Prefix()
        {
            try
            {
                InventoryRefreshHook.Detach();
                CategoryBar.Hide();
                CraftMultiplierBar.Hide();
                DismantleTab.Hide();
                MaterialSectionHeaders.Clear();
            }
            catch
            {
            }
        }
    }
}
