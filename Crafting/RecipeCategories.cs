namespace WorkbenchesPlus
{
    internal enum CraftCategory
    {
        All = 0,
        Weapons = 1,
        Armor = 2,
        Shields = 3,
        Tools = 4,
        Building = 5,
        Furniture = 6,
        Food = 7,
        Potions = 8,
        Materials = 9,
        Misc = 10
    }

    internal static class RecipeCategories
    {
        public static readonly CraftCategory[] Order =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Tools,
            CraftCategory.Building,
            CraftCategory.Furniture,
            CraftCategory.Food,
            CraftCategory.Potions,
            CraftCategory.Materials,
            CraftCategory.Misc
        };

        public static string Label(CraftCategory cat)
        {
            switch (cat)
            {
                case CraftCategory.All: return "All";
                case CraftCategory.Weapons: return "Weapons";
                case CraftCategory.Armor: return "Armor";
                case CraftCategory.Shields: return "Shields";
                case CraftCategory.Tools: return "Tools";
                case CraftCategory.Building: return "Building";
                case CraftCategory.Furniture: return "Furniture";
                case CraftCategory.Food: return "Food";
                case CraftCategory.Potions: return "Potions";
                case CraftCategory.Materials: return "Materials";
                default: return "Misc";
            }
        }

        public static string ShortLabel(CraftCategory cat)
        {
            return Label(cat);
        }

        public static CraftCategory Classify(Recipe recipe)
        {
            if (recipe?.m_item?.m_itemData?.m_shared == null)
                return CraftCategory.Misc;

            ItemDrop.ItemData.SharedData shared = recipe.m_item.m_itemData.m_shared;
            ItemDrop.ItemData.ItemType t = shared.m_itemType;
            string prefab = recipe.m_item.name ?? "";
            string station = recipe.m_craftingStation != null ? recipe.m_craftingStation.name : "";

            if (t == ItemDrop.ItemData.ItemType.Shield)
                return CraftCategory.Shields;

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
                // Utility can be belts / wishbone — keep under armor-ish unless tool-like
                if (prefab.IndexOf("Tool", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return CraftCategory.Tools;
                return CraftCategory.Armor;
            }

            if (t == ItemDrop.ItemData.ItemType.Tool)
                return CraftCategory.Tools;

            if (t == ItemDrop.ItemData.ItemType.Consumable)
            {
                if (IsPotionLike(prefab, shared.m_name))
                    return CraftCategory.Potions;
                return CraftCategory.Food;
            }

            if (t == ItemDrop.ItemData.ItemType.Material)
                return CraftCategory.Materials;

            if (t == ItemDrop.ItemData.ItemType.Ammo
                || t == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
                return CraftCategory.Weapons;

            // Pieces / furniture via station + prefab
            if (station.IndexOf("piece_workbench", System.StringComparison.OrdinalIgnoreCase) >= 0
                || station.IndexOf("Workbench", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (LooksFurniture(prefab))
                    return CraftCategory.Furniture;
                if (LooksBuilding(prefab))
                    return CraftCategory.Building;
            }

            if (LooksFurniture(prefab))
                return CraftCategory.Furniture;
            if (LooksBuilding(prefab))
                return CraftCategory.Building;

            // Recipes that craft a Piece (building) often still have an ItemDrop — rare.
            return CraftCategory.Misc;
        }

        public static bool Matches(Recipe recipe, CraftCategory filter)
        {
            if (filter == CraftCategory.All)
                return true;
            return Classify(recipe) == filter;
        }

        private static bool IsPotionLike(string prefab, string sharedName)
        {
            string s = (prefab ?? "") + " " + (sharedName ?? "");
            return s.IndexOf("Mead", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Potion", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Flask", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Brew", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Tonic", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Poison", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Bait", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
                || p.IndexOf("torch", System.StringComparison.OrdinalIgnoreCase) >= 0
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
