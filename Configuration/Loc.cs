using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// In-mod string table: English here, every other language of the game's settings in Translations/*.cs.
    /// Vanilla Localization has no entry for our strings, so we pick the table by the game language (or the
    /// Language setting) and fall back to English for a missing language or key.
    /// </summary>
    internal static partial class Loc
    {
        /// <summary>The game's languages (its localization file's columns), for the Language setting.</summary>
        public static readonly string[] GameLanguages =
        {
            "English", "Swedish", "French", "Italian", "German", "Spanish", "Russian", "Romanian", "Bulgarian",
            "Macedonian", "Finnish", "Danish", "Norwegian", "Icelandic", "Turkish", "Lithuanian", "Czech",
            "Hungarian", "Slovak", "Polish", "Dutch", "Portuguese_European", "Portuguese_Brazilian", "Chinese",
            "Chinese_Trad", "Japanese", "Korean", "Hindi", "Thai", "Abenaki", "Croatian", "Georgian", "Greek",
            "Serbian", "Ukrainian", "Latvian",
        };

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
            { "meters", "{0} m" },
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

            // Category chips (CraftCategory names).
            { "cat_All", "All" },
            { "cat_Weapons", "Weapons" },
            { "cat_Armor", "Armor" },
            { "cat_Shields", "Shields" },
            { "cat_Tools", "Tools" },
            { "cat_Ammo", "Ammo" },
            { "cat_Magic", "Magic" },
            { "cat_Health", "Health" },
            { "cat_Stamina", "Stamina" },
            { "cat_Eitr", "Eitr" },
            { "cat_Feasts", "Feasts" },
            { "cat_Prep", "Prep" },
            { "cat_Fish", "Fish" },
            { "cat_Bait", "Bait" },
            { "cat_Potions", "Potions" },
            { "cat_Cast", "Cast" },
            { "cat_Materials", "Materials" },
            { "cat_Trinkets", "Trinkets" },
            { "cat_Clothes", "Clothes" },
            { "cat_Misc", "Misc" },

            // Section headers (RecipeInfo.Group): fixed groups, material families, armor sets. A group missing
            // here (a mod's armor set) is shown as it is.
            { "grp_Arrows", "Arrows" },
            { "grp_Bolts", "Bolts" },
            { "grp_Siege", "Siege" },
            { "grp_Turret bolts", "Turret bolts" },
            { "grp_Capes", "Capes" },
            { "grp_Special", "Special" },
            { "grp_Bait", "Bait" },
            { "grp_Cast", "Cast" },
            { "grp_Clothes", "Clothes" },
            { "grp_Health", "Health" },
            { "grp_Stamina", "Stamina" },
            { "grp_Eitr", "Eitr" },
            { "grp_Potions", "Potions" },
            { "grp_Feasts", "Feasts" },
            { "grp_Fish", "Fish" },
            { "grp_Prep", "Prep" },
            { "grp_Staffs", "Staffs" },
            { "grp_Materials", "Materials" },
            { "grp_Fireworks", "Fireworks" },
            { "grp_Tankards", "Tankards" },
            { "grp_Misc", "Misc" },
            { "grp_Trinkets", "Trinkets" },
            { "grp_Bombs", "Bombs" },
            { "grp_Wood", "Wood" },
            { "grp_Stone", "Stone" },
            { "grp_Flint", "Flint" },
            { "grp_Bear", "Bear" },
            { "grp_Antler", "Antler" },
            { "grp_Fine Wood", "Fine Wood" },
            { "grp_Copper", "Copper" },
            { "grp_Bronze", "Bronze" },
            { "grp_Chitin", "Chitin" },
            { "grp_Iron", "Iron" },
            { "grp_Serpent", "Serpent" },
            { "grp_Silver", "Silver" },
            { "grp_Crystal", "Crystal" },
            { "grp_Yggdrasil", "Yggdrasil" },
            { "grp_Black Metal", "Black Metal" },
            { "grp_Needle", "Needle" },
            { "grp_Carapace", "Carapace" },
            { "grp_Charred", "Charred" },
            { "grp_Flametal", "Flametal" },
            { "grp_Bloodgold", "Bloodgold" },
            { "grp_Rags", "Rags" },
            { "grp_Leather", "Leather" },
            { "grp_Troll Leather", "Troll Leather" },
            { "grp_Root", "Root" },
            { "grp_Wolf", "Wolf" },
            { "grp_Fenrir", "Fenrir" },
            { "grp_Padded", "Padded" },
            { "grp_Lox", "Lox" },
            { "grp_Berserker", "Berserker" },
            { "grp_Vilebone", "Vilebone" },
            { "grp_Mage", "Mage" },
            { "grp_Ashlands", "Ashlands" },
            { "grp_Mage (Ashlands)", "Mage (Ashlands)" },
            { "grp_Deep North Heavy", "Deep North Heavy" },
            { "grp_Deep North Medium", "Deep North Medium" },
            { "grp_Deep North Mage", "Deep North Mage" },
            { "grp_Midsummer Crown", "Midsummer Crown" },
            { "grp_Pointy Hat", "Pointy Hat" },
            { "grp_Straw Hat", "Straw Hat" },
            // Numbered vanilla outfits: "Dress3" is shown as the translated word plus the number.
            { "grp_Dress", "Dress" },
            { "grp_Tunic", "Tunic" },
            { "grp_Hat", "Hat" },
            { "grp_Harvester", "Harvester" },
        };

        private static readonly Regex Numbered = new Regex(@"^(\D+?)(\d+)$", RegexOptions.Compiled);

        /// <summary>The Language setting, or the game's language when it is Auto.</summary>
        public static string Language
        {
            get
            {
                string forced = Plugin.Settings != null ? Plugin.Settings.Language.Value : null;
                if (!string.IsNullOrEmpty(forced) && forced != "Auto")
                    return forced;
                try
                {
                    if (Localization.instance != null)
                        return Localization.instance.GetSelectedLanguage();
                }
                catch
                {
                }
                return "English";
            }
        }

        public static string T(string key)
        {
            Dictionary<string, string> table = Table(Language) ?? English;
            string value;
            if (table.TryGetValue(key, out value))
                return value;
            return English.TryGetValue(key, out value) ? value : key;
        }

        /// <summary>Category chip label.</summary>
        public static string Category(CraftCategory cat)
        {
            return T("cat_" + cat);
        }

        /// <summary>Section header text for a RecipeInfo.Group (which stays the English sort key).</summary>
        public static string Group(string group)
        {
            if (string.IsNullOrEmpty(group))
                return group;
            if (English.ContainsKey("grp_" + group))
                return T("grp_" + group);
            Match m = Numbered.Match(group);
            if (m.Success && English.ContainsKey("grp_" + m.Groups[1].Value))
                return T("grp_" + m.Groups[1].Value) + " " + m.Groups[2].Value;
            return group;
        }

        public static string Meters(float meters)
        {
            return T("meters").Replace("{0}", UnityEngine.Mathf.RoundToInt(meters).ToString());
        }

        // A switch, not a dictionary of tables: the tables live in other files of this partial class, and a static
        // dictionary built from them could run before they are set.
        private static Dictionary<string, string> Table(string language)
        {
            switch (language)
            {
                case "English": return English;
                case "Swedish": return Swedish;
                case "French": return French;
                case "Italian": return Italian;
                case "German": return German;
                case "Spanish": return Spanish;
                case "Russian": return Russian;
                case "Romanian": return Romanian;
                case "Bulgarian": return Bulgarian;
                case "Macedonian": return Macedonian;
                case "Finnish": return Finnish;
                case "Danish": return Danish;
                case "Norwegian": return Norwegian;
                case "Icelandic": return Icelandic;
                case "Turkish": return Turkish;
                case "Lithuanian": return Lithuanian;
                case "Czech": return Czech;
                case "Hungarian": return Hungarian;
                case "Slovak": return Slovak;
                case "Polish": return Polish;
                case "Dutch": return Dutch;
                case "Portuguese_European": return PortugueseEuropean;
                case "Portuguese_Brazilian": return PortugueseBrazilian;
                case "Chinese": return Chinese;
                case "Chinese_Trad": return ChineseTrad;
                case "Japanese": return Japanese;
                case "Korean": return Korean;
                case "Hindi": return Hindi;
                case "Thai": return Thai;
                case "Abenaki": return Abenaki;
                case "Croatian": return Croatian;
                case "Georgian": return Georgian;
                case "Greek": return Greek;
                case "Serbian": return Serbian;
                case "Ukrainian": return Ukrainian;
                case "Latvian": return Latvian;
                default: return null;
            }
        }
    }
}
