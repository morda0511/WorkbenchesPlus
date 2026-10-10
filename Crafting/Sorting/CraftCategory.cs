namespace WorkbenchesPlus
{
    /// <summary>One chip in the category bar. Every recipe belongs to exactly one.</summary>
    internal enum CraftCategory
    {
        All = 0,
        Weapons,
        Armor,
        Shields,
        Tools,
        Ammo,
        Magic,
        Health,
        Stamina,
        Eitr,
        Feasts,
        Prep,
        Fish,
        Bait,
        Potions,
        Cast,
        Materials,
        Trinkets,
        Clothes,
        Misc
    }

    internal static class CraftCategories
    {
        /// <summary>Chip order for every station. Empty chips are hidden by the bar.</summary>
        public static readonly CraftCategory[] ChipOrder =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Tools,
            CraftCategory.Ammo,
            CraftCategory.Magic,
            CraftCategory.Health,
            CraftCategory.Stamina,
            CraftCategory.Eitr,
            CraftCategory.Feasts,
            CraftCategory.Prep,
            CraftCategory.Fish,
            CraftCategory.Bait,
            CraftCategory.Potions,
            CraftCategory.Cast,
            CraftCategory.Materials,
            CraftCategory.Trinkets,
            CraftCategory.Clothes,
            CraftCategory.Misc
        };

        public static string Label(CraftCategory cat)
        {
            return cat == CraftCategory.All ? "All" : cat.ToString();
        }
    }
}
