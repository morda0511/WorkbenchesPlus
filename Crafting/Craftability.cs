using System.Collections.Generic;

namespace WorkbenchesPlus
{
    internal static class Craftability
    {
        public const int BucketFully = 0;
        public const int BucketPartial = 1;
        public const int BucketNone = 2;
        public const int BucketBlocked = 3;

        /// <summary>
        /// Section tier for sorting + headers: 0 = fully craftable, 1 = everything else.
        /// Partial / none / blocked share one block, so a label never repeats when nothing is craftable.
        /// </summary>
        public static int SectionTier(int bucket)
        {
            return bucket == BucketFully ? 0 : 1;
        }

        public static int ScoreBucket(Player player, Recipe recipe)
        {
            if (player == null || recipe == null || recipe.m_item == null)
                return BucketBlocked;

            // Station level / discovery gate - use vanilla HaveRequirements when possible.
            try
            {
                if (player.HaveRequirements(recipe, discover: false, qualityLevel: 1))
                    return BucketFully;
            }
            catch (System.Exception ex)
            {
                if (Plugin.Settings != null && Plugin.Settings.DebugLogging.Value)
                    Plugin.Log.LogWarning("HaveRequirements: " + ex.Message);
            }

            int have = 0;
            int need = 0;
            Piece.Requirement[] res = recipe.m_resources;
            if (res == null || res.Length == 0)
                return BucketNone;

            Inventory inv = player.GetInventory();
            if (inv == null)
                return BucketNone;

            CraftingStation station = player.GetCurrentCraftingStation();
            for (int i = 0; i < res.Length; i++)
            {
                Piece.Requirement r = res[i];
                if (r == null || r.m_resItem == null || r.m_resItem.m_itemData == null)
                    continue;
                if (!CountsTowardCraft(station, r))
                    continue;
                int amount = r.GetAmount(1);
                if (amount <= 0)
                    continue;
                need++;
                string shared = r.m_resItem.m_itemData.m_shared.m_name;
                if (inv.CountItems(shared, -1, true) >= amount)
                    have++;
            }

            if (need <= 0)
                return BucketNone;
            if (have <= 0)
                return BucketNone;
            if (have >= need)
            {
                // Materials OK but HaveRequirements failed → station/level/other block.
                return BucketBlocked;
            }
            return BucketPartial;
        }

        /// <summary>
        /// Same skip as vanilla HaveRequirementItems: Potential forge only spends idols.
        /// </summary>
        private static bool CountsTowardCraft(CraftingStation station, Piece.Requirement req)
        {
            if (req == null)
                return false;
            if (station != null && station.m_upgrader != req.m_upgraderResource)
                return false;
            if (station == null && req.m_upgraderResource)
                return false;
            return true;
        }
    }
}
