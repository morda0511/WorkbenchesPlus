using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Upgrade tab: vanilla only describes the item at the NEW quality. Append the current values
    /// to every changed stat line, e.g. "Durability: 400 (now: 200)".
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    internal static class UpgradeCompare
    {
        private static readonly FieldInfo SelectedRecipeField =
            AccessTools.Field(typeof(InventoryGui), "m_selectedRecipe");

        private static readonly Regex Tags = new Regex("<[^>]*>", RegexOptions.Compiled);

        // UpdateRecipe runs every frame; only rebuild the text when the vanilla text changed.
        private static string _lastBase;
        private static string _lastFinal;

        private static void Postfix(InventoryGui __instance)
        {
            try
            {
                if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value
                    || !Plugin.Settings.EnableUpgradeCompare.Value)
                    return;
                if (DismantleMode.Active || __instance == null || __instance.m_recipeDecription == null)
                    return;

                Player player = Player.m_localPlayer;
                CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
                if (station == null || StationFilter.IsUpgrader(station))
                    return;

                Recipe recipe;
                ItemDrop.ItemData item;
                if (!TryGetSelected(__instance, out recipe, out item))
                    return;
                if (item == null || recipe?.m_item?.m_itemData == null)
                    return;
                if (item.m_quality >= item.m_shared.m_maxQuality)
                    return;

                string vanilla = __instance.m_recipeDecription.text;
                if (string.IsNullOrEmpty(vanilla))
                    return;

                if (vanilla == _lastFinal)
                    return;
                if (vanilla != _lastBase)
                {
                    _lastBase = vanilla;
                    _lastFinal = Build(vanilla, recipe, item);
                }
                if (_lastFinal != null && _lastFinal != vanilla)
                    __instance.m_recipeDecription.text = _lastFinal;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Settings != null && Plugin.Settings.DebugLogging.Value)
                    Plugin.Log.LogWarning("UpgradeCompare: " + ex.Message);
            }
        }

        private static string Build(string vanilla, Recipe recipe, ItemDrop.ItemData item)
        {
            string now = Localization.instance.Localize(
                ItemDrop.ItemData.GetTooltip(
                    recipe.m_item.m_itemData, item.m_quality, true, Game.m_worldLevel, 1));

            var current = new Dictionary<string, string>();
            foreach (string line in now.Split('\n'))
            {
                string key, value;
                if (SplitLine(line, out key, out value) && !current.ContainsKey(key))
                    current[key] = value;
            }

            string label = Loc.T("now");
            string[] lines = vanilla.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string key, value, old;
                if (!SplitLine(lines[i], out key, out value))
                    continue;
                if (!current.TryGetValue(key, out old) || old == value)
                    continue;
                lines[i] = lines[i] + "  <color=#a0a0a0>(" + label + ": " + old + ")</color>";
            }
            return string.Join("\n", lines);
        }

        /// <summary>"Damage: &lt;color&gt;50&lt;/color&gt;" -> key "Damage", value "50" (tags stripped).</summary>
        private static bool SplitLine(string line, out string key, out string value)
        {
            key = value = null;
            string plain = Tags.Replace(line, "");
            int colon = plain.IndexOf(':');
            if (colon <= 0 || colon >= plain.Length - 1)
                return false;
            key = plain.Substring(0, colon).Trim();
            value = plain.Substring(colon + 1).Trim();
            return key.Length > 0 && value.Length > 0;
        }

        private static bool TryGetSelected(InventoryGui gui, out Recipe recipe, out ItemDrop.ItemData item)
        {
            recipe = null;
            item = null;
            if (SelectedRecipeField == null)
                return false;
            object pair = SelectedRecipeField.GetValue(gui);
            if (pair == null)
                return false;
            System.Type t = pair.GetType();
            recipe = AccessTools.Property(t, "Recipe")?.GetValue(pair, null) as Recipe;
            item = AccessTools.Property(t, "ItemData")?.GetValue(pair, null) as ItemDrop.ItemData;
            return recipe != null;
        }
    }
}
