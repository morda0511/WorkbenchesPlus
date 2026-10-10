using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;

namespace WorkbenchesPlus
{
    /// <summary>
    /// DebugLogging only: writes every recipe of the loaded game (vanilla + mods) with the facts
    /// the sorter can use, to BepInEx/WorkbenchesPlus_recipes.tsv. One file per game start.
    /// </summary>
    internal static class RecipeDump
    {
        private static bool _done;

        public static void Tick()
        {
            if (_done || Plugin.Settings == null || !Plugin.Settings.DebugLogging.Value)
                return;
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null
                || ObjectDB.instance.m_recipes.Count == 0 || Player.m_localPlayer == null)
                return;

            _done = true;
            try
            {
                Write();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning("RecipeDump: " + ex.Message);
            }
        }

        private static void Write()
        {
            var sb = new StringBuilder();
            sb.AppendLine("station\trepairStation\tminLevel\tprefab\tname\titemType\tskill\tmaxQuality\tamount"
                + "\tfood\tstamina\teitr\tarmor\tdamage\tblock\tweight\tresources\tcategory\tmaterialBucket"
                + "\trawFood\trawStamina\trawEitr\tfoodFrom\tammoType");

            foreach (Recipe r in ObjectDB.instance.m_recipes)
            {
                if (r == null || r.m_item == null || !r.m_enabled)
                    continue;
                ItemDrop.ItemData.SharedData s = r.m_item.m_itemData?.m_shared;
                if (s == null)
                    continue;

                var res = new List<string>();
                if (r.m_resources != null)
                {
                    foreach (Piece.Requirement q in r.m_resources)
                    {
                        if (q?.m_resItem == null)
                            continue;
                        res.Add(q.m_resItem.name + ":" + q.m_amount + ":" + q.m_amountPerLevel
                            + (q.m_upgraderResource ? ":idol" : ""));
                    }
                }

                RecipeFacts fx = FactsReader.Read(r);
                string category = Safe(() => SortEngine.CategoryOf(r).ToString());
                string bucket = Safe(() => GroupOf(r));

                sb.Append(r.m_craftingStation != null ? r.m_craftingStation.m_name : "(hand)").Append('\t')
                  .Append(r.m_repairStation != null ? r.m_repairStation.m_name : "").Append('\t')
                  .Append(r.m_minStationLevel).Append('\t')
                  .Append(r.m_item.name).Append('\t')
                  .Append(s.m_name).Append('\t')
                  .Append(s.m_itemType).Append('\t')
                  .Append(s.m_skillType).Append('\t')
                  .Append(s.m_maxQuality).Append('\t')
                  .Append(r.m_amount).Append('\t')
                  .Append(fx.Food).Append('\t')
                  .Append(fx.Stamina).Append('\t')
                  .Append(fx.Eitr).Append('\t')
                  .Append(s.m_armor).Append('\t')
                  .Append(s.m_damages.GetTotalDamage()).Append('\t')
                  .Append(s.m_blockPower).Append('\t')
                  .Append(s.m_weight).Append('\t')
                  .Append(string.Join(";", res)).Append('\t')
                  .Append(category).Append('\t')
                  .Append(bucket)
                  .Append('\t').Append(s.m_food).Append('\t').Append(s.m_foodStamina).Append('\t').Append(s.m_foodEitr)
                  .Append('\t').Append(fx.FoodFrom)
                  .Append('\t').Append(fx.AmmoType)
                  .AppendLine();
            }

            string path = Path.Combine(Paths.BepInExRootPath, "WorkbenchesPlus_recipes.tsv");
            File.WriteAllText(path, sb.ToString());
            Plugin.Log.LogInfo("Recipe dump written: " + path);
        }

        private static string GroupOf(Recipe r)
        {
            RecipeInfo info = SortEngine.Info(r);
            return info != null ? info.Group : "";
        }

        private static string Safe(System.Func<string> f)
        {
            try
            {
                return f() ?? "";
            }
            catch
            {
                return "?";
            }
        }
    }
}
