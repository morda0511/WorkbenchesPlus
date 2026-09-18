using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Groups tools by material tier: Antler / Bronze / Iron / BlackMetal, etc.
    /// Keys use a "Tool" prefix so they never collide with weapon/armor set ids.
    /// </summary>
    internal static class ToolSetDetector
    {
        private static readonly string[] MaterialStems =
        {
            "BlackMetal", "Flametal", "Carapace", "Crystal", "Bronze", "Copper", "Silver",
            "Iron", "Flint", "Chitin", "Bone", "Abyssal", "Needle", "FineWood", "Wood",
            "HardAntler", "Antler", "Obsidian", "Ancient", "Ashlands", "Asksvin"
        };

        private static readonly string[] ToolTypeTokens =
        {
            "Cultivator", "Pickaxe", "Hammer", "Scythe", "Hoe"
        };

        private static readonly Regex SharedCleanup = new Regex(
            @"^\$?(item[_-]?)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedToolTrim = new Regex(
            @"[_-]?(cultivator|pickaxe|hammer|scythe|hoe)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedToolPrefix = new Regex(
            @"^(cultivator|pickaxe|hammer|scythe|hoe)[_-]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SetKey(Recipe recipe, bool groupModded)
        {
            if (recipe == null || !IsTool(recipe))
                return null;

            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            string blob = (prefab + " " + shared).Trim();

            string mat = FromPrefab(prefab);
            if (string.IsNullOrEmpty(mat))
                mat = FromShared(shared);
            if (string.IsNullOrEmpty(mat))
                mat = FromKnownStem(blob);

            // Bare Hammer / Hoe / Cultivator → early wood tier
            if (string.IsNullOrEmpty(mat) && IsBareEarlyTool(prefab))
                mat = "Wood";

            if (string.IsNullOrEmpty(mat) || mat.Length < 3)
                return null;

            mat = CanonicalizeMaterial(mat);
            if (string.IsNullOrEmpty(mat))
                return null;

            if (!groupModded && !IsKnownMaterial(mat) && !LooksVanillaToolPrefab(prefab))
                return null;

            return "Tool" + mat;
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

            return orderTokens.Length + 10;
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

        public static bool IsTool(Recipe recipe)
        {
            if (recipe == null)
                return false;

            string prefab = PrefabName(recipe) ?? "";
            if (LooksToolPrefab(prefab))
                return true;

            ItemDrop.ItemData.ItemType t = GetItemType(recipe);
            return t == ItemDrop.ItemData.ItemType.Tool;
        }

        private static string[] DefaultOrder()
        {
            return new[] { "Hammer", "Hoe", "Cultivator", "Pickaxe", "Scythe", "Other" };
        }

        private static bool LooksToolPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return false;
            return prefab.IndexOf("Pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || prefab.IndexOf("Hammer", System.StringComparison.OrdinalIgnoreCase) >= 0
                || prefab.IndexOf("Hoe", System.StringComparison.OrdinalIgnoreCase) >= 0
                || prefab.IndexOf("Cultivator", System.StringComparison.OrdinalIgnoreCase) >= 0
                || prefab.IndexOf("Scythe", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBareEarlyTool(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return false;
            return prefab.Equals("Hammer", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Hoe", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Cultivator", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string FromPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;

            for (int i = 0; i < ToolTypeTokens.Length; i++)
            {
                string tok = ToolTypeTokens[i];
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
                string next = SharedToolTrim.Replace(s, "");
                next = SharedToolPrefix.Replace(next, "");
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
            if (s.Equals("Hardantler", System.StringComparison.OrdinalIgnoreCase))
                return "HardAntler";

            return s;
        }

        private static bool IsKnownMaterial(string mat)
        {
            for (int i = 0; i < MaterialStems.Length; i++)
            {
                if (mat.Equals(MaterialStems[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool LooksVanillaToolPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return false;
            for (int i = 0; i < ToolTypeTokens.Length; i++)
            {
                if (prefab.StartsWith(ToolTypeTokens[i], System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
    }
}
