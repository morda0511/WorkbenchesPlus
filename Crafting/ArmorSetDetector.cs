using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Derives a stable set id for ANY armor set (vanilla + modded), so Helmet/Chest/Legs/Cape
    /// from the same set always share one key and sort as one block.
    /// </summary>
    internal static class ArmorSetDetector
    {
        private static readonly string[] StripSuffixes =
        {
            "Helmet", "Hood", "Hat", "Circlet", "Crown",
            "Chest", "Jacket", "Tunic", "Cuirass", "Armor", // Armor last as suffix only
            "Legs", "Greaves", "Pants", "Trousers", "Skirt",
            "Cape", "Cloak", "Mantle", "Shoulder"
        };

        // Longer first so TrollLeather wins over Leather, etc. when matching shared stems.
        private static readonly string[] KnownSetStems =
        {
            "TrollLeather", "Carapace", "Ashlands", "Fenring", "Fenris",
            "Padded", "Bronze", "Iron", "Wolf", "Root", "Mage", "Eitr",
            "Leather", "Rags", "Rag", "Himmin", "Asksvin", "Embla", "Flametal"
        };

        private static readonly Regex SharedCleanup = new Regex(
            @"^\$?(item[_-]?)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedPieceTrim = new Regex(
            @"[_-]?(helmet|hood|hat|circlet|crown|chest|jacket|tunic|cuirass|legs|greaves|pants|trousers|skirt|cape|cloak|mantle|shoulder|armor)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedPiecePrefix = new Regex(
            @"^(helmet|hood|hat|chest|legs|cape|armor|trousers|tunic)[_-]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SetKey(Recipe recipe, bool groupModded)
        {
            if (recipe == null)
                return null;

            ItemDrop.ItemData.ItemType itemType = GetItemType(recipe);
            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            string blob = (prefab + " " + shared).Trim();

            if (!IsArmorPiece(itemType, prefab, shared))
                return null;

            // Prefab → shared token → known stem in prefab/shared ($item_leather_chest etc.)
            string root = FromPrefab(prefab);
            if (string.IsNullOrEmpty(root))
                root = FromShared(shared);
            if (string.IsNullOrEmpty(root))
                root = FromKnownStem(blob);

            root = Canonicalize(root);
            if (string.IsNullOrEmpty(root) || root.Length < 4)
                return null;

            // Any real armor slot always groups (leather, bronze, iron, troll, …).
            if (IsSlotType(itemType) || LooksLikeArmorName(blob))
                return root;

            if (!groupModded && !root.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase)
                && !root.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase))
                return null;

            return root;
        }

        private static bool LooksLikeArmorName(string blob)
        {
            if (string.IsNullOrEmpty(blob))
                return false;
            return blob.IndexOf("armor", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("leather", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("troll", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("bronze", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("padded", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("carapace", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("fenring", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("fenris", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("rag", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("cape", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("helmet", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("tunic", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("trousers", System.StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("greaves", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSlotType(ItemDrop.ItemData.ItemType t)
        {
            return t == ItemDrop.ItemData.ItemType.Helmet
                || t == ItemDrop.ItemData.ItemType.Chest
                || t == ItemDrop.ItemData.ItemType.Legs
                || t == ItemDrop.ItemData.ItemType.Shoulder;
        }

        private static bool IsArmorPiece(ItemDrop.ItemData.ItemType t, string prefab, string shared)
        {
            if (IsSlotType(t))
                return true;

            string blob = (prefab ?? "") + " " + (shared ?? "");
            for (int i = 0; i < StripSuffixes.Length; i++)
            {
                if (blob.IndexOf(StripSuffixes[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return prefab.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("Helmet", System.StringComparison.OrdinalIgnoreCase);
        }

        private static string FromPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;

            if (prefab.Equals("HelmetDrake", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorWolf";

            if (prefab.Equals("HelmetBerserkerUndead", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("ArmorBerserkerUndeadChest", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("ArmorBerserkerUndeadLegs", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorVilebone";

            // HelmetBronze → ArmorBronze (set helmets). Standalone hats keep Helmet*
            if (prefab.StartsWith("Helmet", System.StringComparison.OrdinalIgnoreCase) && prefab.Length > 6)
            {
                string rest = prefab.Substring(6);
                if (IsStandaloneHelmet(rest))
                    return "Helmet" + rest;
                return "Armor" + rest;
            }

            if (prefab.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase) && prefab.Length > 4)
            {
                string rest = prefab.Substring(4);
                if (IsSetCape(rest))
                    return "Armor" + rest;
                return "Cape" + rest;
            }

            // ArmorIronChest / ArmorTrollLeatherLegs → strip piece suffix(es)
            string name = prefab;
            bool stripped = false;
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < StripSuffixes.Length; i++)
                {
                    string s = StripSuffixes[i];
                    // Don't strip leading "Armor" as if it were a piece suffix of a short name
                    if (s.Equals("Armor", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (name.EndsWith(s, System.StringComparison.OrdinalIgnoreCase)
                        && name.Length > s.Length + 3)
                    {
                        name = name.Substring(0, name.Length - s.Length);
                        stripped = true;
                        break;
                    }
                }
            }

            if (!stripped && !name.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase))
                return null;

            return name;
        }

        private static string FromShared(string shared)
        {
            if (string.IsNullOrEmpty(shared))
                return null;

            string s = SharedCleanup.Replace(shared, "");
            // Keep peeling piece tokens from both ends
            for (int i = 0; i < 3; i++)
            {
                string next = SharedPieceTrim.Replace(s, "");
                next = SharedPiecePrefix.Replace(next, "");
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
            for (int i = 0; i < KnownSetStems.Length; i++)
            {
                if (blob.IndexOf(KnownSetStems[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Armor" + KnownSetStems[i];
            }
            return null;
        }

        private static string Canonicalize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return null;

            string s = raw.Replace("-", "_");
            if (s.IndexOf('_') >= 0)
            {
                string[] parts = s.Split('_');
                var sb = new StringBuilder();
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

            if (!s.StartsWith("Armor", System.StringComparison.OrdinalIgnoreCase)
                && !s.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase)
                && !s.StartsWith("Helmet", System.StringComparison.OrdinalIgnoreCase))
                s = "Armor" + s;

            if (s.StartsWith("ArmorArmor", System.StringComparison.OrdinalIgnoreCase))
                s = "Armor" + s.Substring("ArmorArmor".Length);

            // Aliases so every piece of a set lands on the same id
            if (s.Equals("ArmorRag", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorRags";
            if (s.Equals("ArmorTroll", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorTrollLeather";
            if (s.Equals("ArmorFenris", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorFenring";
            if (s.Equals("ArmorPadd", System.StringComparison.OrdinalIgnoreCase))
                return "ArmorPadded";

            return s;
        }

        private static bool IsStandaloneHelmet(string rest)
        {
            // Drake is grouped with Wolf — not a standalone set.
            return rest.Equals("Yule", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Dverger", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Fisherman", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Hat", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("PointyHat", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSetCape(string rest)
        {
            return rest.Equals("Wolf", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Troll", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Lox", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Feather", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Asksvin", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Carapace", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Ashlands", System.StringComparison.OrdinalIgnoreCase)
                || rest.Equals("Fenring", System.StringComparison.OrdinalIgnoreCase);
        }

        public static int PieceOrder(Recipe recipe, string[] orderTokens)
        {
            ItemDrop.ItemData.ItemType t = GetItemType(recipe);
            if (t == ItemDrop.ItemData.ItemType.Chest)
                return IndexOf(orderTokens, "Chest");
            if (t == ItemDrop.ItemData.ItemType.Helmet)
                return IndexOf(orderTokens, "Helmet");
            if (t == ItemDrop.ItemData.ItemType.Legs)
                return IndexOf(orderTokens, "Legs");
            if (t == ItemDrop.ItemData.ItemType.Shoulder)
                return IndexOf(orderTokens, "Cape");

            string prefab = PrefabName(recipe) ?? "";
            for (int i = 0; i < orderTokens.Length; i++)
            {
                string token = orderTokens[i];
                if (string.IsNullOrEmpty(token) || token.Equals("Other", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (prefab.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return orderTokens.Length + 10;
        }

        public static string[] ParseOrder(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return new[] { "Chest", "Helmet", "Legs", "Cape", "Other" };
            string[] parts = csv.Split(',');
            var list = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length > 0)
                    list.Add(p);
            }
            if (list.Count == 0)
                list.AddRange(new[] { "Chest", "Helmet", "Legs", "Cape", "Other" });
            return list.ToArray();
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
