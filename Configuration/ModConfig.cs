using BepInEx.Configuration;

namespace WorkbenchesPlus
{
    public class ModConfig
    {
        public ConfigEntry<bool> EnableMod { get; }
        public ConfigEntry<bool> EnableCategories { get; }
        public ConfigEntry<bool> EnableCraftableFirst { get; }
        public ConfigEntry<bool> EnableArmorSetGrouping { get; }
        public ConfigEntry<bool> EnableWeaponSetGrouping { get; }
        public ConfigEntry<bool> EnableToolSetGrouping { get; }
        public ConfigEntry<bool> EnableShieldSetGrouping { get; }
        public ConfigEntry<bool> EnableMaterialSectionHeaders { get; }
        public ConfigEntry<string> SortMode { get; }
        public ConfigEntry<string> ArmorPieceOrder { get; }
        public ConfigEntry<string> WeaponPieceOrder { get; }
        public ConfigEntry<string> ToolPieceOrder { get; }
        public ConfigEntry<string> ShieldPieceOrder { get; }
        public ConfigEntry<bool> GroupModdedArmorSets { get; }
        public ConfigEntry<bool> GroupModdedWeaponSets { get; }
        public ConfigEntry<bool> RefreshOnInventoryChange { get; }
        public ConfigEntry<bool> RefreshOnStationChange { get; }
        public ConfigEntry<bool> ShowCraftabilityIndicators { get; }
        public ConfigEntry<bool> EnableCraftMultiplier { get; }
        public ConfigEntry<int> MaxCraftMultiplier { get; }
        public ConfigEntry<bool> EnableDismantle { get; }
        public ConfigEntry<bool> EnableBuildRangeCycle { get; }
        public ConfigEntry<bool> DebugLogging { get; }

        public ModConfig(ConfigFile file)
        {
            EnableMod = file.Bind("1 - General", "EnableMod", true,
                "Master switch for Workbenches+.");
            EnableCategories = file.Bind("1 - General", "EnableCategories", true,
                "Show category chips above the crafting recipe list.");
            EnableCraftableFirst = file.Bind("2 - Sorting", "EnableCraftableFirst", true,
                "When SortMode is CraftableFirst / CategoryThenCraftable, prioritize craftable recipes.");
            EnableArmorSetGrouping = file.Bind("2 - Sorting", "EnableArmorSetGrouping", true,
                "Keep armor pieces from the same set next to each other.");
            EnableWeaponSetGrouping = file.Bind("2 - Sorting", "EnableWeaponSetGrouping", true,
                "Keep weapons of the same material together (Iron, BlackMetal, Bronze, …).");
            EnableToolSetGrouping = file.Bind("2 - Sorting", "EnableToolSetGrouping", true,
                "Keep tools of the same material together (Antler, Bronze, Iron, BlackMetal, …).");
            EnableShieldSetGrouping = file.Bind("2 - Sorting", "EnableShieldSetGrouping", true,
                "Keep shields of the same material together (Wood, Bronze, Iron, BlackMetal, …).");
            EnableMaterialSectionHeaders = file.Bind("2 - Sorting", "EnableMaterialSectionHeaders", true,
                "Show section labels between material groups (IRON, BRONZE, Arrows), left-aligned.");
            SortMode = file.Bind("2 - Sorting", "SortMode", "CraftableFirst",
                "CraftableFirst | CategoryThenCraftable | Progression | Alphabetical | Vanilla");
            ArmorPieceOrder = file.Bind("3 - Armor", "ArmorPieceOrder", "Chest,Helmet,Legs,Cape,Other",
                "Comma-separated piece order within an armor set.");
            GroupModdedArmorSets = file.Bind("3 - Armor", "GroupModdedArmorSets", true,
                "Apply set-name heuristics to non-vanilla armor prefabs.");
            WeaponPieceOrder = file.Bind("3b - Weapons", "WeaponPieceOrder",
                "Arrow,Bolt,Knife,Sword,Mace,Axe,Battleaxe,Spear,Atgeir,Sledge,Bow,Crossbow,Club,Bomb,Pickaxe,Other",
                "Comma-separated weapon type order within a material group. Arrows first by default.");
            GroupModdedWeaponSets = file.Bind("3b - Weapons", "GroupModdedWeaponSets", true,
                "Apply material heuristics to non-vanilla weapon prefabs.");
            ToolPieceOrder = file.Bind("3c - Tools", "ToolPieceOrder",
                "Hammer,Hoe,Cultivator,Pickaxe,Scythe,Other",
                "Comma-separated tool type order within a material group.");
            ShieldPieceOrder = file.Bind("3d - Shields", "ShieldPieceOrder",
                "Shield,Tower,Other",
                "Comma-separated shield type order within a material group.");
            RefreshOnInventoryChange = file.Bind("4 - Refresh", "RefreshOnInventoryChange", true,
                "Re-sort when the local player inventory changes while crafting is open.");
            RefreshOnStationChange = file.Bind("4 - Refresh", "RefreshOnStationChange", true,
                "Reset category to All when switching crafting stations.");
            ShowCraftabilityIndicators = file.Bind("5 - UI", "ShowCraftabilityIndicators", false,
                "Show a small green checkmark on fully craftable recipe rows.");
            EnableCraftMultiplier = file.Bind("5 - UI", "EnableCraftMultiplier", true,
                "Show up/down arrows beside the Craft button for multi-craft amount.");
            MaxCraftMultiplier = file.Bind("5 - UI", "MaxCraftMultiplier", 99,
                "Upper cap for the craft multiplier (materials still limit how high you can go).");
            EnableDismantle = file.Bind("5 - UI", "EnableDismantle", true,
                "Show a Dismantle tab beside Craft/Upgrade to break items back into crafting materials.");
            EnableBuildRangeCycle = file.Bind("6 - Build range", "EnableBuildRangeCycle", true,
                "Look at a station with a build radius and press Shift+E (AltPlace + Use) to set build range: default → 50 → 100 → 150 → 200 → default. Saved on the station.");
            DebugLogging = file.Bind("9 - Debug", "DebugLogging", false,
                "Extra log lines for sorting decisions (set keys, order).");
        }

        public string Mode()
        {
            return (SortMode.Value ?? "CraftableFirst").Trim();
        }

        public bool IsVanillaOrder()
        {
            return Mode().Equals("Vanilla", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsAlphabetical()
        {
            return Mode().Equals("Alphabetical", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsProgression()
        {
            return Mode().Equals("Progression", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsCategoryThenCraftable()
        {
            return Mode().Equals("CategoryThenCraftable", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool UseCraftBuckets()
        {
            if (!EnableCraftableFirst.Value)
                return false;
            string m = Mode();
            return m.Equals("CraftableFirst", System.StringComparison.OrdinalIgnoreCase)
                || m.Equals("CategoryThenCraftable", System.StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(m);
        }
    }
}
