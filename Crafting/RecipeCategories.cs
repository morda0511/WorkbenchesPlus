namespace WorkbenchesPlus
{
    internal enum CraftCategory
    {
        All = 0,
        Weapons = 1,
        Armor = 2,
        Shields = 3,
        Tools = 4,
        Ammo = 5,
        Magic = 6,
        Food = 7,
        Feasts = 8,
        Bait = 9,
        Potions = 10,
        Materials = 11,
        Trinkets = 12,
        Clothes = 13,
        Furniture = 14,
        Building = 15,
        Health = 16,
        Stamina = 17,
        Eitr = 18,
        Cast = 19,
        Prep = 20,
        Fish = 21,
        Misc = 22
    }

    internal static class RecipeCategories
    {
        /// <summary>Fallback when no station profile applies (hand craft / unknown).</summary>
        public static readonly CraftCategory[] Order = StationCategoryProfiles.OrderFor(null);

        public static string Label(CraftCategory cat)
        {
            switch (cat)
            {
                case CraftCategory.All: return "All";
                case CraftCategory.Weapons: return "Weapons";
                case CraftCategory.Armor: return "Armor";
                case CraftCategory.Shields: return "Shields";
                case CraftCategory.Tools: return "Tools";
                case CraftCategory.Ammo: return "Ammo";
                case CraftCategory.Magic: return "Magic";
                case CraftCategory.Food: return "Food";
                case CraftCategory.Feasts: return "Feasts";
                case CraftCategory.Bait: return "Bait";
                case CraftCategory.Potions: return "Potions";
                case CraftCategory.Materials: return "Materials";
                case CraftCategory.Trinkets: return "Trinkets";
                case CraftCategory.Clothes: return "Clothes";
                case CraftCategory.Furniture: return "Furniture";
                case CraftCategory.Building: return "Building";
                case CraftCategory.Health: return "Health";
                case CraftCategory.Stamina: return "Stamina";
                case CraftCategory.Eitr: return "Eitr";
                case CraftCategory.Cast: return "Cast";
                case CraftCategory.Prep: return "Prep";
                case CraftCategory.Fish: return "Fish";
                default: return "Misc";
            }
        }

        public static string ShortLabel(CraftCategory cat)
        {
            return Label(cat);
        }

        /// <summary>
        /// When a recipe classifies outside this station's chip set, hide it from Available
        /// by mapping to Misc only if Misc exists — never invent foreign chips (Prep on Galdr).
        /// </summary>
        public static CraftCategory Classify(Recipe recipe)
        {
            CraftCategory raw = ClassifyRaw(recipe);
            Player player = Player.m_localPlayer;
            CraftingStation station = player != null ? player.GetCurrentCraftingStation() : null;
            if (station == null || StationFilter.IsUpgrader(station))
                return raw;
            if (StationCategoryProfiles.Allows(station, raw))
                return raw;
            if (StationCategoryProfiles.Allows(station, CraftCategory.Misc))
                return CraftCategory.Misc;
            // Station has no Misc chip: keep under All only (do not surface foreign categories).
            return CraftCategory.All;
        }

        private static CraftCategory ClassifyRaw(Recipe recipe)
        {
            if (recipe?.m_item?.m_itemData?.m_shared == null)
                return CraftCategory.Misc;

            ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
            ItemDrop.ItemData.ItemType t = shared.m_itemType;
            string prefab = recipe.m_item.name ?? "";
            string sharedName = shared.m_name ?? "";

            if (IsBait(prefab, sharedName))
                return CraftCategory.Bait;

            if (IsFeast(prefab, sharedName))
                return CraftCategory.Feasts;

            if (IsCastMold(prefab, sharedName) || IsCastBlank(prefab, sharedName))
                return CraftCategory.Cast;

            if (IsPrepUncooked(prefab, sharedName))
                return CraftCategory.Prep;

            if (IsRawFish(prefab, sharedName))
                return CraftCategory.Fish;

            if (IsMagicStaff(prefab, sharedName))
                return CraftCategory.Magic;

            if (IsTrinket(prefab, sharedName))
                return CraftCategory.Trinkets;

            if (IsClothes(prefab))
                return CraftCategory.Clothes;

            if (IsArrowOrBolt(prefab, sharedName) || IsTurretBolt(prefab))
                return CraftCategory.Ammo;

            if (t == ItemDrop.ItemData.ItemType.Shield)
                return CraftCategory.Shields;

            if (LooksToolPrefab(prefab))
                return CraftCategory.Tools;

            if (t == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || t == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || t == ItemDrop.ItemData.ItemType.Bow
                || t == ItemDrop.ItemData.ItemType.Attach_Atgeir
                || t == ItemDrop.ItemData.ItemType.Torch)
                return CraftCategory.Weapons;

            if (t == ItemDrop.ItemData.ItemType.Helmet
                || t == ItemDrop.ItemData.ItemType.Chest
                || t == ItemDrop.ItemData.ItemType.Legs
                || t == ItemDrop.ItemData.ItemType.Shoulder
                || t == ItemDrop.ItemData.ItemType.Utility)
            {
                if (prefab.IndexOf("Tool", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return CraftCategory.Tools;
                return CraftCategory.Armor;
            }

            if (t == ItemDrop.ItemData.ItemType.Tool)
                return CraftCategory.Tools;

            if (t == ItemDrop.ItemData.ItemType.Consumable)
            {
                if (IsPotionLike(prefab, sharedName))
                {
                    CraftCategory potionStat = ClassifyPotionStat(prefab, sharedName);
                    if (potionStat != CraftCategory.Potions)
                        return potionStat;
                    return CraftCategory.Potions;
                }

                CraftCategory foodStat = ClassifyFoodStat(shared);
                if (foodStat != CraftCategory.Food)
                    return foodStat;
                return CraftCategory.Food;
            }

            if (t == ItemDrop.ItemData.ItemType.Material)
            {
                if (IsPotionLike(prefab, sharedName))
                {
                    CraftCategory potionStat = ClassifyPotionStat(prefab, sharedName);
                    if (potionStat != CraftCategory.Potions)
                        return potionStat;
                    return CraftCategory.Potions;
                }
                return CraftCategory.Materials;
            }

            if (t == ItemDrop.ItemData.ItemType.Ammo
                || t == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
            {
                if (IsArrowOrBolt(prefab, sharedName) || IsTurretBolt(prefab))
                    return CraftCategory.Ammo;
                return CraftCategory.Misc;
            }

            if (LooksFurniture(prefab))
                return CraftCategory.Furniture;
            if (LooksBuilding(prefab))
                return CraftCategory.Building;

            return CraftCategory.Misc;
        }

        public static bool IsBait(Recipe recipe)
        {
            if (recipe?.m_item == null)
                return false;
            string prefab = recipe.m_item.name ?? "";
            string shared = "";
            try
            {
                if (recipe.m_item.m_itemData?.m_shared != null)
                    shared = recipe.m_item.m_itemData.m_shared.m_name ?? "";
            }
            catch
            {
            }
            return IsBait(prefab, shared);
        }

        public static bool IsBait(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Bait", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("FishingBait", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsArrowOrBolt(string prefab, string sharedName)
        {
            if (IsBait(prefab, sharedName))
                return false;
            if ((prefab ?? "").IndexOf("TurretBolt", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Arrow", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Bolt", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool Matches(Recipe recipe, CraftCategory filter)
        {
            if (filter == CraftCategory.All)
                return true;
            return Classify(recipe) == filter;
        }

        private static bool IsFeast(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            // Serving tray tool is Feaster — not a feast meal.
            if ((prefab ?? "").Equals("Feaster", System.StringComparison.OrdinalIgnoreCase))
                return false;
            return s.IndexOf("Feast", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Deep North moulds (Gussformen) — English chip label "Cast".</summary>
        private static bool IsCastMold(string prefab, string sharedName)
        {
            string p = prefab ?? "";
            string s = p + " " + (sharedName ?? "");
            return p.StartsWith("Mold", System.StringComparison.OrdinalIgnoreCase)
                || s.IndexOf("item_mold", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Mould", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Gussform", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Deep North cast blanks (*Uncooked gear) — not food prep.
        /// </summary>
        private static bool IsCastBlank(string prefab, string sharedName)
        {
            if (IsFoodPrepUncooked(prefab, sharedName))
                return false;
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Uncook", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPrepUncooked(string prefab, string sharedName)
        {
            return IsFoodPrepUncooked(prefab, sharedName);
        }

        /// <summary>Food Preparation Table dough / unbaked / food uncooked only.</summary>
        private static bool IsFoodPrepUncooked(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            if (s.IndexOf("Dough", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (s.IndexOf("Unbaked", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (s.IndexOf("Uncook", System.StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            // Explicit food prep — never Deep North Gold / gear blanks.
            return ContainsAny(s,
                "Pie", "Chicken", "Mushroom", "MeatPlatter", "Misthare", "Pancake",
                "Cupcake", "FishAndBread", "Sweetbread", "HoneyGlazed", "RoastedCrust",
                "Piquant", "LoxPie", "Bread");
        }

        private static bool ContainsAny(string hay, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (hay.IndexOf(needles[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static bool IsRawFish(string prefab, string sharedName)
        {
            string p = prefab ?? "";
            string s = p + " " + (sharedName ?? "");
            if (s.IndexOf("FishingBait", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (s.IndexOf("Feast", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return s.IndexOf("RawFish", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("FishRaw", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.Equals("Fish1", System.StringComparison.OrdinalIgnoreCase)
                || p.Equals("FishRaw", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("FishRaw", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("RawFish", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMagicStaff(string prefab, string sharedName)
        {
            if (IsCastMold(prefab, sharedName))
                return false;
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Staff", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("TorchMist", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTrinket(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Trinket", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsClothes(string prefab)
        {
            string p = prefab ?? "";
            return p.StartsWith("ArmorDress", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("ArmorTunic", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("ArmorHarvester", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("HelmetHat", System.StringComparison.OrdinalIgnoreCase)
                || p.StartsWith("HelmetStrawhat", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTurretBolt(string prefab)
        {
            return (prefab ?? "").IndexOf("TurretBolt", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Dominant food bar value for sorting (100 → 10) within Health/Stamina/Eitr.
        /// </summary>
        public static float FoodStatSortValue(Recipe recipe)
        {
            if (recipe?.m_item?.m_itemData?.m_shared == null)
                return 0f;
            ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
            CraftCategory cat = ClassifyFoodStat(shared);
            if (cat == CraftCategory.Health)
                return shared.m_food;
            if (cat == CraftCategory.Stamina)
                return shared.m_foodStamina;
            if (cat == CraftCategory.Eitr)
                return shared.m_foodEitr;
            float best = shared.m_food;
            if (shared.m_foodStamina > best)
                best = shared.m_foodStamina;
            if (shared.m_foodEitr > best)
                best = shared.m_foodEitr;
            return best;
        }

        /// <summary>
        /// Dominant food bar from vanilla SharedData (m_food / m_foodStamina / m_foodEitr).
        /// Ties prefer Health, then Stamina, then Eitr.
        /// </summary>
        public static CraftCategory ClassifyFoodStat(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
                return CraftCategory.Food;

            float hp = shared.m_food;
            float sta = shared.m_foodStamina;
            float eitr = shared.m_foodEitr;
            if (hp <= 0f && sta <= 0f && eitr <= 0f)
                return CraftCategory.Food;

            if (hp >= sta && hp >= eitr)
                return CraftCategory.Health;
            if (sta >= eitr)
                return CraftCategory.Stamina;
            return CraftCategory.Eitr;
        }

        /// <summary>
        /// Mead / potion bases by name when they have no food bars (Mead Ketill).
        /// </summary>
        public static CraftCategory ClassifyPotionStat(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            bool health = s.IndexOf("Health", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Heal", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool stamina = s.IndexOf("Stamina", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool eitr = s.IndexOf("Eitr", System.StringComparison.OrdinalIgnoreCase) >= 0;

            int hits = (health ? 1 : 0) + (stamina ? 1 : 0) + (eitr ? 1 : 0);
            if (hits == 1)
            {
                if (health)
                    return CraftCategory.Health;
                if (stamina)
                    return CraftCategory.Stamina;
                return CraftCategory.Eitr;
            }
            return CraftCategory.Potions;
        }

        private static bool LooksToolPrefab(string prefab)
        {
            string p = prefab ?? "";
            return p.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Hammer", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Hoe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Cultivator", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Scythe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Chisel", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Adze", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("SnowShovel", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("GrapplingHook", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Lantern", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Saddle", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("Sadle", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPotionLike(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Mead", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Potion", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Flask", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Brew", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Tonic", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("BarleyWine", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("OatMilk", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksFurniture(string prefab)
        {
            string p = prefab ?? "";
            return p.IndexOf("chair", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("table", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("bed", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("rug", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("banner", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("candle", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("sign", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("chest", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("itemstand", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("armorstand", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksBuilding(string prefab)
        {
            string p = prefab ?? "";
            return p.IndexOf("wood_", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("stone_", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("darkwood_", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("piece_", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("wall", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("floor", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("roof", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("beam", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("pole", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("stair", System.StringComparison.OrdinalIgnoreCase) >= 0
                || p.IndexOf("door", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
