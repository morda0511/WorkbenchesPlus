using System.Collections.Generic;

namespace WorkbenchesPlus
{
    /// <summary>
    /// The only place where Valheim recipes meet the pure sorting code: turns every recipe into
    /// RecipeFacts once, classifies them together (variants and casts need the whole set) and
    /// hands out the cached RecipeInfo. Rebuilt when the ObjectDB or the game language changes.
    /// </summary>
    internal static class SortEngine
    {
        private static Dictionary<Recipe, RecipeInfo> _map;
        private static ObjectDB _db;
        private static string _language;
        private static readonly HashSet<Recipe> Unknown = new HashSet<Recipe>();

        /// <summary>Verdict for a recipe; null only for recipes the ObjectDB does not know.</summary>
        public static RecipeInfo Info(Recipe recipe)
        {
            if (recipe == null || recipe.m_item == null)
                return null;

            EnsureBuilt();
            RecipeInfo info;
            if (_map != null && _map.TryGetValue(recipe, out info))
                return info;

            // A mod added the recipe after our snapshot: one rebuild, then remember the miss.
            if (_map != null && !Unknown.Contains(recipe))
            {
                Rebuild();
                if (_map.TryGetValue(recipe, out info))
                    return info;
                Unknown.Add(recipe);
            }
            return null;
        }

        public static CraftCategory CategoryOf(Recipe recipe)
        {
            RecipeInfo info = Info(recipe);
            return info != null ? info.Category : CraftCategory.Misc;
        }

        /// <summary>Plain ordering helper for callers that have no craftability (dismantle list).</summary>
        public static int Compare(Recipe a, Recipe b)
        {
            RecipeInfo ia = Info(a);
            RecipeInfo ib = Info(b);
            if (ia == null || ib == null)
                return ia == null ? (ib == null ? 0 : 1) : -1;
            return RecipeOrder.Compare(ia, 0, ib, 0);
        }

        private static void EnsureBuilt()
        {
            ObjectDB db = ObjectDB.instance;
            string lang = CurrentLanguage();
            if (_map != null && db == _db && lang == _language)
                return;
            Rebuild();
        }

        private static void Rebuild()
        {
            _map = new Dictionary<Recipe, RecipeInfo>();
            _db = ObjectDB.instance;
            _language = CurrentLanguage();
            Unknown.Clear();
            if (_db == null || _db.m_recipes == null)
                return;

            var recipes = new List<Recipe>(_db.m_recipes.Count);
            var facts = new List<RecipeFacts>(_db.m_recipes.Count);
            foreach (Recipe r in _db.m_recipes)
            {
                RecipeFacts f = FactsReader.Read(r);
                if (f == null)
                    continue;
                recipes.Add(r);
                facts.Add(f);
            }

            List<RecipeInfo> infos = RecipeClassifier.Build(facts);
            for (int i = 0; i < infos.Count; i++)
            {
                infos[i].SortName = LocalizedName(recipes[i], facts[i]);
                _map[recipes[i]] = infos[i];
            }
        }

        private static string LocalizedName(Recipe r, RecipeFacts f)
        {
            try
            {
                if (Localization.instance != null && !string.IsNullOrEmpty(f.Name))
                    return Localization.instance.Localize(f.Name);
            }
            catch
            {
            }
            return f.Prefab;
        }

        private static string CurrentLanguage()
        {
            try
            {
                return Localization.instance != null ? Localization.instance.GetSelectedLanguage() : "";
            }
            catch
            {
                return "";
            }
        }
    }

    /// <summary>Valheim Recipe -> RecipeFacts (the one reader; the dump uses it too).</summary>
    internal static class FactsReader
    {
        /// <summary>
        /// Intermediates (dough, uncooked pies ...) are Materials: their own food fields are placeholders and the
        /// tooltip shows the finished dish through m_appendToolTip. Use those numbers, like the game does.
        /// </summary>
        public static ItemDrop.ItemData.SharedData FoodSource(ItemDrop.ItemData.SharedData s)
        {
            if (s.m_itemType == ItemDrop.ItemData.ItemType.Consumable || s.m_appendToolTip == null
                || s.m_appendToolTip.m_itemData == null)
                return s;
            ItemDrop.ItemData.SharedData a = s.m_appendToolTip.m_itemData.m_shared;
            return a != null && (a.m_food > 0f || a.m_foodStamina > 0f || a.m_foodEitr > 0f) ? a : s;
        }

        public static RecipeFacts Read(Recipe r)
        {
            if (r == null || r.m_item == null)
                return null;
            ItemDrop.ItemData.SharedData s = r.m_item.m_itemData != null ? r.m_item.m_itemData.m_shared : null;
            if (s == null)
                return null;

            ItemDrop.ItemData.SharedData food = FoodSource(s);
            var f = new RecipeFacts
            {
                Station = r.m_craftingStation != null ? r.m_craftingStation.m_name : "(hand)",
                MinLevel = r.m_minStationLevel,
                Prefab = r.m_item.name,
                Name = s.m_name,
                ItemType = s.m_itemType.ToString(),
                Skill = s.m_skillType.ToString(),
                MaxQuality = s.m_maxQuality,
                Amount = r.m_amount,
                Food = food.m_food,
                Stamina = food.m_foodStamina,
                Eitr = food.m_foodEitr,
                FoodFrom = food == s ? "" : (s.m_appendToolTip.name ?? ""),
                AmmoType = s.m_ammoType ?? ""
            };

            if (r.m_resources != null)
            {
                foreach (Piece.Requirement q in r.m_resources)
                {
                    if (q == null || q.m_resItem == null)
                        continue;
                    f.Ingredients.Add(new Ingredient
                    {
                        Prefab = q.m_resItem.name,
                        Amount = q.m_amount,
                        PerLevel = q.m_amountPerLevel,
                        Idol = q.m_upgraderResource
                    });
                }
            }
            return f;
        }
    }
}
