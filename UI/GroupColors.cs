using UnityEngine;

namespace WorkbenchesPlus
{
    /// <summary>Biome tag for gear headers, bar colours for food values.</summary>
    internal static class GroupColors
    {
        public static readonly Color Health = new Color(1.00f, 0.42f, 0.42f);
        public static readonly Color Stamina = new Color(1.00f, 0.88f, 0.35f);
        public static readonly Color Eitr = new Color(0.55f, 0.68f, 1.00f);

        /// <summary>" [Swamp]" for gear headers (the tier of the material), empty for everything else.</summary>
        public static string BiomeSuffix(RecipeInfo info)
        {
            switch (info.Category)
            {
                case CraftCategory.Weapons:
                case CraftCategory.Armor:
                case CraftCategory.Shields:
                case CraftCategory.Tools:
                    if (info.GroupBand >= 0 && info.GroupBand <= 7)
                        return " [" + Loc.T("biome" + info.GroupBand) + "]";
                    break;
            }
            return "";
        }

        /// <summary>Colour of the strongest food bar of a recipe.</summary>
        public static Color ForBar(RecipeFacts f)
        {
            if (f.Food >= f.Stamina && f.Food >= f.Eitr)
                return Health;
            return f.Stamina >= f.Eitr ? Stamina : Eitr;
        }

        public static string Hex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
