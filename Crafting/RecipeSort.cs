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
            public int AmmoFirst;
            public int CraftBucket;
            public int MaterialPct;
            public int StationLevel;
            public int CategoryOrder;
            public string SetKey;
            public string MaterialBucket;
            public int PieceOrder;
            public float FoodStatValue;
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

            Player player = Player.m_localPlayer;
            if (player != null && StationFilter.IsUpgrader(player.GetCurrentCraftingStation()))
            {
                if (CategoryBar.Active != CraftCategory.All)
                    CategoryBar.SetActive(CraftCategory.All, rebuild: false);
                return;
            }

            // Dismantle already built a station-filtered inventory list — do not strip it again
            // (and keep DismantleMode.Items indices aligned with recipes).
            if (!DismantleMode.Active)
            {
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
            Player player = Player.m_localPlayer;
            if (player != null && StationFilter.IsUpgrader(player.GetCurrentCraftingStation()))
            {
                MaterialSectionHeaders.Clear();
                return;
            }
            if (Plugin.Settings.IsVanillaOrder())
            {
                MaterialSectionHeaders.Clear();
                return;
            }
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
            var pairByRecipe = new List<object>(pairs.Count);
            var elements = new List<GameObject>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                object pair = pairs[i];
                Recipe r = ReadRecipe(pair);
                recipes.Add(r);
                pairByRecipe.Add(pair);
                elements.Add(ReadElement(pair));
            }

            var order = new List<int>(recipes.Count);
            for (int i = 0; i < recipes.Count; i++)
                order.Add(i);

            var entries = BuildEntries(recipes);
            bool groupArmor = Plugin.Settings.EnableArmorSetGrouping.Value
                || Plugin.Settings.EnableWeaponSetGrouping.Value
                || Plugin.Settings.EnableToolSetGrouping.Value
                || Plugin.Settings.EnableShieldSetGrouping.Value;
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

            pairs.Clear();
            var orderedRecipes = new List<Recipe>(order.Count);
            var orderedElements = new List<GameObject>(order.Count);
            Transform listParent = null;

            for (int i = 0; i < order.Count; i++)
            {
                int idx = order[i];
                object pair = pairByRecipe[idx];
                pairs.Add(pair);
                orderedRecipes.Add(recipes[idx]);
                GameObject go = elements[idx];
                orderedElements.Add(go);
                if (listParent == null && go != null)
                    listParent = go.transform.parent;
            }

            MaterialSectionHeaders.Apply(
                listParent,
                orderedRecipes,
                orderedElements,
                space);

            if (Plugin.Settings.DebugLogging.Value)
            {
                Plugin.Log.LogInfo("ReorderGui rows=" + pairs.Count + " space=" + space);
                int n = System.Math.Min(12, order.Count);
                for (int i = 0; i < n; i++)
                {
                    Entry e = entries[order[i]];
                    Plugin.Log.LogInfo("  #" + i + " set=" + (e.SetKey ?? "-")
                        + " bucket=" + (e.MaterialBucket ?? "-")
                        + " ammo=" + e.AmmoFirst
                        + " tier=" + MaterialNameBucket.Tier(e.MaterialBucket)
                        + " bucketCraft=" + e.CraftBucket
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
                || Plugin.Settings.EnableWeaponSetGrouping.Value
                || Plugin.Settings.EnableToolSetGrouping.Value
                || Plugin.Settings.EnableShieldSetGrouping.Value;
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
            string[] toolOrder = ToolSetDetector.ParseOrder(Plugin.Settings.ToolPieceOrder.Value);
            string[] shieldOrder = ShieldSetDetector.ParseOrder(Plugin.Settings.ShieldPieceOrder.Value);
            bool groupArmor = Plugin.Settings.EnableArmorSetGrouping.Value;
            bool groupWeapons = Plugin.Settings.EnableWeaponSetGrouping.Value;
            bool groupTools = Plugin.Settings.EnableToolSetGrouping.Value;
            bool groupShields = Plugin.Settings.EnableShieldSetGrouping.Value;
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
                    // All arrows/bolts share one group — do not split by Wood/Iron/Bronze.
                    if (WeaponSetDetector.IsAmmo(r))
                    {
                        setKey = "WeaponAmmo";
                        pieceOrder = WeaponSetDetector.PieceOrder(r, weaponOrder);
                    }
                    else
                    {
                        setKey = WeaponSetDetector.SetKey(r, groupModdedWeapons);
                        if (!string.IsNullOrEmpty(setKey))
                            pieceOrder = WeaponSetDetector.PieceOrder(r, weaponOrder);
                    }
                }

                if (string.IsNullOrEmpty(setKey) && groupTools)
                {
                    setKey = ToolSetDetector.SetKey(r, groupModdedWeapons);
                    if (!string.IsNullOrEmpty(setKey))
                        pieceOrder = ToolSetDetector.PieceOrder(r, toolOrder);
                }

                if (string.IsNullOrEmpty(setKey) && groupShields)
                {
                    setKey = ShieldSetDetector.SetKey(r, groupModdedWeapons);
                    if (!string.IsNullOrEmpty(setKey))
                        pieceOrder = ShieldSetDetector.PieceOrder(r, shieldOrder);
                }

                entries.Add(new Entry
                {
                    Recipe = r,
                    OriginalIndex = i,
                    AmmoFirst = WeaponSetDetector.IsAmmo(r) ? 0 : 1,
                    CraftBucket = craftBuckets ? Craftability.ScoreBucket(player, r) : 0,
                    MaterialPct = craftBuckets ? Craftability.MaterialPercent(player, r) : 0,
                    StationLevel = r != null ? r.m_minStationLevel : 0,
                    CategoryOrder = categoryThen ? (int)RecipeCategories.Classify(r) : 0,
                    SetKey = setKey,
                    MaterialBucket = MaterialNameBucket.Resolve(r),
                    PieceOrder = pieceOrder,
                    FoodStatValue = RecipeCategories.FoodStatSortValue(r),
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

            // Craftable blocks first (with their own Bronze/Iron/… labels at the top),
            // then uncraftable blocks (same labels again further down).
            bool useBuckets = Plugin.Settings != null
                && (Plugin.Settings.EnableMaterialSectionHeaders.Value
                    || Plugin.Settings.EnableWeaponSetGrouping.Value
                    || Plugin.Settings.EnableArmorSetGrouping.Value
                    || Plugin.Settings.EnableToolSetGrouping.Value
                    || Plugin.Settings.EnableShieldSetGrouping.Value);

            if (useBuckets)
            {
                int bucketCmp = CompareNameBuckets(a, b, craftBuckets);
                if (bucketCmp != 0)
                    return bucketCmp;
            }
            else
            {
                if (craftBuckets)
                {
                    int c = a.CraftBucket.CompareTo(b.CraftBucket);
                    if (c != 0)
                        return c;
                }

                if (groupArmor)
                {
                    int ammo = a.AmmoFirst.CompareTo(b.AmmoFirst);
                    if (ammo != 0)
                        return ammo;
                }

                bool aSet = groupArmor && !string.IsNullOrEmpty(a.SetKey);
                bool bSet = groupArmor && !string.IsNullOrEmpty(b.SetKey);

                if (aSet && bSet)
                {
                    int mat = CompareMaterialThenPiece(a, b);
                    if (mat != 0)
                        return mat;
                }
                else if (groupArmor && (aSet || bSet))
                {
                    if (aSet)
                        return -1;
                    return 1;
                }
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

        private static int CompareNameBuckets(Entry a, Entry b, bool craftBuckets)
        {
            // 1) Craftable tier first → craftable Bronze appears at the top of All.
            if (craftBuckets)
            {
                int c = a.CraftBucket.CompareTo(b.CraftBucket);
                if (c != 0)
                    return c;
            }

            bool aHas = !string.IsNullOrEmpty(a.MaterialBucket);
            bool bHas = !string.IsNullOrEmpty(b.MaterialBucket);
            if (aHas != bHas)
                return aHas ? -1 : 1;
            if (!aHas)
                return 0;

            // 2) Within the same craft tier: material sections (Bronze, Iron, …).
            if (!string.Equals(a.MaterialBucket, b.MaterialBucket, System.StringComparison.OrdinalIgnoreCase))
            {
                int ta = MaterialNameBucket.Tier(a.MaterialBucket);
                int tb = MaterialNameBucket.Tier(b.MaterialBucket);
                int tierCmp = ta.CompareTo(tb);
                if (tierCmp != 0)
                    return tierCmp;
                return string.Compare(a.MaterialBucket, b.MaterialBucket, System.StringComparison.OrdinalIgnoreCase);
            }

            // 3) Same craft + same material: Tools → sort by inner material tier
            //    (Antler / Bronze / Iron / Black metal), not craftable order inside the label.
            if (a.MaterialBucket.Equals("Tools", System.StringComparison.OrdinalIgnoreCase))
            {
                int ta = MaterialNameBucket.NestedToolTier(a.Recipe);
                int tb = MaterialNameBucket.NestedToolTier(b.Recipe);
                int tierCmp = ta.CompareTo(tb);
                if (tierCmp != 0)
                    return tierCmp;
            }

            // Food stations: within Health / Stamina / Eitr, highest bar first (100 → 10).
            if (IsFoodStatBucket(a.MaterialBucket))
            {
                int foodCmp = b.FoodStatValue.CompareTo(a.FoodStatValue);
                if (foodCmp != 0)
                    return foodCmp;
            }

            int kind = MaterialKind(a.SetKey).CompareTo(MaterialKind(b.SetKey));
            if (kind != 0)
                return kind;

            int po = a.PieceOrder.CompareTo(b.PieceOrder);
            if (po != 0)
                return po;

            return string.Compare(a.SortName, b.SortName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFoodStatBucket(string bucket)
        {
            if (string.IsNullOrEmpty(bucket))
                return false;
            return bucket.Equals("Health", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Stamina", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Eitr", System.StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareMaterialThenPiece(Entry a, Entry b)
        {
            // Compare by material name (BlackMetal), not Weapon/Armor/Tool/Shield prefix,
            // so All does not split the same tier into multiple blocks.
            string matA = MaterialProgression.MaterialName(a.SetKey);
            string matB = MaterialProgression.MaterialName(b.SetKey);

            if (string.Equals(matA, matB, System.StringComparison.OrdinalIgnoreCase))
            {
                int kind = MaterialKind(a.SetKey).CompareTo(MaterialKind(b.SetKey));
                if (kind != 0)
                    return kind;

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

            return string.Compare(matA, matB, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Stable order inside one material: ammo, weapons, shields, tools, armor.</summary>
        private static int MaterialKind(string setKey)
        {
            if (string.IsNullOrEmpty(setKey))
                return 50;
            if (setKey.Equals("WeaponAmmo", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (setKey.StartsWith("Weapon", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (setKey.StartsWith("Shield", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (setKey.StartsWith("Tool", System.StringComparison.OrdinalIgnoreCase))
                return 3;
            if (setKey.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase)
                || setKey.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase)
                || setKey.StartsWith("Helmet", System.StringComparison.OrdinalIgnoreCase))
                return 4;
            return 40;
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
