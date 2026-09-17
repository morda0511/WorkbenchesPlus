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
            if (recipes == null)
                return;
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;

            RecipeSort.Apply(recipes);
        }

        private static void Postfix(InventoryGui __instance)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;

            // Vanilla can ignore Prefix order — force row order after the list is built.
            RecipeSort.ReorderGui(__instance);
            CraftabilityIndicators.Apply(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel")]
    internal static class UpdateCraftingPanelCategoryPatch
    {
        private static CraftingStation _lastStation;

        private static void Postfix(InventoryGui __instance)
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;

            CategoryBar.Show(__instance);
            InventoryRefreshHook.EnsureBound();

            if (Plugin.Settings.RefreshOnStationChange.Value)
            {
                Player player = Player.m_localPlayer;
                CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
                if (station != _lastStation)
                {
                    _lastStation = station;
                    if (CategoryBar.Active != CraftCategory.All)
                        CategoryBar.SetActive(CraftCategory.All);
                }
            }
        }
    }

    internal static class InventoryRefreshHook
    {
        private static readonly MethodInfo UpdateCraftingPanel =
            AccessTools.Method(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });

        private static Inventory _bound;
        private static bool _hooked;

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
        }

        private static void OnInventoryChanged()
        {
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;
            if (!Plugin.Settings.RefreshOnInventoryChange.Value)
                return;

            InventoryGui gui = InventoryGui.instance;
            if (gui == null || !gui.isActiveAndEnabled)
                return;
            if (UpdateCraftingPanel != null)
                UpdateCraftingPanel.Invoke(gui, new object[] { false });
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    internal static class InventoryGuiShowBindPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            InventoryRefreshHook.EnsureBound();
            CategoryBar.Show(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    internal static class InventoryGuiHideUnbindPatch
    {
        private static void Prefix()
        {
            InventoryRefreshHook.Detach();
            CategoryBar.Hide();
        }
    }
}
