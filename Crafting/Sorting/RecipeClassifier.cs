using System;
using System.Collections.Generic;
using System.Text;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Facts -> category + header + order keys. No Valheim types and no station switch: a recipe is
    /// classified by what it is (item type, skill, ingredients, food bars), so every station is
    /// sorted by the same rules.
    /// </summary>
    internal static class RecipeClassifier
    {
        // Kind ranks inside a header.
        private const int KindAmmo = 0, KindWeapon = 1, KindShield = 2, KindTool = 3, KindArmor = 4, KindTrinket = 5, KindOther = 6;

        private static readonly HashSet<string> ClothesPrefabs = new HashSet<string>
        {
            "HelmetCelebration", "HelmetFishingHat", "HelmetOdin", "CapeOdin"
        };

        // Cape / odd helmet stems that belong to a set under another name.
        private static readonly Dictionary<string, string> SetAlias = new Dictionary<string, string>
        {
            { "DeerHide", "Leather" },
            { "TrollHide", "TrollLeather" },
            { "Drake", "Wolf" },
            { "DNHeavy", "DeepNorthHeavy" },
            { "DNMedium", "DeepNorthMedium" },
            { "DNMage", "DeepNorthMage" },
            { "DeepNorth", "DeepNorthMedium" },
            { "Ash", "AshlandsMedium" },
            { "Asksvin", "Mage_Ashlands" },
            { "Feather", "Mage" },
        };

        private static readonly Dictionary<string, string> SetLabel = new Dictionary<string, string>
        {
            { "Fenring", "Fenrir" },
            { "BerserkerUndead", "Vilebone" },
            { "AshlandsMedium", "Ashlands" },
            { "Mage_Ashlands", "Mage (Ashlands)" },
        };

        // Sets whose recipes only use cheap ingredients but unlock late.
        private static readonly Dictionary<string, int> SetTierOverride = new Dictionary<string, int>
        {
            { "AshlandsMedium", 6 },
            { "Mage_Ashlands", 6 },
        };

        public static List<RecipeInfo> Build(IList<RecipeFacts> all)
        {
            // Pass 1: sets that exist as real armor (so capes can join them).
            var sets = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                RecipeFacts f = all[i];
                if ((f.ItemType == "Chest" || f.ItemType == "Legs" || f.ItemType == "Helmet") && !IsClothes(f))
                    sets.Add(Alias(SetStem(f.Prefab)));
            }

            // Pass 2: classify.
            var infos = new List<RecipeInfo>(all.Count);
            for (int i = 0; i < all.Count; i++)
                infos.Add(Classify(all[i], sets));

            // Pass 3: all pieces of a set share one slot in the progression.
            var setTier = new Dictionary<string, int>();
            var setLevel = new Dictionary<string, int>();
            for (int i = 0; i < infos.Count; i++)
            {
                RecipeInfo n = infos[i];
                if (n.Category != CraftCategory.Armor || n.SetKey.Length == 0)
                    continue;
                int t;
                if (!setTier.TryGetValue(n.SetKey, out t) || n.ItemTier > t)
                    setTier[n.SetKey] = n.ItemTier;
                int lv;
                if (!setLevel.TryGetValue(n.SetKey, out lv) || n.Facts.MinLevel < lv)
                    setLevel[n.SetKey] = n.Facts.MinLevel;
            }
            for (int i = 0; i < infos.Count; i++)
            {
                RecipeInfo n = infos[i];
                if (n.Category != CraftCategory.Armor || n.SetKey.Length == 0)
                    continue;
                int o;
                n.GroupBand = SetTierOverride.TryGetValue(n.SetKey, out o) ? o : setTier[n.SetKey];
                n.GroupOrder = 500 + setLevel[n.SetKey];
            }

            // Pass 4: variants and casts follow the item they come from.
            ResolveAnchors(infos);
            return infos;
        }

        // ---- single recipe ------------------------------------------------------------------

        private static RecipeInfo Classify(RecipeFacts f, HashSet<string> sets)
        {
            var n = new RecipeInfo { Facts = f, SortName = f.Prefab, ItemTier = MaterialCatalog.ItemTier(f) };
            string p = f.Prefab;
            string t = f.ItemType;
            string skill = f.Skill;
            bool twoHanded = t == "TwoHandedWeapon" || t == "TwoHandedWeaponLeft";

            // Hand-picked oddities first.
            if (p.StartsWith("Tankard", StringComparison.Ordinal))
                return Fixed(n, CraftCategory.Misc, "Tankards", 90, KindOther);
            if (p.StartsWith("FireworksRocket", StringComparison.Ordinal))
                return Fixed(n, CraftCategory.Misc, "Fireworks", 91, KindOther);
            if (p == "Snowball")
                return Fixed(n, CraftCategory.Misc, "Misc", 92, KindOther);
            if (p == "BombSiege" || p.StartsWith("Catapult_", StringComparison.OrdinalIgnoreCase))
                return Fixed(n, CraftCategory.Ammo, "Siege", -1, KindAmmo);

            // Casts (moulds for Deep North gear, staff cores) and food intermediates.
            if (t == "Material" && p.EndsWith("Uncooked", StringComparison.Ordinal) && f.FoodTotal <= 0f)
                return Fixed(n, CraftCategory.Cast, "Cast", 85, KindOther);
            if (t == "Material" && p.StartsWith("Feast", StringComparison.Ordinal) && p.EndsWith("_Material", StringComparison.Ordinal))
            {
                n.ItemTier = Biome(p);
                return Fixed(n, CraftCategory.Feasts, "Feasts", 100, KindOther);
            }
            if (t == "Material" && f.FoodTotal > 0f)
            {
                if (p == "FishRaw")
                    return Fixed(n, CraftCategory.Fish, "Fish", 102, KindOther);
                n.Value = Dominant(f);
                return Fixed(n, CraftCategory.Prep, "Prep", 101, KindOther);
            }
            if (t == "Ammo" && f.HasIngredient("FishingBait"))
            {
                n.ItemTier = Biome(p);
                return Fixed(n, CraftCategory.Bait, "Bait", 103, KindOther);
            }

            // Mead bases and food.
            if (p.StartsWith("MeadBase", StringComparison.Ordinal) || p == "BarleyWineBase")
            {
                n.ItemTier = MeadStrength(p);
                CraftCategory c = p.Contains("Health") ? CraftCategory.Health
                    : p.Contains("Stamina") ? CraftCategory.Stamina
                    : p.Contains("Eitr") ? CraftCategory.Eitr
                    : CraftCategory.Potions;
                return Fixed(n, c, c.ToString(), FoodBand(c), KindOther);
            }
            if (t == "Consumable")
            {
                CraftCategory c = DominantBar(f);
                n.Value = Dominant(f);
                return Fixed(n, c, c.ToString(), FoodBand(c), KindOther);
            }

            // Gear.
            if (t == "Ammo" || t == "AmmoNonEquipable")
            {
                if (t == "AmmoNonEquipable")
                    return Fixed(n, CraftCategory.Ammo, "Turret bolts", -2, KindAmmo);
                // Bolt = what a crossbow fires. Some mods give bolts the Bows skill, so the ammo type decides.
                bool bolt = skill == "Crossbows"
                    || f.AmmoType.IndexOf("bolt", StringComparison.OrdinalIgnoreCase) >= 0
                    || p.StartsWith("Bolt", StringComparison.OrdinalIgnoreCase);
                return bolt
                    ? Fixed(n, CraftCategory.Ammo, "Bolts", -3, KindAmmo)
                    : Fixed(n, CraftCategory.Ammo, "Arrows", -4, KindAmmo);
            }

            if (t == "Shield")
            {
                n.ClassRank = p.Contains("Tower") ? 1 : 0;
                return Family(n, CraftCategory.Shields, KindShield);
            }

            if (t == "Helmet" || t == "Chest" || t == "Legs" || t == "Shoulder")
            {
                if (IsClothes(f))
                    return Fixed(n, CraftCategory.Clothes, "Clothes", 81, KindArmor);
                if (p == "HelmetCrownofValheim")
                    return Fixed(n, CraftCategory.Armor, "Special", 7, KindArmor);

                n.KindRank = KindArmor;
                n.Category = CraftCategory.Armor;
                n.PieceRank = t == "Chest" ? 0 : t == "Helmet" ? 1 : t == "Legs" ? 2 : 3;
                string stem = Alias(SetStem(p));
                if (t == "Shoulder" && !sets.Contains(stem))
                {
                    n.Group = "Capes";
                    n.GroupBand = n.ItemTier;
                    return n;
                }
                n.SetKey = stem;
                n.Group = Label(stem);
                n.GroupBand = n.ItemTier;
                return n;
            }

            if (t == "Trinket" || t == "Utility")
                return Fixed(n, CraftCategory.Trinkets, "Trinkets", 80, KindTrinket);

            if (skill == "ElementalMagic" || skill == "BloodMagic")
            {
                n.ClassRank = skill == "ElementalMagic" ? 0 : 1;
                return Fixed(n, CraftCategory.Magic, "Staffs", -1, KindWeapon);
            }

            if (t == "Tool" || t == "Torch" || skill == "Pickaxes" || skill == "Farming"
                || (t == "Bow" && skill == "None") || p == "KnifeButcher"
                || (t == "Misc" && p.StartsWith("Saddle", StringComparison.Ordinal)))
            {
                n.ClassRank = ToolRank(p);
                return Family(n, CraftCategory.Tools, KindTool);
            }

            if (t == "OneHandedWeapon" || t == "TwoHandedWeapon" || t == "TwoHandedWeaponLeft" || t == "Bow")
            {
                if (skill == "None" && p.StartsWith("Bomb", StringComparison.Ordinal))
                {
                    n.Subs = SubCategories.ForWeapon(t, skill, p);
                    n.ClassRank = 22;
                    return Fixed(n, CraftCategory.Weapons, "Bombs", 88, KindWeapon);
                }
                n.ClassRank = WeaponRank(skill, twoHanded);
                n.Subs = SubCategories.ForWeapon(t, skill, p);
                return Family(n, CraftCategory.Weapons, KindWeapon);
            }

            if (t == "Material")
                return Fixed(n, CraftCategory.Materials, "Materials", 86, KindOther);

            return Fixed(n, CraftCategory.Misc, "Misc", 93, KindOther);
        }

        private static RecipeInfo Fixed(RecipeInfo n, CraftCategory cat, string group, int band, int kind)
        {
            n.Category = cat;
            n.Group = group;
            n.GroupBand = band;
            n.KindRank = kind;
            return n;
        }

        /// <summary>Header = material of the best defining ingredient (Iron, Bronze, Flametal ...).</summary>
        private static RecipeInfo Family(RecipeInfo n, CraftCategory cat, int kind)
        {
            n.Category = cat;
            n.KindRank = kind;
            string family;
            int order;
            int tier;
            if (MaterialCatalog.TryFamily(n.Facts, out family, out order, out tier))
            {
                n.Group = family;
                n.GroupOrder = order;
                n.GroupBand = tier;
                if (MaterialCatalog.IsBasicFamily(family) && n.ItemTier > tier)
                {
                    n.Group = "Special";
                    n.GroupOrder = 500;
                    n.GroupBand = n.ItemTier;
                }
            }
            else
            {
                n.Group = "Special";
                n.GroupBand = n.ItemTier;
            }
            return n;
        }

        // ---- ranks ----------------------------------------------------------------------------

        private static int WeaponRank(string skill, bool twoHanded)
        {
            // Knife, Sword, Mace, Axe, Battleaxe, Spear, Atgeir, Sledge, Bow, Crossbow, Fist
            int hand = twoHanded ? 1 : 0;
            switch (skill)
            {
                case "Knives": return 0;
                case "Swords": return 2 + hand;
                case "Clubs": return twoHanded ? 14 : 4;
                case "Axes": return 6 + hand;
                case "Spears": return 8;
                case "Polearms": return 10;
                case "Bows": return 16;
                case "Crossbows": return 18;
                case "Unarmed": return 20;
                default: return 24;
            }
        }

        private static int ToolRank(string prefab)
        {
            if (prefab.StartsWith("Hammer", StringComparison.Ordinal)) return 0;
            if (prefab.StartsWith("Hoe", StringComparison.Ordinal)) return 1;
            if (prefab.StartsWith("Cultivator", StringComparison.Ordinal)) return 2;
            if (prefab.StartsWith("Pickaxe", StringComparison.Ordinal)) return 3;
            if (prefab.StartsWith("Scythe", StringComparison.Ordinal)) return 4;
            if (prefab.StartsWith("Shovel", StringComparison.Ordinal)) return 5;
            if (prefab.StartsWith("Torch", StringComparison.Ordinal) || prefab == "Lantern") return 6;
            if (prefab.StartsWith("Saddle", StringComparison.Ordinal)) return 8;
            return 7;
        }

        private static int FoodBand(CraftCategory c)
        {
            switch (c)
            {
                case CraftCategory.Health: return 0;
                case CraftCategory.Stamina: return 1;
                case CraftCategory.Eitr: return 2;
                default: return 3;
            }
        }

        private static CraftCategory DominantBar(RecipeFacts f)
        {
            // Ties prefer Health, then Stamina, then Eitr.
            if (f.Food >= f.Stamina && f.Food >= f.Eitr)
                return CraftCategory.Health;
            return f.Stamina >= f.Eitr ? CraftCategory.Stamina : CraftCategory.Eitr;
        }

        private static float Dominant(RecipeFacts f)
        {
            return Math.Max(f.Food, Math.Max(f.Stamina, f.Eitr));
        }

        private static int MeadStrength(string p)
        {
            if (p.EndsWith("Minor", StringComparison.Ordinal)) return 0;
            if (p.EndsWith("Medium", StringComparison.Ordinal)) return 1;
            if (p.EndsWith("Major", StringComparison.Ordinal)) return 2;
            if (p.EndsWith("Lingering", StringComparison.Ordinal)) return 3;
            return 0;
        }

        /// <summary>Biome order read from the prefab name (FeastSwamps_Material, FishingBaitCave ...).</summary>
        private static int Biome(string p)
        {
            string s = p.ToLowerInvariant();
            if (s.Contains("deepnorth")) return 8;
            if (s.Contains("ashlands")) return 7;
            if (s.Contains("mistlands")) return 6;
            if (s.Contains("ocean")) return 5;
            if (s.Contains("plains")) return 4;
            if (s.Contains("mountain") || s.Contains("cave")) return 3;
            if (s.Contains("swamp")) return 2;
            if (s.Contains("forest")) return 1;
            return 0;
        }

        // ---- armor sets -------------------------------------------------------------------------

        private static bool IsClothes(RecipeFacts f)
        {
            return ClothesPrefabs.Contains(f.Prefab);
        }

        /// <summary>ArmorFenringChest / HelmetFenring / CapeLox -> Fenring / Fenring / Lox.</summary>
        private static string SetStem(string prefab)
        {
            string s = prefab;
            foreach (string pre in new[] { "Armor", "Helmet", "Cape" })
            {
                if (s.StartsWith(pre, StringComparison.Ordinal) && s.Length > pre.Length)
                {
                    s = s.Substring(pre.Length);
                    break;
                }
            }

            bool ashlands = s.EndsWith("_Ashlands", StringComparison.Ordinal);
            if (ashlands)
                s = s.Substring(0, s.Length - "_Ashlands".Length);

            foreach (string suf in new[] { "Chest", "Legs", "legs", "Hood", "Cuirass", "Greaves" })
            {
                if (s.EndsWith(suf, StringComparison.Ordinal) && s.Length > suf.Length)
                {
                    s = s.Substring(0, s.Length - suf.Length);
                    break;
                }
            }
            return ashlands ? s + "_Ashlands" : s;
        }

        private static string Alias(string stem)
        {
            string a;
            return SetAlias.TryGetValue(stem, out a) ? a : stem;
        }

        private static string Label(string stem)
        {
            string l;
            if (SetLabel.TryGetValue(stem, out l))
                return l;

            var sb = new StringBuilder();
            for (int i = 0; i < stem.Length; i++)
            {
                char c = stem[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(stem[i - 1]))
                    sb.Append(' ');
                sb.Append(c == '_' ? ' ' : c);
            }
            return sb.ToString();
        }

        // ---- variants and casts -------------------------------------------------------------

        private static void ResolveAnchors(List<RecipeInfo> infos)
        {
            var byKey = new Dictionary<string, RecipeInfo>();
            var byMold = new Dictionary<string, RecipeInfo>();
            for (int i = 0; i < infos.Count; i++)
            {
                RecipeInfo n = infos[i];
                string key = n.Facts.Station + "|" + n.Facts.Prefab;
                if (!byKey.ContainsKey(key))
                    byKey[key] = n;

                if (n.Category == CraftCategory.Cast)
                    continue;
                for (int k = 0; k < n.Facts.Ingredients.Count; k++)
                {
                    string ing = n.Facts.Ingredients[k].Prefab;
                    if (ing.StartsWith("Mold", StringComparison.Ordinal) && !byMold.ContainsKey(ing))
                        byMold[ing] = n;
                }
            }

            for (int i = 0; i < infos.Count; i++)
            {
                RecipeInfo n = infos[i];
                if (n.Category == CraftCategory.Cast)
                    n.Root = FindProduct(n, byKey, byMold);
                else if (IsEquipment(n))
                    n.Root = FindBase(n, byKey);
            }

            // Flatten chains: Root always points at the very first item; Depth counts the steps.
            for (int i = 0; i < infos.Count; i++)
            {
                RecipeInfo n = infos[i];
                int depth = 0;
                RecipeInfo r = n.Root;
                while (r != null && r.Root != null && depth < 8)
                {
                    r = r.Root;
                    depth++;
                }
                if (n.Root != null)
                {
                    n.Root = r;
                    n.Depth = depth + 1;
                }
            }
        }

        private static bool IsEquipment(RecipeInfo n)
        {
            return n.Category == CraftCategory.Weapons || n.Category == CraftCategory.Shields
                || n.Category == CraftCategory.Tools || n.Category == CraftCategory.Armor
                || n.Category == CraftCategory.Magic;
        }

        private static RecipeInfo FindBase(RecipeInfo n, Dictionary<string, RecipeInfo> byKey)
        {
            for (int k = 0; k < n.Facts.Ingredients.Count; k++)
            {
                Ingredient ing = n.Facts.Ingredients[k];
                RecipeInfo other;
                if (ing.Idol || ing.Prefab == n.Facts.Prefab)
                    continue;
                if (byKey.TryGetValue(n.Facts.Station + "|" + ing.Prefab, out other) && IsEquipment(other) && other != n)
                    return other;
            }
            return null;
        }

        private static RecipeInfo FindProduct(RecipeInfo cast, Dictionary<string, RecipeInfo> byKey, Dictionary<string, RecipeInfo> byMold)
        {
            RecipeInfo p;
            string prefab = cast.Facts.Prefab;
            if (prefab.EndsWith("Uncooked", StringComparison.Ordinal))
            {
                string product = prefab.Substring(0, prefab.Length - "Uncooked".Length);
                if (byKey.TryGetValue(cast.Facts.Station + "|" + product, out p) && p != cast)
                    return p;
            }
            for (int k = 0; k < cast.Facts.Ingredients.Count; k++)
            {
                string ing = cast.Facts.Ingredients[k].Prefab;
                if (ing.StartsWith("Mold", StringComparison.Ordinal) && byMold.TryGetValue(ing, out p) && p != cast)
                    return p;
            }
            return null;
        }
    }
}
