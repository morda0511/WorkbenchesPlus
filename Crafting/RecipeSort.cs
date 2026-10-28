using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Glue between the vanilla recipe list and the sorter: station filter, category chip filter,
    /// then one ordering (RecipeOrder) for the list and for the rows on screen.
    /// </summary>
    internal static class RecipeSort
    {
        private struct Entry
        {
            public Recipe Recipe;
            public RecipeInfo Info;
            public int CraftTier;
            public bool Fav;
            public int OriginalIndex;
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

            Player player = Player.m_localPlayer;
            if (player != null && StationFilter.IsUpgrader(player.GetCurrentCraftingStation()))
            {
                if (CategoryBar.Active != CraftCategory.All)
                    CategoryBar.SetActive(CraftCategory.All, rebuild: false);
                return;
            }

            // Dismantle already built a station-filtered inventory list - do not strip it again
            // (and keep DismantleMode.Items indices aligned with recipes).
            if (!DismantleMode.Active)
            {
                // Fail open: a station filter that empties the whole list is wrong (modded stations),
                // so keep the vanilla list in that case and say so once.
                var vanillaList = new List<Recipe>(recipes);
                StationFilter.Apply(recipes);
                if (recipes.Count == 0 && vanillaList.Count > 0)
                {
                    recipes.AddRange(vanillaList);
                    WarnOnce(player != null ? player.GetCurrentCraftingStation() : null);
                }

                if (Plugin.Settings.EnableCategories.Value)
                    CategoryBar.SyncAvailable(recipes);

                if (recipes.Count == 0)
                    return;

                if (Plugin.Settings.EnableCategories.Value && CategoryBar.Active != CraftCategory.All)
                {
                    for (int i = recipes.Count - 1; i >= 0; i--)
                    {
                        if (SortEngine.CategoryOf(recipes[i]) != CategoryBar.Active)
                            recipes.RemoveAt(i);
                    }

                    if (CategoryBar.HasSubFilter)
                    {
                        for (int i = recipes.Count - 1; i >= 0; i--)
                        {
                            if (!CategoryBar.PassesSubFilter(SortEngine.Info(recipes[i])))
                                recipes.RemoveAt(i);
                        }
                    }
                }

                if (SearchBox.Query.Length > 0)
                {
                    for (int i = recipes.Count - 1; i >= 0; i--)
                    {
                        if (!SearchBox.Matches(SortEngine.Info(recipes[i])))
                            recipes.RemoveAt(i);
                    }
                }
            }

            if (recipes.Count == 0)
                return;

            if (DismantleMode.Active)
            {
                // Keep recipes and inventory items on the same indices.
                DismantleMode.SortPaired(recipes);
                return;
            }

            SortInPlace(recipes);
        }

        /// <summary>
        /// Valheim ends UpdateRecipeList with List.Sort(RecipeDataPair) + anchoredPosition layout.
        /// We must re-sort that list AND rewrite Y positions, or nothing visible changes.
        /// </summary>
        public static void ReorderGui(InventoryGui gui)
        {
            if (gui == null || Plugin.Settings == null || !Plugin.Settings.EnableMod.Value)
            {
                MaterialSectionHeaders.Clear();
                return;
            }
            // The Forge of Potential is sorted too: its recipe list stays untouched (vanilla expands it from
            // the inventory), only the finished rows are reordered here.
            // Dismantle list is already paired+sorted; do not reshuffle rows.
            if (DismantleMode.Active)
            {
                MaterialSectionHeaders.Clear();
                return;
            }
            if (AvailableRecipesField == null)
                return;

            IList pairs = AvailableRecipesField.GetValue(gui) as IList;
            if (pairs == null || pairs.Count == 0)
            {
                MaterialSectionHeaders.Clear();
                return;
            }

            var recipes = new List<Recipe>(pairs.Count);
            var elements = new List<GameObject>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                recipes.Add(ReadRecipe(pairs[i]));
                elements.Add(ReadElement(pairs[i]));
            }

            Player current = Player.m_localPlayer;
            bool upgrader = current != null && StationFilter.IsUpgrader(current.GetCurrentCraftingStation());
            List<Entry> entries = BuildEntries(recipes, upgrader ? pairs : null);
            entries.Sort(CompareEntries);

            float space = 30f;
            if (RecipeListSpaceField != null)
            {
                object sp = RecipeListSpaceField.GetValue(gui);
                if (sp is float f && f > 0.01f)
                    space = f;
            }

            var oldPairs = new List<object>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
                oldPairs.Add(pairs[i]);

            pairs.Clear();
            var orderedRecipes = new List<Recipe>(entries.Count);
            var orderedElements = new List<GameObject>(entries.Count);
            Transform listParent = null;

            for (int i = 0; i < entries.Count; i++)
            {
                int idx = entries[i].OriginalIndex;
                pairs.Add(oldPairs[idx]);
                orderedRecipes.Add(recipes[idx]);
                GameObject go = elements[idx];
                orderedElements.Add(go);
                if (listParent == null && go != null)
                    listParent = go.transform.parent;
            }

            var slots = new List<HeaderSlot>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
                slots.Add(SlotFor(entries[i]));

            MaterialSectionHeaders.Apply(listParent, slots, orderedElements, space);

            if (!upgrader)
            {
                for (int i = 0; i < orderedRecipes.Count; i++)
                {
                    FavoriteRowClick.Attach(orderedElements[i], orderedRecipes[i]);
                    AppendFoodValue(orderedElements[i], SortEngine.Info(orderedRecipes[i]));
                }
            }
        }

        private static readonly HashSet<string> Warned = new HashSet<string>();

        private static void WarnOnce(CraftingStation station)
        {
            string key = station != null ? station.name + "|" + station.m_name : "none";
            if (Warned.Add(key) && Plugin.Log != null)
                Plugin.Log.LogWarning("Station filter would hide every recipe at '" + key + "' - showing the vanilla list.");
        }

        private static void SortInPlace(List<Recipe> recipes)
        {
            if (recipes.Count <= 1)
                return;

            List<Entry> entries = BuildEntries(recipes, null);
            entries.Sort(CompareEntries);

            recipes.Clear();
            for (int i = 0; i < entries.Count; i++)
                recipes.Add(entries[i].Recipe);
        }

        /// <param name="upgradePairs">Forge of Potential rows: craftability comes from the row itself (it is per inventory item).</param>
        private static List<Entry> BuildEntries(List<Recipe> recipes, IList upgradePairs)
        {
            Player player = Player.m_localPlayer;
            var entries = new List<Entry>(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
            {
                Recipe r = recipes[i];
                entries.Add(new Entry
                {
                    Recipe = r,
                    Info = SortEngine.Info(r),
                    CraftTier = upgradePairs != null
                        ? (RowCanCraft(upgradePairs[i]) ? 0 : 1)
                        : Craftability.SectionTier(Craftability.ScoreBucket(player, r)),
                    Fav = upgradePairs == null && !DismantleMode.Active && FavoritesStore.Is(r),
                    OriginalIndex = i
                });
            }
            return entries;
        }

        private static int CompareEntries(Entry a, Entry b)
        {
            int c;
            if (a.Fav != b.Fav)
                return a.Fav ? -1 : 1;
            if (a.Info == null || b.Info == null)
                c = a.Info == null ? (b.Info == null ? 0 : 1) : -1;
            else
                c = RecipeOrder.Compare(a.Info, a.CraftTier, b.Info, b.CraftTier);
            return c != 0 ? c : a.OriginalIndex.CompareTo(b.OriginalIndex);
        }

        private static HeaderSlot SlotFor(Entry e)
        {
            if (e.Fav)
                return new HeaderSlot { Key = "FAV", Label = Loc.T("favorites"), Color = MaterialSectionHeaders.HeaderGold };
            if (e.Info == null || string.IsNullOrEmpty(e.Info.Group))
                return new HeaderSlot();
            return new HeaderSlot
            {
                Key = e.Info.GroupBand + "|" + e.Info.GroupOrder + "|" + e.Info.Group + "|" + e.CraftTier,
                Label = Loc.Group(e.Info.Group) + GroupColors.BiomeSuffix(e.Info),
                Color = MaterialSectionHeaders.HeaderGold
            };
        }

        /// <summary>Food rows: the strongest bar after the name, in the bar colour (Health red ...).</summary>
        private static void AppendFoodValue(GameObject row, RecipeInfo info)
        {
            if (row == null || info == null || info.Value <= 0f)
                return;
            if (info.Category != CraftCategory.Health && info.Category != CraftCategory.Stamina
                && info.Category != CraftCategory.Eitr && info.Category != CraftCategory.Prep)
                return;

            Transform nameT = row.transform.Find("name");
            TMPro.TMP_Text label = nameT != null ? nameT.GetComponent<TMPro.TMP_Text>() : null;
            if (label == null || label.text.EndsWith("</color>"))
                return;

            label.text += "  <color=#" + GroupColors.Hex(GroupColors.ForBar(info.Facts)) + ">"
                + Mathf.RoundToInt(info.Value) + "</color>";
        }

        private static bool RowCanCraft(object pair)
        {
            if (pair == null)
                return false;
            PropertyInfo p = AccessTools.Property(pair.GetType(), "CanCraft");
            return p != null && p.GetValue(pair, null) is bool b && b;
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
