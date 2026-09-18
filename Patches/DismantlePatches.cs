using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkbenchesPlus
{
    [HarmonyPatch(typeof(InventoryGui), "InCraftTab")]
    internal static class DismantleInCraftTabPatch
    {
        // Dismantle keeps Craft clickable (interactable=true), which would make
        // InCraftTab false and push UpdateRecipeList down the upgrade path —
        // that desyncs rows from inventory items and breaks RemoveItem.
        private static void Postfix(ref bool __result)
        {
            if (DismantleMode.Active)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "InUpradeTab")]
    internal static class DismantleInUpgradeTabPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (DismantleMode.Active)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "AddRecipeToList")]
    internal static class DismantleAddRecipeToListPatch
    {
        private static int _injectIndex;

        public static void ResetInject()
        {
            _injectIndex = 0;
        }

        // Attach the real inventory ItemData so selection/removal stay paired.
        private static void Prefix(ref ItemDrop.ItemData item, ref bool canCraft)
        {
            if (!DismantleMode.Active)
                return;

            canCraft = true;
            if (_injectIndex >= 0 && _injectIndex < DismantleMode.Items.Count)
            {
                item = DismantleMode.Items[_injectIndex];
                _injectIndex++;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
    [HarmonyPriority(Priority.First)]
    internal static class DismantleRecipeListPatch
    {
        private static void Prefix(List<Recipe> recipes)
        {
            try
            {
                DismantleAddRecipeToListPatch.ResetInject();
                if (!DismantleMode.Active)
                    return;
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                    || !Plugin.Settings.EnableDismantle.Value)
                    return;
                DismantleMode.BuildList(recipes);
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Dismantle list: " + ex.Message);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement")]
    internal static class DismantleSetupRequirementPatch
    {
        private static void Postfix(Transform elementRoot)
        {
            try
            {
                if (!DismantleMode.Active || elementRoot == null)
                    return;

                Transform amount = elementRoot.Find("res_amount");
                if (amount == null)
                    return;
                TMP_Text text = amount.GetComponent<TMP_Text>();
                if (text != null)
                    text.color = Color.white;

                Transform icon = elementRoot.Find("res_icon") ?? elementRoot.Find("icon");
                if (icon != null)
                {
                    Image img = icon.GetComponent<Image>();
                    if (img != null)
                        img.color = Color.white;
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetRecipe")]
    internal static class DismantleSetRecipePatch
    {
        private static void Postfix(InventoryGui __instance, int index, bool center)
        {
            try
            {
                if (!DismantleMode.Active)
                    return;
                DismantleMode.BindSelectedItem(__instance, index);
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class DismantleUpdateRecipePatch
    {
        private static readonly FieldInfo CraftButtonField =
            AccessTools.Field(typeof(InventoryGui), "m_craftButton");
        private static readonly FieldInfo ItemCraftTypeField =
            AccessTools.Field(typeof(InventoryGui), "m_itemCraftType");

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (!DismantleMode.Active || __instance == null)
                    return;

                CraftMultiplierBar.ForceHideArrows();
                DismantleTab.RefreshVisuals(__instance);

                if (ItemCraftTypeField != null)
                {
                    TMP_Text typeLabel = ItemCraftTypeField.GetValue(__instance) as TMP_Text;
                    if (typeLabel != null)
                        typeLabel.text = "DISMANTLE";
                }

                // While the craft bar is running, leave vanilla progress UI alone.
                if (DismantleMode.HasProgress)
                    return;

                ItemDrop.ItemData item = DismantleMode.ResolveSelectedItem(__instance);
                Player player = Player.m_localPlayer;
                bool can = item != null && player != null
                    && DismantleMode.CanDismantle(player, player.GetCurrentCraftingStation(), item);

                if (CraftButtonField != null)
                {
                    Button btn = CraftButtonField.GetValue(__instance) as Button;
                    if (btn != null)
                    {
                        btn.interactable = can;
                        TMP_Text label = btn.GetComponentInChildren<TMP_Text>(true);
                        if (label != null)
                            label.text = "DISMANTLE";
                    }
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
    internal static class DismantleCraftPressedPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            try
            {
                if (!DismantleMode.Active)
                    return true;
                if (Plugin.Settings == null || !Plugin.Settings.EnableDismantle.Value)
                    return true;

                // Start craft timer + sound; complete in DoCrafting.
                DismantleMode.TryBeginProgress(__instance);
                return false;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Dismantle begin: " + ex.Message);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    internal static class DismantleDoCraftingPatch
    {
        private static bool Prefix(InventoryGui __instance)
        {
            try
            {
                if (!DismantleMode.HasProgress)
                    return true;

                bool handled = DismantleMode.TryCompleteProgress(__instance);
                if (handled)
                    AccessToolsExt.RebuildCraftingPanel();
                return false;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Log != null)
                    Plugin.Log.LogWarning("Dismantle finish: " + ex.Message);
                return false;
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "SetupRequirementList")]
    internal static class DismantleRequirementListPatch
    {
        private static readonly FieldInfo ReqListField =
            AccessTools.Field(typeof(InventoryGui), "m_reqList");

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (!DismantleMode.Active || ReqListField == null)
                    return;

                var list = ReqListField.GetValue(__instance) as System.Collections.IList;
                if (list == null || list.Count == 0)
                    return;

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    Piece.Requirement req = list[i] as Piece.Requirement;
                    if (req == null)
                        continue;
                    if (req.m_upgraderResource)
                        list.RemoveAt(i);
                    else if (req.m_resItem != null
                        && req.m_resItem.name != null
                        && req.m_resItem.name.StartsWith("Upgrader", System.StringComparison.OrdinalIgnoreCase))
                        list.RemoveAt(i);
                }
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnCraftCancelPressed")]
    internal static class DismantleCancelPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            if (DismantleMode.HasProgress)
                DismantleMode.CancelProgress(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
    internal static class DismantleTabCraftPatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            if (DismantleMode.HasProgress)
                DismantleMode.CancelProgress(__instance);
            DismantleMode.ClearIfNotSuppressed();
        }

        private static void Postfix(InventoryGui __instance)
        {
            DismantleTab.RefreshVisuals(__instance);
            if (!DismantleMode.Active)
                CraftMultiplierBar.Show(__instance);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnTabUpgradePressed")]
    internal static class DismantleTabUpgradePatch
    {
        private static void Prefix(InventoryGui __instance)
        {
            if (DismantleMode.HasProgress)
                DismantleMode.CancelProgress(__instance);
            DismantleMode.ClearIfNotSuppressed();
        }

        private static void Postfix(InventoryGui __instance)
        {
            DismantleTab.RefreshVisuals(__instance);
            CraftMultiplierBar.ForceHideArrows();
        }
    }
}
