using BepInEx.Configuration;

namespace WorkbenchesPlus
{
    public class ModConfig
    {
        public ConfigEntry<bool> EnableMod { get; }
        public ConfigEntry<bool> EnableCategories { get; }
        public ConfigEntry<bool> ShowCraftabilityIndicators { get; }
        public ConfigEntry<bool> EnableCraftMultiplier { get; }
        public ConfigEntry<bool> EnableUpgradeCompare { get; }
        public ConfigEntry<bool> EnableDismantle { get; }
        public ConfigEntry<int> DismantleRefundPercent { get; }
        public ConfigEntry<bool> EnableBuildRangeCycle { get; }
        public ConfigEntry<int> BuildRangeMaxMeters { get; }
        public ConfigEntry<bool> EnableAutoRepair { get; }
        public ConfigEntry<bool> DebugLogging { get; }

        public ModConfig(ConfigFile file)
        {
            EnableMod = file.Bind("General", "EnableMod", true,
                "Master switch for Workbenches+.");

            EnableCategories = file.Bind("Crafting menu", "EnableCategories", true,
                "Show category chips under the Repair button.");
            ShowCraftabilityIndicators = file.Bind("Crafting menu", "ShowCraftabilityIndicators", false,
                "Show a small green checkmark on fully craftable recipe rows.");
            EnableCraftMultiplier = file.Bind("Crafting menu", "EnableCraftMultiplier", true,
                "Show up/down arrows beside the Craft button to craft several at once.");
            EnableUpgradeCompare = file.Bind("Crafting menu", "EnableUpgradeCompare", true,
                "Upgrade tab: show the current stat value next to the upgraded one, e.g. Durability: 400 (now: 200).");

            EnableDismantle = file.Bind("Dismantle", "EnableDismantle", true,
                "Show a Dismantle tab beside Craft/Upgrade to break items back into crafting materials.");
            DismantleRefundPercent = file.Bind("Dismantle", "DismantleRefundPercent", 100,
                new ConfigDescription(
                    "Percent of the crafting materials returned by Dismantle.",
                    new AcceptableValueRange<int>(0, 100)));

            EnableBuildRangeCycle = file.Bind("Build range", "EnableBuildRangeCycle", true,
                "Look at a station the hammer needs and press Shift+E (AltPlace + Use) to cycle its build range: default, 50, 100, 150, 200 m. Saved on the station. Not available inside someone else's ward.");
            BuildRangeMaxMeters = file.Bind("Build range", "BuildRangeMaxMeters", 200,
                new ConfigDescription(
                    "Highest build range the cycle reaches (steps of 50 m).",
                    new AcceptableValueRange<int>(50, 500)));

            EnableAutoRepair = file.Bind("Auto repair", "EnableAutoRepair", false,
                "Repair worn armor, weapons and tools automatically while you stand inside the build range of a matching station (needs roof and fire like normal repair). One item every 1.5 s.");

            DebugLogging = file.Bind("Debug", "DebugLogging", false,
                "Writes a recipe dump (BepInEx/WorkbenchesPlus_recipes.tsv).");
        }
    }
}
