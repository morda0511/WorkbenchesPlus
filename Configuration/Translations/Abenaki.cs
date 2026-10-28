using System.Collections.Generic;

namespace WorkbenchesPlus
{
    internal static partial class Loc
    {
        // Abenaki. Only the game's own words (biomes, weapon skills, materials); the rest falls back to English.
        private static readonly Dictionary<string, string> Abenaki = new Dictionary<string, string>
        {
            { "biome0", "Pamskodal" },
            { "biome1", "Pezowakamigokw" },
            { "biome2", "Megoakw" },
            { "biome3", "Wajo" },
            { "biome4", "Mskodal" },
            { "biome5", "Ahlabipikônkial" },
            { "biome6", "Skwedaibegwiki" },
            { "biome7", "Peponki" },
            { "sub_Knives", "Chakwakol" },
            { "sub_Swords", "Tmokwatahiganal" },
            { "sub_Clubs", "Kwalômal" },
            { "sub_Axes", "Temahigenal" },
            { "sub_Spears", "Enigakol" },
            { "sub_Polearms", "Kwanastahiganal" },
            { "sub_Bows", "Atôbiak" },
            { "cat_Bait", "Wlômanoik" },
            { "grp_Bait", "Wlômanoik" },
            { "grp_Wood", "Abazi" },
            { "grp_Stone", "Sen" },
            { "grp_Flint", "Mazipskw" },
            { "grp_Fine Wood", "Waolakwam" },
            { "grp_Copper", "Wizôwahlagw" },
            { "grp_Bronze", "Wadamôôbamahlagw" },
            { "grp_Chitin", "Sôgaadagen" },
            { "grp_Iron", "Alnahlakw" },
            { "grp_Silver", "Ôwadiwôbahlagw" },
            { "grp_Crystal", "Ahazaapskw" },
            { "grp_Black Metal", "Mkazahlagw" },
            { "grp_Needle", "Pokjahigan" },
            { "grp_Flametal", "Chegsahlagw" },
        };
    }
}
