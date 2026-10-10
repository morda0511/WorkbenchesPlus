namespace WorkbenchesPlus
{
    /// <summary>
    /// Second chip level: refine a category (Weapons -> 1-Hand, 2-Hand, Swords, Axes ...).
    /// A recipe can belong to several sub chips (a 1-handed axe is "OneHand" and "Axes").
    /// </summary>
    internal static class SubCategories
    {
        private static readonly string[] None = new string[0];

        // Order = order of the chips under Weapons.
        private static readonly string[] Weapons =
        {
            "OneHand", "TwoHand",
            "Knives", "Swords", "Clubs", "Axes", "Spears", "Polearms",
            "Bows", "Crossbows", "Fists", "Bombs"
        };

        public static string[] For(CraftCategory cat)
        {
            return cat == CraftCategory.Weapons ? Weapons : None;
        }

        /// <summary>Hand keys form their own filter group (1-Hand / 2-Hand); everything else is the type group.</summary>
        public static bool IsHand(string key)
        {
            return key == "OneHand" || key == "TwoHand";
        }

        /// <summary>Sub chips of a weapon recipe: its hands plus its weapon type.</summary>
        public static string[] ForWeapon(string itemType, string skill, string prefab)
        {
            // Bombs are thrown: own chip only, so they do not clutter 1-Hand.
            if (skill == "None" && prefab.StartsWith("Bomb", System.StringComparison.Ordinal))
                return new[] { "Bombs" };

            // Bows and crossbows only under their own chip, never under 1-Hand / 2-Hand.
            if (skill == "Bows" || itemType == "Bow" && skill != "Crossbows")
                return new[] { "Bows" };
            if (skill == "Crossbows")
                return new[] { "Crossbows" };

            string hands = itemType == "OneHandedWeapon" ? "OneHand" : "TwoHand";
            string type = null;
            switch (skill)
            {
                case "Knives": type = "Knives"; break;
                case "Swords": type = "Swords"; break;
                case "Clubs": type = "Clubs"; break;
                case "Axes": type = "Axes"; break;
                case "Spears": type = "Spears"; break;
                case "Polearms": type = "Polearms"; break;
                case "Bows": type = "Bows"; break;
                case "Crossbows": type = "Crossbows"; break;
                case "Unarmed": type = "Fists"; break;
            }
            return type == null ? new[] { hands } : new[] { hands, type };
        }
    }
}
