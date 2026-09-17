namespace WorkbenchesPlus
{
    /// <summary>
    /// Keeps only recipes that belong to the player's current crafting station.
    /// "All" means everything for THIS station, not every station in the game.
    /// </summary>
    internal static class StationFilter
    {
        public static void Apply(System.Collections.Generic.List<Recipe> recipes)
        {
            if (recipes == null || recipes.Count == 0)
                return;

            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            CraftingStation current = player.GetCurrentCraftingStation();
            for (int i = recipes.Count - 1; i >= 0; i--)
            {
                if (!BelongsToStation(recipes[i], current))
                    recipes.RemoveAt(i);
            }
        }

        public static bool BelongsToStation(Recipe recipe, CraftingStation current)
        {
            if (recipe == null)
                return false;

            CraftingStation required = recipe.m_craftingStation;

            // No station nearby: only recipes that need no station.
            if (current == null)
                return required == null;

            // Recipe needs a station: must be the same type (by m_name).
            if (required != null)
            {
                if (string.IsNullOrEmpty(required.m_name) || string.IsNullOrEmpty(current.m_name))
                    return false;
                return required.m_name == current.m_name;
            }

            // Hand / no-station recipe: only if this station shows basic recipes.
            return current.m_showBasicRecipies;
        }
    }
}
