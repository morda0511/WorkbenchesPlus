using System.Collections.Generic;

namespace WorkbenchesPlus
{
    /// <summary>
    /// World progression order for armor/weapon material groups.
    /// Earlier biomes / lighter materials first; unknown keys after known tiers.
    /// </summary>
    internal static class MaterialProgression
    {
        // Index = sort weight. Longer / more specific names first when matching.
        private static readonly string[] TierOrder =
        {
            // Meadows / early
            "Wood", "FineWood", "Flint", "Stone", "Bone", "Rags", "Rag", "Leather",
            // Black Forest
            "Copper", "TrollLeather", "Troll", "Bronze", "Banded", "HardAntler", "Antler",
            // Swamp
            "Iron", "Root", "Ancient", "Abyssal", "Chitin", "Serpentscale", "Serpent",
            // Mountain
            "Silver", "Wolf", "Fenrir", "Fenring", "Fenris", "Frost", "Crystal", "Obsidian", "Drake", "Lox", "Berserker", "Bear",
            // Plains
            "BlackMetal", "Padded", "Needle", "Fang", "Porcupine",
            // Mistlands
            "Carapace", "Eitr", "Mage", "Himmin", "Mist", "Dvergr", "Vilebone", "Gold", "Bloodgold",
            // Ashlands named
            "Flametal", "Ashlands", "Asksvin", "Embla", "Blood", "Bile", "Ooze",
            "Berzerkr", "Jotun", "Skull", "SkollHati", "Eldner", "Demolisher", "Splitnir",
            "Krom", "Niedhogg", "Slayer",
            // Elemental / misc ammo & specials (late-ish within their biome, after metals)
            "Fire", "Poison",
            // Workbench utility groups
            "Bomb", "Firework", "Potion", "Bait", "Health", "Stamina", "Eitr", "Cast", "Prep", "Fish", "Feasts", "Trinkets", "Skol", "Cape", "Clothes", "Furniture", "Materials", "Misc", "Special"
        };

        private static readonly Dictionary<string, int> IndexByMaterial =
            BuildIndex();

        public static int Tier(string setKey)
        {
            if (string.IsNullOrEmpty(setKey))
                return 10000;

            string mat = MaterialName(setKey);
            if (string.IsNullOrEmpty(mat))
                return 9000;

            int idx;
            if (IndexByMaterial.TryGetValue(mat, out idx))
                return idx;

            // Partial match (e.g. ArmorTrollLeatherChest leftover quirks)
            for (int i = 0; i < TierOrder.Length; i++)
            {
                if (mat.IndexOf(TierOrder[i], System.StringComparison.OrdinalIgnoreCase) >= 0
                    || TierOrder[i].IndexOf(mat, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            // Recognized as a set, but unknown material → after known tiers, before no-set items
            return 8000;
        }

        /// <summary>WeaponBlackMetal / ArmorIron / ToolBronze → BlackMetal / Iron / Bronze.</summary>
        public static string MaterialName(string setKey)
        {
            return StripPrefix(setKey);
        }

        private static string StripPrefix(string setKey)
        {
            string s = setKey;
            if (s.StartsWith("Weapon", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Weapon".Length);
            else if (s.StartsWith("Tool", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Tool".Length);
            else if (s.StartsWith("Shield", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Shield".Length);
            else if (s.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Armor".Length);
            else if (s.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Cape".Length);
            else if (s.StartsWith("Helmet", System.StringComparison.OrdinalIgnoreCase))
                s = s.Substring("Helmet".Length);

            return s;
        }

        private static Dictionary<string, int> BuildIndex()
        {
            var map = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < TierOrder.Length; i++)
            {
                string key = TierOrder[i];
                if (!map.ContainsKey(key))
                    map[key] = i;
            }
            // Aliases
            map["Blackmetal"] = map.ContainsKey("BlackMetal") ? map["BlackMetal"] : 0;
            map["Finewood"] = map.ContainsKey("FineWood") ? map["FineWood"] : 0;
            return map;
        }
    }
}
