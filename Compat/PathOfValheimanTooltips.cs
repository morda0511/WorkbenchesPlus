using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// With Path of Valheiman installed: hovering a recipe row shows the full item tooltip, the same one
    /// its own category grid shows (rarity, stat ranges like "+(1-3) food slots", tags). Path of Valheiman
    /// keeps those stats out of the plain recipe description, so without this they are only visible in
    /// its grid. Built exactly like the grid does it: a preview copy of the item at quality 1, marked with
    /// Implicits.MarkPreview, through InventoryGrid.CreateItemTooltip.
    /// </summary>
    internal static class PathOfValheimanTooltips
    {
        private static readonly FieldInfo AvailableRecipesField =
            AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
        private static readonly MethodInfo CreateItemTooltip =
            AccessTools.Method(typeof(InventoryGrid), "CreateItemTooltip");

        private static bool _bound;
        private static MethodInfo _markPreview;   // PassiveTree.Implicits.MarkPreview(ItemData)
        private static bool _failed;

        public static void Apply(InventoryGui gui)
        {
            if (_failed || gui == null || !PathOfValheimanCompat.Loaded || PathOfValheimanCompat.OwnsList(gui))
                return;
            if (AvailableRecipesField == null || CreateItemTooltip == null || gui.m_playerGrid == null)
                return;
            Bind();

            IList rows = AvailableRecipesField.GetValue(gui) as IList;
            if (rows == null || rows.Count == 0)
                return;

            GameObject tooltipPrefab = ItemTooltipPrefab(gui);
            if (tooltipPrefab == null)
                return;

            try
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    object pair = rows[i];
                    Recipe recipe = Read<Recipe>(pair, "Recipe");
                    GameObject row = Read<GameObject>(pair, "InterfaceElement");
                    if (recipe == null || recipe.m_item == null || row == null)
                        continue;

                    // Upgrade / dismantle rows stand for a real item: show that one. Craft rows: a preview.
                    ItemDrop.ItemData item = Read<ItemDrop.ItemData>(pair, "ItemData") ?? Preview(recipe);
                    if (item == null)
                        continue;

                    UITooltip tip = row.GetComponent<UITooltip>();
                    if (tip == null)
                        tip = row.AddComponent<UITooltip>();
                    if (tip.m_tooltipPrefab == null)
                        tip.m_tooltipPrefab = tooltipPrefab;
                    CreateItemTooltip.Invoke(gui.m_playerGrid, new object[] { item, tip });
                }
            }
            catch (System.Exception ex)
            {
                _failed = true;
                Plugin.Log.LogWarning("Path of Valheiman tooltips disabled: " + ex.Message);
            }
        }

        private static ItemDrop.ItemData Preview(Recipe recipe)
        {
            ItemDrop.ItemData item = recipe.m_item.m_itemData.Clone();
            item.m_quality = 1;
            item.m_durability = item.GetMaxDurability();
            item.m_dropPrefab = recipe.m_item.gameObject;
            if (_markPreview != null)
                _markPreview.Invoke(null, new object[] { item });
            return item;
        }

        /// <summary>The tooltip window inventory slots use (item tooltip, not the small text one).</summary>
        private static GameObject ItemTooltipPrefab(InventoryGui gui)
        {
            GameObject element = gui.m_playerGrid.m_elementPrefab;
            UITooltip t = element != null ? element.GetComponentInChildren<UITooltip>(true) : null;
            return t != null ? t.m_tooltipPrefab : null;
        }

        private static T Read<T>(object pair, string property) where T : class
        {
            PropertyInfo p = pair?.GetType().GetProperty(property);
            return p != null ? p.GetValue(pair, null) as T : null;
        }

        private static void Bind()
        {
            if (_bound)
                return;
            _bound = true;
            BepInEx.PluginInfo info;
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("treextr.pathofvalheiman", out info) || info?.Instance == null)
                return;
            System.Type t = info.Instance.GetType().Assembly.GetType("PassiveTree.Implicits");
            _markPreview = t != null
                ? t.GetMethod("MarkPreview", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                : null;
        }
    }
}
