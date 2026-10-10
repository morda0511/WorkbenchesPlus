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
        // InCraftTab false and push UpdateRecipeList down the upgrade path - 
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

        public static void ResetInject()
        {
            DismantleMode.BeginRows();
        }

        // Attach the real inventory ItemData so selection/removal stay paired.
        private static void Prefix(Recipe recipe, ref ItemDrop.ItemData item, ref bool canCraft)
        {
            if (!DismantleMode.Active)
                return;

            canCraft = true;
            item = DismantleMode.ClaimItemForRow(recipe, item);
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

        /// <summary>Refund list in the requirement slots, no station-level badge, stats of the item as it is.</summary>
        private static void ShowDismantleInfo(InventoryGui gui, Player player, ItemDrop.ItemData item)
        {
            if (gui.m_minStationLevelIcon != null)
                gui.m_minStationLevelIcon.gameObject.SetActive(false);

            GameObject[] slots = gui.m_recipeRequirementList;
            if (slots == null)
                return;

            List<KeyValuePair<ItemDrop, int>> refund = item != null && player != null
                ? DismantleMode.RefundFor(player, player.GetCurrentCraftingStation(), item)
                : new List<KeyValuePair<ItemDrop, int>>();

            int shown = 0;
            for (int i = 0; i < refund.Count && shown < slots.Length; i++)
            {
                ItemDrop drop = refund[i].Key;
                if (drop == null || drop.m_itemData == null || refund[i].Value <= 0 || slots[shown] == null)
                    continue;
                ShowSlot(slots[shown].transform, drop.m_itemData, refund[i].Value);
                shown++;
            }
            for (int i = shown; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    InventoryGui.HideRequirement(slots[i].transform);
            }

            if (item != null && gui.m_recipeDecription != null)
            {
                try
                {
                    gui.m_recipeDecription.text = Localization.instance.Localize(
                        ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, item.m_worldLevel));
                }
                catch
                {
                }
            }
        }

        private static void ShowSlot(Transform root, ItemDrop.ItemData data, int amount)
        {
            Transform iconT = root.Find("res_icon"), nameT = root.Find("res_name"), amountT = root.Find("res_amount");
            if (iconT == null || nameT == null || amountT == null)
                return;
            Image icon = iconT.GetComponent<Image>();
            TMP_Text name = nameT.GetComponent<TMP_Text>();
            TMP_Text count = amountT.GetComponent<TMP_Text>();
            iconT.gameObject.SetActive(true);
            nameT.gameObject.SetActive(true);
            amountT.gameObject.SetActive(true);
            string label = Localization.instance.Localize(data.m_shared.m_name);
            if (icon != null)
            {
                icon.sprite = data.GetIcon();
                icon.color = Color.white;
            }
            if (name != null)
                name.text = label;
            if (count != null)
            {
                count.text = amount.ToString();
                count.color = Color.white;
            }
            UITooltip tip = root.GetComponent<UITooltip>();
            if (tip != null)
                tip.m_text = label;
        }

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
                        typeLabel.text = Loc.T("dismantle");
                }

                ItemDrop.ItemData item = DismantleMode.ResolveSelectedItem(__instance);
                Player player = Player.m_localPlayer;

                // Vanilla filled this panel for "upgrade to quality+1": costs of the next level, the
                // station level that needs, and the stats of the next level. Show the dismantle view instead.
                ShowDismantleInfo(__instance, player, item);

                // While the craft bar is running, leave vanilla progress UI alone.
                if (DismantleMode.HasProgress)
                    return;

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
                            label.text = Loc.T("dismantle");
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
