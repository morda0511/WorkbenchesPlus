using System;
using System.Reflection;
using HarmonyLib;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Startup self-test: the mod reaches into private InventoryGui / CraftingStation members.
    /// After a Valheim update a renamed member would otherwise fail silently. Log it instead.
    /// </summary>
    internal static class ReflectionCheck
    {
        private static readonly string[] InventoryGuiFields =
        {
            "m_availableRecipes", "m_recipeListSpace", "m_recipeListRoot", "m_crafting",
            "m_craftTimer", "m_craftRecipe", "m_craftUpgradeItem", "m_selectedRecipe",
            "m_craftButton", "m_repairButton", "m_itemCraftType", "m_reqList",
            "m_tabCraft", "m_tabUpgrade", "m_qualityLevelUp", "m_qualityLevelDown",
            "m_multiCraftAmount", "m_touchMultiCrafting",
            "m_craftItemEffects", "m_craftItemDoneEffects", "CraftingVibration",
        };

        private static readonly string[] CraftingStationFields =
        {
            "m_buildRange", "m_updateExtensionTimer", "m_areaMarkerCircle",
        };

        public static void Run()
        {
            int missing = 0;
            missing += CheckFields(typeof(InventoryGui), InventoryGuiFields);
            missing += CheckFields(typeof(CraftingStation), CraftingStationFields);
            missing += CheckMethod(typeof(InventoryGui), "UpdateCraftingPanel", new[] { typeof(bool) });
            missing += CheckMethod(typeof(InventoryGui), "GetSelectedRecipeIndex", new[] { typeof(bool) });

            if (missing > 0)
                Plugin.Log.LogWarning(missing + " reflected Valheim member(s) not found - "
                    + "the game was probably updated. Some Workbenches+ features may not work.");
        }

        private static int CheckFields(Type type, string[] names)
        {
            int missing = 0;
            foreach (string name in names)
            {
                if (AccessTools.Field(type, name) != null)
                    continue;
                Plugin.Log.LogWarning("Missing field " + type.Name + "." + name);
                missing++;
            }
            return missing;
        }

        private static int CheckMethod(Type type, string name, Type[] args)
        {
            if (AccessTools.Method(type, name, args) != null)
                return 0;
            Plugin.Log.LogWarning("Missing method " + type.Name + "." + name);
            return 1;
        }
    }
}
