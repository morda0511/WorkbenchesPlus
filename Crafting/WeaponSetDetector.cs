using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Groups weapons by material tier: Iron with Iron, BlackMetal with BlackMetal, etc.
    /// Keys use a "Weapon" prefix so they never collide with armor set ids.
    /// </summary>
    internal static class WeaponSetDetector
    {
        // Longer first (BlackMetal before Metal, Battleaxe before Axe, FineWood before Wood).
        private static readonly string[] MaterialStems =
        {
            "BlackMetal", "Flametal", "Carapace", "Crystal", "Bronze", "Copper", "Silver",
            "Iron", "Flint", "Chitin", "Bone", "Abyssal", "Needle", "FineWood", "Wood",
            "Ooze", "Bile", "Frost", "Poison", "Fire", "Obsidian", "Ancient", "Draugr",
            "Huntsman", "Fang", "Gold", "Bloodgold", "Blood", "Mist", "Ashlands", "Asksvin", "Himmin",
            "Berzerkr", "Jotun", "Skull", "SkollHati", "Eldner", "Demolisher", "Splitnir",
            "Krom", "Niedhogg", "Slayer"
        };

        private static readonly string[] WeaponTypeTokens =
        {
            "Battleaxe", "Crossbow", "Atgeir", "Sword", "Knife", "Spear", "Sledge",
            "Mace", "Axe", "Bow", "Arrow", "Bolt", "Club", "Bomb", "Torch", "Pickaxe"
        };

        private static readonly Regex SharedCleanup = new Regex(
            @"^\$?(item[_-]?)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedWeaponTrim = new Regex(
            @"[_-]?(battleaxe|crossbow|atgeir|sword|knife|spear|sledge|mace|axe|bow|arrow|bolt|club|bomb|torch|pickaxe)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SharedWeaponPrefix = new Regex(
            @"^(battleaxe|crossbow|atgeir|sword|knife|spear|sledge|mace|axe|bow|arrow|bolt|club|bomb)[_-]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string SetKey(Recipe recipe, bool groupModded)
        {
            if (recipe == null)
                return null;
            if (RecipeCategories.IsBait(recipe))
                return null;

            ItemDrop.ItemData.ItemType t = GetItemType(recipe);
            if (!IsWeaponType(t))
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

            // Always group vanilla-looking weapon materials; odd mod names need the toggle.
            if (!groupModded && !IsKnownMaterial(mat) && !LooksVanillaWeaponPrefab(prefab))
                return null;

            // Tools that share weapon item types should not sit in weapon material groups.
            if (LooksToolPrefab(prefab))
                return null;

            return "Weapon" + mat;
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

        public static bool IsAmmo(Recipe recipe)
        {
            if (recipe == null || RecipeCategories.IsBait(recipe))
                return false;

            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            return RecipeCategories.IsArrowOrBolt(prefab, shared);
        }

        public static int PieceOrder(Recipe recipe, string[] orderTokens)
        {
            string prefab = PrefabName(recipe) ?? "";
            string shared = SharedName(recipe) ?? "";
            string blob = prefab + " " + shared;

            // Prefer explicit Arrow/Bolt tokens before generic Ammo item-type fallback.
            for (int i = 0; i < orderTokens.Length; i++)
            {
                string token = orderTokens[i];
                if (string.IsNullOrEmpty(token) || token.Equals("Other", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (blob.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            ItemDrop.ItemData.ItemType t = GetItemType(recipe);
            if (t == ItemDrop.ItemData.ItemType.Ammo || t == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
            {
                int arrow = IndexOf(orderTokens, "Arrow");
                int bolt = IndexOf(orderTokens, "Bolt");
                return System.Math.Min(arrow, bolt);
            }
            if (t == ItemDrop.ItemData.ItemType.Bow)
                return IndexOf(orderTokens, "Bow");
            if (t == ItemDrop.ItemData.ItemType.Attach_Atgeir)
                return IndexOf(orderTokens, "Atgeir");

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

        private static string[] DefaultOrder()
        {
            // Arrows/bolts first — most used at the forge/workbench weapon list.
            return new[]
            {
                "Arrow", "Bolt", "Knife", "Sword", "Mace", "Axe", "Battleaxe", "Spear", "Atgeir",
                "Sledge", "Bow", "Crossbow", "Club", "Bomb", "Pickaxe", "Other"
            };
        }

        private static bool IsWeaponType(ItemDrop.ItemData.ItemType t)
        {
            return t == ItemDrop.ItemData.ItemType.OneHandedWeapon
                || t == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || t == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                || t == ItemDrop.ItemData.ItemType.Bow
                || t == ItemDrop.ItemData.ItemType.Attach_Atgeir
                || t == ItemDrop.ItemData.ItemType.Ammo
                || t == ItemDrop.ItemData.ItemType.AmmoNonEquipable;
        }

        private static string FromPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;

            // SwordIron / AxeBlackMetal / ArrowFire
            string name = prefab;
            for (int i = 0; i < WeaponTypeTokens.Length; i++)
            {
                string tok = WeaponTypeTokens[i];
                if (name.StartsWith(tok, System.StringComparison.OrdinalIgnoreCase)
                    && name.Length > tok.Length)
                {
                    return name.Substring(tok.Length);
                }
                if (name.EndsWith(tok, System.StringComparison.OrdinalIgnoreCase)
                    && name.Length > tok.Length + 2)
                {
                    return name.Substring(0, name.Length - tok.Length);
                }
            }

            return FromKnownStem(name);
        }

        private static string FromShared(string shared)
        {
            if (string.IsNullOrEmpty(shared))
                return null;

            string s = SharedCleanup.Replace(shared, "");
            for (int i = 0; i < 3; i++)
            {
                string next = SharedWeaponTrim.Replace(s, "");
                next = SharedWeaponPrefix.Replace(next, "");
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
                // black_metal → BlackMetal
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

            // Aliases
            if (s.Equals("Blackmetal", System.StringComparison.OrdinalIgnoreCase))
                return "BlackMetal";
            if (s.Equals("Finewood", System.StringComparison.OrdinalIgnoreCase))
                return "FineWood";
            if (s.Equals("Draugrfang", System.StringComparison.OrdinalIgnoreCase)
                || s.Equals("Fang", System.StringComparison.OrdinalIgnoreCase))
                return "Fang";

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

        private static bool LooksVanillaWeaponPrefab(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return false;
            for (int i = 0; i < WeaponTypeTokens.Length; i++)
            {
                if (prefab.StartsWith(WeaponTypeTokens[i], System.StringComparison.OrdinalIgnoreCase))
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
