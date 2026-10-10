using System;

namespace WorkbenchesPlus
{
    /// <summary>
    /// The one comparer for every station:
    /// craftable first -> header (band, order, name) -> item kind -> class -> piece -> value -> tier -> name.
    /// Variants and casts compare through their anchor, so they sit right behind the item they come from.
    /// </summary>
    internal static class RecipeOrder
    {
        public static int Compare(RecipeInfo a, int craftTierA, RecipeInfo b, int craftTierB)
        {
            int c = craftTierA.CompareTo(craftTierB);
            if (c != 0)
                return c;

            c = a.GroupBand.CompareTo(b.GroupBand);
            if (c != 0)
                return c;
            c = a.GroupOrder.CompareTo(b.GroupOrder);
            if (c != 0)
                return c;
            c = string.Compare(a.Group, b.Group, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;

            return CompareInGroup(a, b);
        }

        /// <summary>Same header: the order the player reads top to bottom.</summary>
        public static int CompareInGroup(RecipeInfo a, RecipeInfo b)
        {
            RecipeInfo ra = a.Anchor;
            RecipeInfo rb = b.Anchor;

            int c = ra.KindRank.CompareTo(rb.KindRank);
            if (c != 0)
                return c;
            c = ra.ClassRank.CompareTo(rb.ClassRank);
            if (c != 0)
                return c;
            c = ra.PieceRank.CompareTo(rb.PieceRank);
            if (c != 0)
                return c;
            c = rb.Value.CompareTo(ra.Value);           // food: highest bar first
            if (c != 0)
                return c;
            c = ra.ItemTier.CompareTo(rb.ItemTier);
            if (c != 0)
                return c;
            c = ra.Facts.MinLevel.CompareTo(rb.Facts.MinLevel);
            if (c != 0)
                return c;
            c = string.Compare(ra.SortName, rb.SortName, StringComparison.OrdinalIgnoreCase);
            if (c != 0)
                return c;
            c = a.Depth.CompareTo(b.Depth);             // base item before its variants
            if (c != 0)
                return c;
            return string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
