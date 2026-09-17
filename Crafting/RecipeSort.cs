using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    internal static class RecipeSort
    {
        private struct Entry
        {
            public Recipe Recipe;
            public int OriginalIndex;
            public int CraftBucket;
            public int MaterialPct;
            public int StationLevel;
            public int CategoryOrder;
            public string SetKey;
            public int PieceOrder;
            public string SortName;
        }

        private static readonly FieldInfo AvailableRecipesField =
            AccessTools.Field(typeof(InventoryGui), "m_availableRecipes");
        private static readonly FieldInfo RecipeListSpaceField =
            AccessTools.Field(typeof(InventoryGui), "m_recipeListSpace");

        public static void Apply(List<Recipe> recipes)
        {
            if (recipes == null)
                return;
            if (Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;

            // "All" = everything for the current station only (never forge+cauldron+workbench mixed).
            StationFilter.Apply(recipes);

            if (Plugin.Settings.EnableCategories.Value)
                CategoryBar.SyncAvailable(recipes);

            if (recipes.Count == 0)
                return;

            if (Plugin.Settings.EnableCategories.Value && CategoryBar.Active != CraftCategory.All)
            {
                for (int i = recipes.Count - 1; i >= 0; i--)
                {
                    if (!RecipeCategories.Matches(recipes[i], CategoryBar.Active))
                        recipes.RemoveAt(i);
                }
            }

            // Prefix order is only a hint — vanilla re-sorts RecipeDataPair and lays out by Y.
            SortInPlace(recipes);
        }

        /// <summary>
        /// Valheim ends UpdateRecipeList with List.Sort(RecipeDataPair) + anchoredPosition layout.
        /// We must re-sort that list AND rewrite Y positions, or nothing visible changes.
        /// </summary>
        public static void ReorderGui(InventoryGui gui)
        {
            if (gui == null || Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
                return;
            if (Plugin.Settings.IsVanillaOrder())
                return;
            if (AvailableRecipesField == null)
                return;

            IList pairs = AvailableRecipesField.GetValue(gui) as IList;
            if (pairs == null || pairs.Count <= 1)
                return;

            var recipes = new List<Recipe>(pairs.Count);
            var pairByRecipe = new List<object>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                object pair = pairs[i];
                Recipe r = ReadRecipe(pair);
                recipes.Add(r);
                pairByRecipe.Add(pair);
            }

            var order = new List<int>(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
                order.Add(i);

            var entries = BuildEntries(recipes);
            bool groupArmor = Plugin.Settings.EnableArmorSetGrouping.Value
                || Plugin.Settings.EnableWeaponSetGrouping.Value;
            bool craftBuckets = Plugin.Settings.UseCraftBuckets();
            bool alphabetical = Plugin.Settings.IsAlphabetical();
            bool progression = Plugin.Settings.IsProgression();
            bool categoryThen = Plugin.Settings.IsCategoryThenCraftable();

            order.Sort((ia, ib) => Compare(
                entries[ia], entries[ib],
                groupArmor, craftBuckets, alphabetical, progression, categoryThen));

            float space = 30f;
            if (RecipeListSpaceField != null)
            {
                object sp = RecipeListSpaceField.GetValue(gui);
                if (sp is float f && f > 0.01f)
                    space = f;
            }

            // Rebuild list order, then apply the same Y layout vanilla uses.
            pairs.Clear();
            for (int i = 0; i < order.Count; i++)
            {
                object pair = pairByRecipe[order[i]];
                pairs.Add(pair);
                GameObject go = ReadElement(pair);
                if (go == null)
                    continue;
                RectTransform rt = go.transform as RectTransform;
                if (rt == null)
                    continue;
                rt.anchoredPosition = new Vector2(0f, -i * space);
                rt.SetSiblingIndex(i);
            }

            if (Plugin.Settings.DebugLogging.Value)
            {
                Plugin.Log.LogInfo("ReorderGui rows=" + pairs.Count + " space=" + space);
                int n = System.Math.Min(10, order.Count);
                for (int i = 0; i < n; i++)
                {
                    Entry e = entries[order[i]];
                    Plugin.Log.LogInfo("  #" + i + " set=" + (e.SetKey ?? "-")
                        + " tier=" + MaterialProgression.Tier(e.SetKey)
                        + " bucket=" + e.CraftBucket
                        + " piece=" + e.PieceOrder
                        + " name=" + e.SortName);
                }
            }
        }

        private static void SortInPlace(List<Recipe> recipes)
        {
            if (recipes.Count <= 1)
                return;
            if (Plugin.Settings.IsVanillaOrder())
                return;

            var entries = BuildEntries(recipes);
            bool groupArmor = Plugin.Settings.EnableArmorSetGrouping.Value
                || Plugin.Settings.EnableWeaponSetGrouping.Value;
            bool craftBuckets = Plugin.Settings.UseCraftBuckets();
            bool alphabetical = Plugin.Settings.IsAlphabetical();
            bool progression = Plugin.Settings.IsProgression();
            bool categoryThen = Plugin.Settings.IsCategoryThenCraftable();

            entries.Sort((a, b) => Compare(
                a, b, groupArmor, craftBuckets, alphabetical, progression, categoryThen));

            recipes.Clear();
            for (int i = 0; i < entries.Count; i++)
                recipes.Add(entries[i].Recipe);
        }

        private static List<Entry> BuildEntries(List<Recipe> recipes)
        {
            Player player = Player.m_localPlayer;
            string[] armorOrder = ArmorSetDetector.ParseOrder(Plugin.Settings.ArmorPieceOrder.Value);
            string[] weaponOrder = WeaponSetDetector.ParseOrder(Plugin.Settings.WeaponPieceOrder.Value);
            bool groupArmor = Plugin.Settings.EnableArmorSetGrouping.Value;
            bool groupWeapons = Plugin.Settings.EnableWeaponSetGrouping.Value;
            bool groupModdedArmor = Plugin.Settings.GroupModdedArmorSets.Value;
            bool groupModdedWeapons = Plugin.Settings.GroupModdedWeaponSets.Value;
            bool craftBuckets = Plugin.Settings.UseCraftBuckets();
            bool categoryThen = Plugin.Settings.IsCategoryThenCraftable();

            var entries = new List<Entry>(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe r = recipes[i];
                string setKey = null;
                int pieceOrder = 0;

                if (groupArmor)
                {
                    setKey = ArmorSetDetector.SetKey(r, groupModdedArmor);
                    if (!string.IsNullOrEmpty(setKey))
                        pieceOrder = ArmorSetDetector.PieceOrder(r, armorOrder);
                }

                if (string.IsNullOrEmpty(setKey) && groupWeapons)
                {
                    setKey = WeaponSetDetector.SetKey(r, groupModdedWeapons);
                    if (!string.IsNullOrEmpty(setKey))
                        pieceOrder = WeaponSetDetector.PieceOrder(r, weaponOrder);
                }

                entries.Add(new Entry
                {
                    Recipe = r,
                    OriginalIndex = i,
                    CraftBucket = craftBuckets ? Craftability.ScoreBucket(player, r) : 0,
                    MaterialPct = craftBuckets ? Craftability.MaterialPercent(player, r) : 0,
                    StationLevel = r != null ? r.m_minStationLevel : 0,
                    CategoryOrder = categoryThen ? (int)RecipeCategories.Classify(r) : 0,
                    SetKey = setKey,
                    PieceOrder = pieceOrder,
                    SortName = DisplayName(r)
                });
            }
            return entries;
        }

        private static int Compare(
            Entry a,
            Entry b,
            bool groupArmor,
            bool craftBuckets,
            bool alphabetical,
            bool progression,
            bool categoryThen)
        {
            if (categoryThen)
            {
                int c = a.CategoryOrder.CompareTo(b.CategoryOrder);
                if (c != 0)
                    return c;
            }

            // Per-item craftability first — never pull uncraftable set mates into the top.
            if (craftBuckets)
            {
                int c = a.CraftBucket.CompareTo(b.CraftBucket);
                if (c != 0)
                    return c;
            }

            bool aSet = groupArmor && !string.IsNullOrEmpty(a.SetKey);
            bool bSet = groupArmor && !string.IsNullOrEmpty(b.SetKey);

            // Within the same craft tier: keep set pieces together, ordered by biome/material.
            if (aSet && bSet)
            {
                if (string.Equals(a.SetKey, b.SetKey, System.StringComparison.OrdinalIgnoreCase))
                {
                    int po = a.PieceOrder.CompareTo(b.PieceOrder);
                    if (po != 0)
                        return po;
                    return string.Compare(a.SortName, b.SortName, System.StringComparison.OrdinalIgnoreCase);
                }

                int ta = MaterialProgression.Tier(a.SetKey);
                int tb = MaterialProgression.Tier(b.SetKey);
                int tierCmp = ta.CompareTo(tb);
                if (tierCmp != 0)
                    return tierCmp;

                int keyCmp = string.Compare(a.SetKey, b.SetKey, System.StringComparison.OrdinalIgnoreCase);
                if (keyCmp != 0)
                    return keyCmp;
            }
            else if (groupArmor && (aSet || bSet))
            {
                // Classified sets before leftovers inside this craft tier.
                if (aSet)
                    return -1;
                return 1;
            }

            if (craftBuckets)
            {
                int c = b.MaterialPct.CompareTo(a.MaterialPct);
                if (c != 0)
                    return c;
            }

            if (progression)
            {
                int c = a.StationLevel.CompareTo(b.StationLevel);
                if (c != 0)
                    return c;
            }

            int nameCmp = string.Compare(a.SortName, b.SortName, System.StringComparison.OrdinalIgnoreCase);
            if (nameCmp != 0)
                return nameCmp;

            return a.OriginalIndex.CompareTo(b.OriginalIndex);
        }

        private static string DisplayName(Recipe recipe)
        {
            try
            {
                if (recipe?.m_item?.m_itemData?.m_shared != null)
                    return recipe.m_item.m_itemData.m_shared.m_name ?? "";
            }
            catch
            {
            }
            return recipe?.m_item != null ? recipe.m_item.name : "";
        }

        private static PropertyInfo _recipeProp;
        private static PropertyInfo _elementProp;
        private static bool _propsResolved;

        private static void EnsurePairProps(object sample)
        {
            if (_propsResolved || sample == null)
                return;
            System.Type t = sample.GetType();
            _recipeProp = AccessTools.Property(t, "Recipe");
            _elementProp = AccessTools.Property(t, "InterfaceElement");
            _propsResolved = true;
        }

        private static Recipe ReadRecipe(object pair)
        {
            if (pair == null)
                return null;
            EnsurePairProps(pair);
            if (_recipeProp != null)
                return _recipeProp.GetValue(pair, null) as Recipe;
            FieldInfo f = AccessTools.Field(pair.GetType(), "<Recipe>k__BackingField");
            return f != null ? f.GetValue(pair) as Recipe : null;
        }

        private static GameObject ReadElement(object pair)
        {
            if (pair == null)
                return null;
            EnsurePairProps(pair);
            object v = null;
            if (_elementProp != null)
                v = _elementProp.GetValue(pair, null);
            else
            {
                FieldInfo f = AccessTools.Field(pair.GetType(), "<InterfaceElement>k__BackingField");
                if (f != null)
                    v = f.GetValue(pair);
            }
            if (v is GameObject go)
                return go;
            if (v is Component c)
                return c.gameObject;
            return null;
        }
    }
}
