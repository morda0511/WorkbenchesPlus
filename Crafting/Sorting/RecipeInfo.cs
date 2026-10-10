namespace WorkbenchesPlus
{
    /// <summary>
    /// The sorter verdict on one recipe: which chip it belongs to, which header it sits under,
    /// and the keys that order it inside that header.
    /// </summary>
    internal sealed class RecipeInfo
    {
        public RecipeFacts Facts;
        public CraftCategory Category = CraftCategory.Misc;

        // Header ("group") and where that header sits in the list.
        public string Group = "";
        public int GroupBand;           // tier / fixed slot; lower first
        public int GroupOrder = 500;    // order inside a band; lower first

        // Own keys.
        public int KindRank;            // ammo, weapon, shield, tool, armor, trinket ...
        public int ClassRank;           // weapon class / tool kind / shield kind
        public int PieceRank;           // chest, helmet, legs, cape
        public int ItemTier;            // biome tier of the hardest ingredient
        public float Value;             // food: dominant bar, highest first
        public string SetKey = "";
        public string[] Subs = new string[0];   // second-level chips (Weapons: OneHand, Swords ...)
        public string SortName = "";    // shown name; set by the caller (localized in game)

        // Variants and casts sort right behind the item they derive from.
        public RecipeInfo Root;
        public int Depth;

        public bool HasSub(string key)
        {
            for (int i = 0; i < Subs.Length; i++)
            {
                if (Subs[i] == key)
                    return true;
            }
            return false;
        }

        public RecipeInfo Anchor
        {
            get { return Root ?? this; }
        }
    }
}
