using System.Collections.Generic;
using System.Globalization;

namespace WorkbenchesPlus
{
    internal sealed class Ingredient
    {
        public string Prefab;
        public int Amount;
        public int PerLevel;
        public bool Idol;
    }

    /// <summary>
    /// Everything the sorter knows about a recipe, as plain data (no Valheim types), so the whole
    /// sorting logic runs in a normal console test against the recipe dump.
    /// </summary>
    internal sealed class RecipeFacts
    {
        public string Station = "";
        public int MinLevel;
        public string Prefab = "";
        public string Name = "";        // localization token (shared.m_name)
        public string ItemType = "";    // ItemDrop.ItemData.ItemType as text
        public string Skill = "";       // Skills.SkillType as text
        public int MaxQuality;
        public int Amount = 1;
        public float Food;
        public float Stamina;
        public float Eitr;
        public string FoodFrom = "";   // prefab whose food values are used (dough -> bread), empty = own
        public string AmmoType = "";   // shared.m_ammoType ("$ammo_arrows", "$ammo_bolts" ...): which weapon fires it
        public readonly List<Ingredient> Ingredients = new List<Ingredient>();

        public float FoodTotal => Food + Stamina + Eitr;

        public bool HasIngredient(string prefab)
        {
            for (int i = 0; i < Ingredients.Count; i++)
            {
                if (Ingredients[i].Prefab == prefab)
                    return true;
            }
            return false;
        }

        // ---- TSV (RecipeDump format) -------------------------------------------------------
        // station repairStation minLevel prefab name itemType skill maxQuality amount food stamina
        // eitr armor damage block weight resources category materialBucket
        public static RecipeFacts FromTsv(string line)
        {
            string[] c = line.Split('\t');
            if (c.Length < 17)
                return null;

            var f = new RecipeFacts
            {
                Station = c[0],
                MinLevel = ToInt(c[2]),
                Prefab = c[3],
                Name = c[4],
                ItemType = c[5],
                Skill = c[6],
                MaxQuality = ToInt(c[7]),
                Amount = ToInt(c[8]),
                Food = ToFloat(c[9]),
                Stamina = ToFloat(c[10]),
                Eitr = ToFloat(c[11]),
                AmmoType = c.Length > 23 ? c[23] : ""
            };

            foreach (string part in c[16].Split(';'))
            {
                if (part.Length == 0)
                    continue;
                string[] p = part.Split(':');
                f.Ingredients.Add(new Ingredient
                {
                    Prefab = p[0],
                    Amount = p.Length > 1 ? ToInt(p[1]) : 0,
                    PerLevel = p.Length > 2 ? ToInt(p[2]) : 0,
                    Idol = p.Length > 3 && p[3] == "idol"
                });
            }
            return f;
        }

        private static int ToInt(string s)
        {
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
        }

        private static float ToFloat(string s)
        {
            float v;
            return float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
        }
    }
}
