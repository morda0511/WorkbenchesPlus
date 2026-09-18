using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Groups shields by material tier: Wood / Bronze / Iron / BlackMetal, etc.
    /// Keys use a "Shield" prefix so they never collide with weapon/armor/tool set ids.
    /// </summary>
    internal static class ShieldSetDetector
    {
        private static readonly string[] MaterialStems =
        {
            "BlackMetal", "Flametal", "Carapace", "Serpentscale", "Serpent", "Crystal",
            "Bronze", "Copper", "Silver", "Iron", "Banded", "Bone", "Chitin", "Abyssal",
            "FineWood", "Wood", "Ancient", "Ashlands", "Asksvin", "Flametal"
        };

        private static readonly string[] ShieldTypeTokens =
        {
            "TowerShield", "Shield"
        };

        private static readonly Regex SharedCleanup = new Regex(
            @"^\$?(item[_-]?)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedShieldTrim = new Regex(
            @"[_-]?(towershield|shield)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedShieldPrefix = new Regex(
            @"^(towershield|shield)[_-]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SetKey(Recipe recipe, bool groupModded)
        {
            if (recipe == null || !IsShield(recipe))
                return null;

            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            string blob = (prefab + " " + shared).Trim();

            string mat = FromPrefab(prefab);
            if (string.IsNullOrEmpty(mat))
                mat = FromShared(shared);
            if (string.IsNullOrEmpty(mat))
                mat = FromKnownStem(blob);

            if (string.IsNullOrEmpty(mat) || mat.Length < 3)
                return null;

            mat = CanonicalizeMaterial(mat);
            if (string.IsNullOrEmpty(mat))
                return null;

            // Banded is the early bronze-era round shield
            if (mat.Equals("Banded", System.StringComparison.OrdinalIgnoreCase))
                mat = "Bronze";

            if (!groupModded && !IsKnownMaterial(mat) && !LooksVanillaShieldPrefab(prefab))
                return null;

            return "Shield" + mat;
        }

        public static int PieceOrder(Recipe recipe, string[] orderTokens)
        {
            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            string blob = prefab + " " + shared;

            for (int i = 0; i < orderTokens.Length; i++)
            {
                string token = orderTokens[i];
                if (string.IsNullOrEmpty(token) || token.Equals("Other", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (blob.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            // Default: round shield before tower
            if (blob.IndexOf("Tower", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return IndexOf(orderTokens, "Tower") < orderTokens.Length
                    ? IndexOf(orderTokens, "Tower")
                    : orderTokens.Length + 5;

            return IndexOf(orderTokens, "Shield");
        }

        public static string[] ParseOrder(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return DefaultOrder();
            string[] parts = csv.Split(',');
            var list = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length > 0)
                    list.Add(p);
            }
            if (list.Count == 0)
                return DefaultOrder();
            return list.ToArray();
        }

        public static bool IsShield(Recipe recipe)
        {
            if (recipe == null)
                return false;

            ItemDrop.ItemData.ItemType t = GetItemType(recipe);
            if (t == ItemDrop.ItemData.ItemType.Shield)
                return true;

            string prefab = PrefabName(recipe) ?? "";
            return prefab.IndexOf("Shield", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string[] DefaultOrder()
        {
            return new[] { "Shield", "Tower", "Other" };
        }

        private static string FromPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;

            // ShieldBronze / TowerShieldIron
            for (int i = 0; i < ShieldTypeTokens.Length; i++)
            {
                string tok = ShieldTypeTokens[i];
                if (prefab.StartsWith(tok, System.StringComparison.OrdinalIgnoreCase)
                    && prefab.Length > tok.Length)
                    return prefab.Substring(tok.Length);
                if (prefab.EndsWith(tok, System.StringComparison.OrdinalIgnoreCase)
                    && prefab.Length > tok.Length + 2)
                    return prefab.Substring(0, prefab.Length - tok.Length);
            }

            return FromKnownStem(prefab);
        }

        private static string FromShared(string shared)
        {
            if (string.IsNullOrEmpty(shared))
                return null;

            string s = SharedCleanup.Replace(shared, "");
            for (int i = 0; i < 3; i++)
            {
                string next = SharedShieldTrim.Replace(s, "");
                next = SharedShieldPrefix.Replace(next, "");
                next = next.Trim('_', '-', ' ');
                if (next == s)
                    break;
                s = next;
            }
            if (s.Length < 3)
                return null;
            return s;
        }

        private static string FromKnownStem(string blob)
        {
            if (string.IsNullOrEmpty(blob))
                return null;
            for (int i = 0; i < MaterialStems.Length; i++)
            {
                if (blob.IndexOf(MaterialStems[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return MaterialStems[i];
            }
            return null;
        }

        private static string CanonicalizeMaterial(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string s = raw.Replace("-", "_");
            if (s.IndexOf('_') >= 0)
            {
                string[] parts = s.Split('_');
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < parts.Length; i++)
                {
                    string p = parts[i];
                    if (string.IsNullOrEmpty(p))
                        continue;
                    sb.Append(char.ToUpperInvariant(p[0]));
                    if (p.Length > 1)
                        sb.Append(p.Substring(1));
                }
                s = sb.ToString();
            }

            if (s.Length > 0 && char.IsLower(s[0]))
                s = char.ToUpperInvariant(s[0]) + s.Substring(1);

            if (s.Equals("Blackmetal", System.StringComparison.OrdinalIgnoreCase))
                return "BlackMetal";
            if (s.Equals("Finewood", System.StringComparison.OrdinalIgnoreCase))
                return "FineWood";
            if (s.Equals("Serpentscale", System.StringComparison.OrdinalIgnoreCase)
                || s.Equals("Serpent", System.StringComparison.OrdinalIgnoreCase))
                return "Serpentscale";

            return s;
        }

        private static bool IsKnownMaterial(string mat)
        {
            for (int i = 0; i < MaterialStems.Length; i++)
            {
                if (mat.Equals(MaterialStems[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return mat.Equals("Bronze", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksVanillaShieldPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return false;
            return prefab.StartsWith("Shield", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("TowerShield", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string PrefabName(Recipe recipe)
        {
            if (recipe?.m_item == null)
                return null;
            string n = recipe.m_item.name;
            if (!string.IsNullOrEmpty(n))
                return n;
            return recipe.m_item.gameObject != null ? recipe.m_item.gameObject.name : null;
        }

        private static string SharedName(Recipe recipe)
        {
            try
            {
                return recipe?.m_item?.m_itemData?.m_shared?.m_name;
            }
            catch
            {
                return null;
            }
        }

        private static ItemDrop.ItemData.ItemType GetItemType(Recipe recipe)
        {
            try
            {
                if (recipe?.m_item?.m_itemData?.m_shared != null)
                    return recipe.m_item.m_itemData.m_shared.m_itemType;
            }
            catch
            {
            }
            return ItemDrop.ItemData.ItemType.None;
        }

        private static int IndexOf(string[] order, string token)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i].Equals(token, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return order.Length + 5;
        }
    }
}
