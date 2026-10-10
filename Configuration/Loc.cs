using System.Collections.Generic;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Tiny in-mod string table (English + German). Vanilla Localization has no entry for our
    /// strings, so we pick the table by the selected game language and fall back to English.
    /// </summary>
    internal static class Loc
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            { "dismantle", "DISMANTLE" },
            { "dismantled", "Dismantled" },
            { "dismantle_failed", "Dismantle failed" },
            { "build_range", "Build range" },
            { "default", "default" },
            { "no_access", "You do not have access here" },
            { "now", "now" },
            { "search", "Search..." },
            { "favorites", "Favorites" },
            { "biome0", "Meadows" },
            { "biome1", "Black Forest" },
            { "biome2", "Swamp" },
            { "biome3", "Mountain" },
            { "biome4", "Plains" },
            { "biome5", "Mistlands" },
            { "biome6", "Ashlands" },
            { "biome7", "Deep North" },
            { "sub_OneHand", "1-Hand" },
            { "sub_TwoHand", "2-Hand" },
            { "sub_Knives", "Knives" },
            { "sub_Swords", "Swords" },
            { "sub_Clubs", "Clubs" },
            { "sub_Axes", "Axes" },
            { "sub_Spears", "Spears" },
            { "sub_Polearms", "Polearms" },
            { "sub_Bows", "Bows" },
            { "sub_Crossbows", "Crossbows" },
            { "sub_Fists", "Fists" },
            { "sub_Bombs", "Bombs" },
        };

        private static readonly Dictionary<string, string> German = new Dictionary<string, string>
        {
            { "dismantle", "ZERLEGEN" },
            { "dismantled", "Zerlegt" },
            { "dismantle_failed", "Zerlegen fehlgeschlagen" },
            { "build_range", "Baureichweite" },
            { "default", "Standard" },
            { "no_access", "Hier hast du keinen Zugriff" },
            { "now", "aktuell" },
            { "search", "Suchen..." },
            { "favorites", "Favoriten" },
            { "biome0", "Wiesen" },
            { "biome1", "Schwarzwald" },
            { "biome2", "Sumpf" },
            { "biome3", "Berge" },
            { "biome4", "Ebenen" },
            { "biome5", "Nebelland" },
            { "biome6", "Aschland" },
            { "biome7", "Hoher Norden" },
            { "sub_OneHand", "Einhand" },
            { "sub_TwoHand", "Zweihand" },
            { "sub_Knives", "Messer" },
            { "sub_Swords", "Schwerter" },
            { "sub_Clubs", "Keulen" },
            { "sub_Axes", "Äxte" },
            { "sub_Spears", "Speere" },
            { "sub_Polearms", "Stangenwaffen" },
            { "sub_Bows", "Bögen" },
            { "sub_Crossbows", "Armbrüste" },
            { "sub_Fists", "Fäuste" },
            { "sub_Bombs", "Bomben" },
        };

        public static string T(string key)
        {
            Dictionary<string, string> table = English;
            try
            {
                if (Localization.instance != null
                    && Localization.instance.GetSelectedLanguage() == "German")
                    table = German;
            }
            catch
            {
            }

            string value;
            if (table.TryGetValue(key, out value))
                return value;
            return English.TryGetValue(key, out value) ? value : key;
        }
    }
}
