namespace WorkbenchesPlus
{
    /// <summary>
    /// Per-station chip sets. Empty chips stay hidden via CategoryBar.Available.
    /// Station identity uses CraftingStation.m_name (localization token).
    /// </summary>
    internal static class StationCategoryProfiles
    {
        private static readonly CraftCategory[] DefaultOrder =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Tools,
            CraftCategory.Ammo,
            CraftCategory.Magic,
            CraftCategory.Food,
            CraftCategory.Feasts,
            CraftCategory.Bait,
            CraftCategory.Potions,
            CraftCategory.Health,
            CraftCategory.Stamina,
            CraftCategory.Eitr,
            CraftCategory.Cast,
            CraftCategory.Prep,
            CraftCategory.Fish,
            CraftCategory.Materials,
            CraftCategory.Trinkets,
            CraftCategory.Clothes,
            CraftCategory.Furniture,
            CraftCategory.Building,
            CraftCategory.Misc
        };

        private static readonly CraftCategory[] Workbench =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Tools,
            CraftCategory.Ammo,
            CraftCategory.Clothes,
            CraftCategory.Misc
        };

        private static readonly CraftCategory[] Forge =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Tools,
            CraftCategory.Ammo,
            CraftCategory.Materials,
            CraftCategory.Trinkets
        };

        private static readonly CraftCategory[] BlackForge =
        {
            CraftCategory.All,
            CraftCategory.Weapons,
            CraftCategory.Armor,
            CraftCategory.Shields,
            CraftCategory.Ammo,
            CraftCategory.Cast,
            CraftCategory.Tools,
            CraftCategory.Trinkets,
            CraftCategory.Materials
        };

        private static readonly CraftCategory[] Cauldron =
        {
            CraftCategory.All,
            CraftCategory.Health,
            CraftCategory.Stamina,
            CraftCategory.Eitr
        };

        private static readonly CraftCategory[] PrepTable =
        {
            CraftCategory.All,
            CraftCategory.Feasts,
            CraftCategory.Prep,
            CraftCategory.Fish,
            CraftCategory.Bait
        };

        private static readonly CraftCategory[] MeadKetill =
        {
            CraftCategory.All,
            CraftCategory.Health,
            CraftCategory.Stamina,
            CraftCategory.Eitr,
            CraftCategory.Potions
        };

        private static readonly CraftCategory[] Galdr =
        {
            CraftCategory.All,
            CraftCategory.Magic,
            CraftCategory.Cast,
            CraftCategory.Armor,
            CraftCategory.Materials
        };

        private static readonly CraftCategory[] Artisan =
        {
            CraftCategory.All,
            CraftCategory.Materials,
            CraftCategory.Ammo,
            CraftCategory.Misc
        };

        private static readonly CraftCategory[] Stonecutter =
        {
            CraftCategory.All,
            CraftCategory.Materials,
            CraftCategory.Tools,
            CraftCategory.Misc
        };

        private static readonly CraftCategory[] Oven =
        {
            CraftCategory.All,
            CraftCategory.Health,
            CraftCategory.Stamina,
            CraftCategory.Eitr,
            CraftCategory.Feasts
        };

        public static string ProfileKey(CraftingStation station)
        {
            if (station == null)
                return "hand";
            if (StationFilter.IsUpgrader(station))
                return "upgrader";
            return KindOf(station).ToString();
        }

        public static CraftCategory[] OrderFor(CraftingStation station)
        {
            switch (KindOf(station))
            {
                case StationKind.Workbench: return Workbench;
                case StationKind.Forge: return Forge;
                case StationKind.BlackForge: return BlackForge;
                case StationKind.Cauldron: return Cauldron;
                case StationKind.PrepTable: return PrepTable;
                case StationKind.MeadKetill: return MeadKetill;
                case StationKind.Galdr: return Galdr;
                case StationKind.Artisan: return Artisan;
                case StationKind.Stonecutter: return Stonecutter;
                case StationKind.Oven: return Oven;
                default: return DefaultOrder;
            }
        }

        public static bool Allows(CraftingStation station, CraftCategory cat)
        {
            if (cat == CraftCategory.All)
                return true;
            CraftCategory[] order = OrderFor(station);
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == cat)
                    return true;
            }
            return false;
        }

        public static StationKind KindOf(CraftingStation station)
        {
            if (station == null)
                return StationKind.Unknown;

            string name = station.m_name ?? "";
            string prefab = station.name ?? "";
            string blob = name + " " + prefab;

            if (Contains(blob, "blackforge") || Contains(blob, "black_forge"))
                return StationKind.BlackForge;
            if (Contains(blob, "meadcauldron") || Contains(blob, "mead_cauldron") || Contains(blob, "meadketill"))
                return StationKind.MeadKetill;
            if (Contains(blob, "preptable") || Contains(blob, "prep_table") || Contains(blob, "foodprep"))
                return StationKind.PrepTable;
            if (Contains(blob, "cauldron") && !Contains(blob, "mead"))
                return StationKind.Cauldron;
            if (Contains(blob, "magetable") || Contains(blob, "galdr"))
                return StationKind.Galdr;
            if (Contains(blob, "artisan"))
                return StationKind.Artisan;
            if (Contains(blob, "stonecutter"))
                return StationKind.Stonecutter;
            if (Contains(blob, "oven"))
                return StationKind.Oven;
            if (Contains(blob, "workbench"))
                return StationKind.Workbench;
            // plain forge — after blackforge check
            if (Contains(blob, "forge") || name == "$piece_forge")
                return StationKind.Forge;

            return StationKind.Unknown;
        }

        private static bool Contains(string hay, string needle)
        {
            return hay.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal enum StationKind
        {
            Unknown = 0,
            Workbench,
            Forge,
            BlackForge,
            Cauldron,
            PrepTable,
            MeadKetill,
            Galdr,
            Artisan,
            Stonecutter,
            Oven
        }
    }
}
