using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WorkbenchesPlus
{
    /// <summary>
    /// Name-based material buckets for All / section headers.
    /// Exact vanilla prefab overrides first, then heuristics.
    /// </summary>
    internal static class MaterialNameBucket
    {
        // Longest / most specific first so BlackMetal wins over Metal, FineWood over Wood.
        private static readonly string[] Tokens =
        {
            "BlackMetal", "Blackmetal", "FineWood", "Finewood", "TrollLeather", "HardAntler",
            "Serpentscale", "Serpent", "Flametal", "Carapace", "Fenring", "Fenris", "Fenrir", "Vilebone", "VileBone",
            "Bronze", "Copper", "Silver", "Iron", "Flint", "Bone", "Leather", "Padded",
            "Antler", "Wood", "Root", "Ancient", "Abyssal", "Chitin", "Wolf", "Frost",
            "Crystal", "Obsidian", "Needle", "Eitr", "Mage",
            "Himmin", "Mist", "Dvergr", "Ashlands", "Asksvin", "Embla", "Gold", "Bloodgold", "Blood", "Bile",
            "Ooze", "Fire", "Poison", "Banded", "Rags", "Rag", "Berserker", "Berzerkr", "Lox"
        };

        /// <summary>
        /// Exact prefab → bucket. These are the items fuzzy matching always got wrong.
        /// </summary>
        private static readonly Dictionary<string, string> PrefabOverrides =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                // Wolf set
                { "HelmetDrake", "Wolf" },
                { "TrinketSilverDamage", "Wolf" }, // Wolf Sight

                // Bows (vanilla early)
                { "Bow", "Bows" },
                { "BowFineWood", "Bows" },
                { "BowHuntsman", "Bows" },
                { "BowDraugrFang", "Bows" },
                { "BowAshlands", "Bows" },
                { "BowAshlands_Blood", "Bows" },
                { "BowAshlands_Lightning", "Bows" },
                { "BowAshlands_Nature", "Bows" },
                { "BowSpineSnap", "Bows" },
                { "BowGold", "Bows" },
                { "BowGold_BloodLightning", "Bows" },
                { "BowGold_FrostFire", "Bows" },

                // Crossbows
                { "CrossbowArbalest", "Crossbows" },
                { "CrossbowRipper", "Crossbows" },
                { "CrossbowRipper_Blood", "Crossbows" },
                { "CrossbowRipper_Lightning", "Crossbows" },
                { "CrossbowRipper_Nature", "Crossbows" },
                { "CrossbowGold", "Crossbows" },
                { "CrossbowGold_BloodLightning", "Crossbows" },
                { "CrossbowGold_FrostFire", "Crossbows" },

                // Named Black Forge weapons
                { "AxeBerzerkr", "Berzerkr" },
                { "AxeBerzerkr_Blood", "Berzerkr" },
                { "AxeBerzerkr_Lightning", "Berzerkr" },
                { "AxeBerzerkr_Nature", "Berzerkr" },
                { "AxeJotunBane", "Jotun" },
                { "Battleaxe_SkullSplittur", "Skull" },
                { "KnifeSkollAndHati", "SkollHati" },
                { "MaceEldner", "Eldner" },
                { "MaceEldner_Blood", "Eldner" },
                { "MaceEldner_Lightning", "Eldner" },
                { "MaceEldner_Nature", "Eldner" },
                { "SledgeDemolisher", "Demolisher" },
                { "SpearSplitner", "Splitnir" },
                { "SpearSplitner_Blood", "Splitnir" },
                { "SpearSplitner_Lightning", "Splitnir" },
                { "SpearSplitner_Nature", "Splitnir" },
                { "SwordKrom", "Krom" },
                { "SwordNiedhogg", "Niedhogg" },
                { "SwordNiedhogg_Blood", "Niedhogg" },
                { "SwordNiedhogg_Lightning", "Niedhogg" },
                { "SwordNiedhogg_Nature", "Niedhogg" },
                { "SwordSlayer", "Slayer" },
                { "SwordSlayer_Blood", "Slayer" },
                { "SwordSlayer_Lightning", "Slayer" },
                { "SwordSlayer_Nature", "Slayer" },
                { "SwordMistwalker", "Mist" },
                { "SwordFire", "Fire" },
                { "AtgeirHimminAfl", "Himmin" },

                // Capes / utility (Black Forge)
                { "CapeAsh", "Cape" },
                { "CapeDeepNorth", "Cape" },
                { "CapeAsksvin", "Cape" },
                { "CapeDeepNorthMage", "Cape" },
                { "CapeFeather", "Cape" },
                { "HelmetCrownOfValheim", "Special" },
                { "Bell", "Materials" },
                { "GrapplingHook", "Tools" },
                { "Lantern", "Tools" },
                { "SnowShovel", "Tools" },
                { "SaddleAsksvin", "Tools" },
                { "SaddleMoose", "Tools" },

                // Iron (plain iron battleaxe only - not Wood / Black Metal / Crystal)
                { "Battleaxe", "Iron" },
                { "MaceNeedle", "Iron" }, // Porcupine
                { "BattleaxeWood", "Wood" },
                { "Club", "Wood" },
                { "SledgeStagbreaker", "Wood" },
                { "AxeStone", "Stone" },
                { "FistBjornClaw", "Bear" },

                // All shields share one Shields label
                { "ShieldWood", "Shields" },
                { "ShieldWoodTower", "Shields" },
                { "ShieldBoneTower", "Shields" },
                { "ShieldRoots", "Shields" },

                // Black metal (one key - dictionary is case-insensitive)
                { "BattleaxeBlackmetal", "BlackMetal" },
                { "Battleaxe_Blackmetal", "BlackMetal" },

                // Crystal
                { "TrinketSilverResist", "Crystal" }, // Crystal Heart
                { "BattleaxeCrystal", "Crystal" },
                { "Battleaxe_Crystal", "Crystal" },

                // Vilebone (forge - BerserkerUndead / Unbjorn)
                { "HelmetBerserkerUndead", "Vilebone" },
                { "ArmorBerserkerUndeadChest", "Vilebone" },
                { "ArmorBerserkerUndeadLegs", "Vilebone" },
                { "FistBjornUndeadClaw", "Vilebone" },
                { "FistUnbjornClaw", "Vilebone" },

                // Berserker (workbench - not Undead)
                { "HelmetBerserker", "Berserker" },
                { "ArmorBerserkerChest", "Berserker" },
                { "ArmorBerserkerLegs", "Berserker" },

                // Fenrir
                { "HelmetFenrir", "Fenrir" },
                { "ArmorFenrirChest", "Fenrir" },
                { "ArmorFenrirLegs", "Fenrir" },

                // Lox
                { "HelmetLox", "Lox" },
                { "ArmorLoxChest", "Lox" },
                { "ArmorLoxLegs", "Lox" },
                { "CapeLox", "Lox" },
                { "SadleLox", "Lox" },
                { "SaddleLox", "Lox" },

                // Capes (shared Cape label)
                { "CapeDeerHide", "Cape" },
                { "CapeLinen", "Cape" },
                { "CapeOdin", "Cape" },
                { "CapeTrollHide", "Cape" },
                { "CapeWolf", "Cape" },

                // Clothes / hats / dresses
                { "HelmetOdin", "Clothes" },
                { "HelmetCelebration", "Clothes" },
                { "HelmetFishingHat", "Clothes" },
                { "HelmetMidsummerCrown", "Clothes" },
                { "HelmetPointyHat", "Clothes" },
                { "HelmetStrawhat", "Clothes" },

                // Bombs
                { "BombOoze", "Bomb" },
                { "BombBile", "Bomb" },
                { "BombBlob_Poison", "Bomb" },
                { "BombBlob_PoisonElite", "Bomb" },
                { "BombBlob_Frost", "Bomb" },
                { "BombBlob_Lava", "Bomb" },
                { "BombBlob_Morkhalla", "Bomb" },
                { "BombBlob_Tar", "Bomb" },
                { "BombDynamite", "Bomb" },
                { "BombLava", "Bomb" },
                { "BombSiege", "Bomb" },
                { "BombSmoke", "Bomb" },

                // Fireworks
                { "FireworksBlue", "Firework" },
                { "FireworksCyan", "Firework" },
                { "FireworksGreen", "Firework" },
                { "FireworksPurple", "Firework" },
                { "FireworksRed", "Firework" },
                { "FireworksYellow", "Firework" },

                // Potions / skol / misc / furniture / tools
                { "PotionHealthMinor", "Potion" },
                { "PotionStaminaMinor", "Potion" },
                { "Tankard", "Skol" },
                { "TankardAnniversary", "Skol" },
                { "TankardOdin", "Skol" },
                { "Snowball", "Misc" },
                { "CatapultPayload_Grausten", "Misc" },
                { "AxeEarly", "Special" },
                { "Feaster", "Furniture" },
                { "Adze", "Furniture" },
                { "Chisel", "Tools" },
                { "Torch", "Tools" },

                // Galdr staffs → Staffs
                { "StaffFireball", "Staffs" },
                { "StaffIceShards", "Staffs" },
                { "StaffLightning", "Staffs" },
                { "StaffTroll", "Staffs" },
                { "StaffSkeleton", "Staffs" },
                { "StaffClusterbomb", "Staffs" },
                { "StaffShield", "Staffs" },
                { "StaffProtect", "Staffs" },
                { "StaffProtection", "Staffs" },
                { "StaffGreenRoots", "Staffs" },
                { "StaffNature", "Staffs" },
                { "StaffWild", "Staffs" },
            };

        private static readonly Regex SpaceyBlackMetal = new Regex(
            @"black\s*metal",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SpaceyFineWood = new Regex(
            @"fine\s*wood",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SpaceyTrollLeather = new Regex(
            @"troll\s*leather",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SpaceySerpent = new Regex(
            @"serpent\s*scale",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SpaceyVileBone = new Regex(
            @"vile\s*[_-]?\s*bone",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Resolve(Recipe recipe)
        {
            if (recipe == null)
                return null;

            if (RecipeCategories.IsBait(recipe))
                return "Bait";

            CraftCategory cat = RecipeCategories.Classify(recipe);
            if (cat == CraftCategory.Health)
                return "Health";
            if (cat == CraftCategory.Stamina)
                return "Stamina";
            if (cat == CraftCategory.Eitr)
                return "Eitr";
            if (cat == CraftCategory.Cast)
                return "Cast";
            if (cat == CraftCategory.Prep)
                return "Prep";
            if (cat == CraftCategory.Fish)
                return "Fish";
            if (cat == CraftCategory.Food)
                return null;
            if (cat == CraftCategory.Feasts)
                return "Feasts";
            if (cat == CraftCategory.Bait)
                return "Bait";
            if (cat == CraftCategory.Potions)
                return "Potion";
            if (cat == CraftCategory.Magic)
                return "Staffs";
            if (cat == CraftCategory.Trinkets)
                return "Trinkets";
            if (cat == CraftCategory.Clothes)
                return "Clothes";
            if (cat == CraftCategory.Materials)
                return "Materials";
            if (cat == CraftCategory.Tools)
                return "Tools";
            if (cat == CraftCategory.Ammo)
                return WeaponSetDetector.IsAmmo(recipe) ? "Arrows" : "Ammo";

            if (WeaponSetDetector.IsAmmo(recipe))
                return "Arrows";

            string prefab = StripClone(PrefabName(recipe) ?? "");

            // Exact prefab wins - Porcupine is MaceNeedle, Wolf Sight is TrinketSilverDamage, etc.
            string exact;
            if (!string.IsNullOrEmpty(prefab) && PrefabOverrides.TryGetValue(prefab, out exact))
                return exact;

            // Capes before Ashlands/token scan
            if (prefab.StartsWith("Cape", System.StringComparison.OrdinalIgnoreCase))
                return "Cape";

            // Bows / Crossbows before material tokens (Ashlands/Gold would steal them).
            if (prefab.StartsWith("Crossbow", System.StringComparison.OrdinalIgnoreCase))
                return "Crossbows";
            if (prefab.StartsWith("Bow", System.StringComparison.OrdinalIgnoreCase))
                return "Bows";

            // Named Black Forge uniques before Blood/Frost/Fire token noise.
            string named = NamedWeaponBucket(prefab);
            if (!string.IsNullOrEmpty(named))
                return named;

            // Finished Deep North bloodgold gear (Uncooked already Cast via Classify).
            if (ContainsAny(prefab, "Gold"))
                return "Bloodgold";

            // Prefix / family heuristics (before token scan so Fireworks ≠ Fire, BombOoze ≠ Ooze)
            string family = FromPrefabFamily(prefab);
            if (!string.IsNullOrEmpty(family))
                return family;

            // Any Staff* magic weapon → Staffs (Galdr table)
            if (prefab.StartsWith("Staff", System.StringComparison.OrdinalIgnoreCase))
                return "Staffs";

            string shared = SharedName(recipe) ?? "";
            string localized = TryLocalize(shared);
            string blob = prefab + " " + shared + " " + localized;

            string special = TrySpecialBucket(prefab, blob, localized);
            if (!string.IsNullOrEmpty(special))
                return special;

            if (IsVilebone(blob, localized, prefab))
                return "Vilebone";

            if (SpaceyVileBone.IsMatch(blob))
                return "Vilebone";

            if (SpaceyBlackMetal.IsMatch(blob) || ContainsAny(blob, "BlackMetal", "Blackmetal"))
                return "BlackMetal";

            if (SpaceyFineWood.IsMatch(blob))
                return "FineWood";

            if (SpaceyTrollLeather.IsMatch(blob))
                return "TrollLeather";

            if (SpaceySerpent.IsMatch(blob))
                return "Serpentscale";

            for (int i = 0; i < Tokens.Length; i++)
            {
                string tok = Tokens[i];
                if (blob.IndexOf(tok, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (tok.Equals("Bone", System.StringComparison.OrdinalIgnoreCase)
                    && SpaceyVileBone.IsMatch(blob))
                    return "Vilebone";

                // Do not let "Fire" inside Fireworks win (handled above); belt-and-braces:
                if (tok.Equals("Fire", System.StringComparison.OrdinalIgnoreCase)
                    && ContainsAny(blob, "Firework"))
                    return "Firework";

                return Canonical(tok);
            }

            return null;
        }

        private static string NamedWeaponBucket(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;
            if (ContainsAny(prefab, "Berzerkr", "Berserker") && !ContainsAny(prefab, "Undead"))
                return "Berzerkr";
            if (ContainsAny(prefab, "JotunBane", "Jotun"))
                return "Jotun";
            if (ContainsAny(prefab, "SkullSplittur", "SkullSplit"))
                return "Skull";
            if (ContainsAny(prefab, "SkollAndHati", "Skoll"))
                return "SkollHati";
            if (ContainsAny(prefab, "Eldner"))
                return "Eldner";
            if (ContainsAny(prefab, "Demolisher"))
                return "Demolisher";
            if (ContainsAny(prefab, "Splitner", "Splitnir"))
                return "Splitnir";
            if (ContainsAny(prefab, "SwordKrom") || prefab.Equals("SwordKrom", System.StringComparison.OrdinalIgnoreCase))
                return "Krom";
            if (ContainsAny(prefab, "Niedhogg"))
                return "Niedhogg";
            if (ContainsAny(prefab, "SwordSlayer") || (prefab.StartsWith("SwordSlayer", System.StringComparison.OrdinalIgnoreCase)))
                return "Slayer";
            if (ContainsAny(prefab, "Mistwalker"))
                return "Mist";
            if (prefab.Equals("SwordFire", System.StringComparison.OrdinalIgnoreCase))
                return "Fire";
            if (ContainsAny(prefab, "HimminAfl", "Himmin"))
                return "Himmin";
            return null;
        }

        private static string FromPrefabFamily(string prefab)
        {
            if (string.IsNullOrEmpty(prefab))
                return null;

            if (prefab.StartsWith("FishingBait", System.StringComparison.OrdinalIgnoreCase)
                || ContainsAny(prefab, "Bait"))
                return "Bait";

            if (prefab.StartsWith("Shield", System.StringComparison.OrdinalIgnoreCase))
                return "Shields";

            if (prefab.StartsWith("Bomb", System.StringComparison.OrdinalIgnoreCase))
                return "Bomb";
            if (prefab.StartsWith("Fireworks", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("Firework", System.StringComparison.OrdinalIgnoreCase))
                return "Firework";
            if (prefab.StartsWith("Potion", System.StringComparison.OrdinalIgnoreCase))
                return "Potion";
            if (prefab.StartsWith("Tankard", System.StringComparison.OrdinalIgnoreCase))
                return "Skol";

            if (prefab.StartsWith("ArmorDress", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("ArmorTunic", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("ArmorHarvester", System.StringComparison.OrdinalIgnoreCase)
                || prefab.StartsWith("HelmetHat", System.StringComparison.OrdinalIgnoreCase))
                return "Clothes";

            // Berserker undead → Vilebone; plain Berserker → Berserker
            if (ContainsAny(prefab, "BerserkerUndead"))
                return "Vilebone";
            if (ContainsAny(prefab, "Berserker") && !ContainsAny(prefab, "Undead"))
                return "Berserker";

            if (ContainsAny(prefab, "Fenrir"))
                return "Fenrir";

            if (ContainsAny(prefab, "Lox") || prefab.Equals("SadleLox", System.StringComparison.OrdinalIgnoreCase))
                return "Lox";

            return null;
        }

        public static int NestedToolTier(Recipe recipe)
        {
            if (recipe == null)
                return 5000;

            string prefab = StripClone(PrefabName(recipe) ?? "");
            string shared = SharedName(recipe) ?? "";
            string blob = prefab + " " + shared + " " + TryLocalize(shared);

            if (SpaceyBlackMetal.IsMatch(blob) || ContainsAny(blob, "BlackMetal", "Blackmetal"))
                return Tier("BlackMetal");

            if (SpaceyFineWood.IsMatch(blob))
                return Tier("FineWood");

            for (int i = 0; i < Tokens.Length; i++)
            {
                string tok = Tokens[i];
                if (blob.IndexOf(tok, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                string canon = Canonical(tok);
                if (canon.Equals("Vilebone", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                return Tier(canon);
            }

            if (ContainsAny(prefab, "Hammer", "Hoe", "Cultivator", "Torch", "Chisel")
                && !ContainsAny(prefab, "Iron", "Bronze", "Black"))
                return Tier("Wood");

            return 5000;
        }

        private static string TrySpecialBucket(string prefab, string blob, string localized)
        {
            string display = (localized ?? "") + " " + blob;

            if (IsVilebone(blob, localized, prefab))
                return "Vilebone";

            if (ContainsAny(display, "Wolf Sight", "Wolfsicht", "Wolfssicht"))
                return "Wolf";
            if (ContainsAny(display, "Crystal Heart", "Crystal Hearth", "Kristallherz", "Kristallherd"))
                return "Crystal";
            if (ContainsAny(display, "Porcupine", "Stachelschwein"))
                return "Iron";
            if (ContainsAny(display, "Drake Helmet", "Drachenhelm")
                || prefab.Equals("HelmetDrake", System.StringComparison.OrdinalIgnoreCase))
                return "Wolf";
            if (ContainsAny(display, "Huntsman", "Jägerbogen", "Jaegerbogen")
                || ContainsAny(display, "Draugr Fang", "Draugrzahn"))
                return "Bows";

            // Plain iron battleaxe only - Wood / Black Metal / Crystal stay out
            if (ContainsAny(display, "Battleaxe", "Streitaxt")
                && !ContainsAny(prefab, "Crystal", "Wood")
                && !ContainsAny(prefab, "BlackMetal", "Blackmetal")
                && !ContainsAny(display, "Black Metal", "Blackmetal", "Kristall", "Wood"))
                return "Iron";

            if (ContainsAny(display, "Staff of", "Dead raiser", "Dead Raiser", "Dundr", "Trollstav", "Troll staff")
                || ContainsAny(prefab, "Staff"))
                return "Staffs";

            if (ContainsAny(blob, "Pickaxe", "Cultivator", "Scythe", "ButcherKnife", "Butcher", "Torch", "Chisel")
                || prefab.Equals("Torch", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Hammer", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Hoe", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Chisel", System.StringComparison.OrdinalIgnoreCase))
                return "Tools";

            if (ContainsAny(blob, "Huntsman")
                || ContainsAny(blob, "DraugrFang", "Draugrfang")
                || (ContainsAny(blob, "Draugr") && ContainsAny(blob, "Fang") && ContainsAny(blob, "Bow"))
                || prefab.Equals("Bow", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("BowFineWood", System.StringComparison.OrdinalIgnoreCase))
                return "Bows";

            if (ContainsAny(blob, "Drake") && ContainsAny(blob, "Helmet", "Hood", "Hat"))
                return "Wolf";

            if (ContainsAny(blob, "WolfSight", "Wolfsight")
                || (ContainsAny(blob, "Wolf") && ContainsAny(blob, "Sight")))
                return "Wolf";

            if (ContainsAny(blob, "CrystalHearth", "CrystalHeart", "Crystal_Hearth", "Crystal_Heart")
                || (ContainsAny(blob, "Crystal") && ContainsAny(blob, "Hearth", "Heart")))
                return "Crystal";

            if (prefab.Equals("Battleaxe", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("MaceNeedle", System.StringComparison.OrdinalIgnoreCase))
                return "Iron";

            if (prefab.Equals("BattleaxeWood", System.StringComparison.OrdinalIgnoreCase))
                return "Wood";

            if (prefab.Equals("Adze", System.StringComparison.OrdinalIgnoreCase)
                || prefab.Equals("Feaster", System.StringComparison.OrdinalIgnoreCase))
                return "Furniture";

            if (prefab.Equals("Snowball", System.StringComparison.OrdinalIgnoreCase))
                return "Misc";

            return null;
        }

        private static bool IsVilebone(string blob, string localized, string prefab)
        {
            string p = (prefab ?? "").ToLowerInvariant();
            string display = ((localized ?? "") + " " + (blob ?? "") + " " + p).ToLowerInvariant();

            if (p.IndexOf("berserkerundead", System.StringComparison.Ordinal) >= 0
                || p.IndexOf("bjornundead", System.StringComparison.Ordinal) >= 0
                || p.IndexOf("unbjorn", System.StringComparison.Ordinal) >= 0
                || p.Equals("helmetberserkerundead", System.StringComparison.Ordinal)
                || p.Equals("armorberserkerundeadchest", System.StringComparison.Ordinal)
                || p.Equals("armorberserkerundeadlegs", System.StringComparison.Ordinal)
                || p.Equals("fistbjornundeadclaw", System.StringComparison.Ordinal)
                || p.Equals("fistunbjornclaw", System.StringComparison.Ordinal))
                return true;

            if (SpaceyVileBone.IsMatch(display) || display.IndexOf("vilebone", System.StringComparison.Ordinal) >= 0
                || display.IndexOf("vile_bone", System.StringComparison.Ordinal) >= 0
                || display.IndexOf("vile-bone", System.StringComparison.Ordinal) >= 0)
                return true;

            if (display.IndexOf("maulclaws", System.StringComparison.Ordinal) >= 0
                || display.IndexOf("maul claws", System.StringComparison.Ordinal) >= 0)
                return true;
            if (display.IndexOf("vilebone cage", System.StringComparison.Ordinal) >= 0
                || display.IndexOf("vilebone visage", System.StringComparison.Ordinal) >= 0
                || display.IndexOf("vilebone drap", System.StringComparison.Ordinal) >= 0)
                return true;

            return false;
        }

        public static int Tier(string bucket)
        {
            if (string.IsNullOrEmpty(bucket))
                return 10000;

            // Section blocks: Weapons → Shields → Tools → Armor → Cape → Bomb → Firework → Skol → Mist → Misc
            int section = SectionOf(bucket);
            int within = WithinSection(bucket);
            return section * 1000 + within;
        }

        /// <summary>
        /// 0 Weapons, 1 Shields, 2 Tools, 3 Armor, 4 Cape, 5 Bomb, 6 Firework, 7 Skol, 8 Mist, 9 Misc.
        /// </summary>
        public static int SectionOf(string bucket)
        {
            if (string.IsNullOrEmpty(bucket))
                return 0;
            if (bucket.Equals("Shields", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (bucket.Equals("Tools", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (IsArmorBucket(bucket))
                return 3;
            if (bucket.Equals("Cape", System.StringComparison.OrdinalIgnoreCase))
                return 4;
            if (bucket.Equals("Trinkets", System.StringComparison.OrdinalIgnoreCase))
                return 4;
            if (bucket.Equals("Bomb", System.StringComparison.OrdinalIgnoreCase))
                return 5;
            if (bucket.Equals("Firework", System.StringComparison.OrdinalIgnoreCase))
                return 6;
            if (bucket.Equals("Skol", System.StringComparison.OrdinalIgnoreCase))
                return 7;
            if (bucket.Equals("Mist", System.StringComparison.OrdinalIgnoreCase))
                return 8;
            if (bucket.Equals("Potion", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Bait", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Feasts", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Health", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Stamina", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Eitr", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Cast", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Prep", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Fish", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Materials", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Furniture", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Misc", System.StringComparison.OrdinalIgnoreCase))
                return 9;
            return 0; // weapons + unknown materials
        }

        private static bool IsArmorBucket(string bucket)
        {
            return bucket.Equals("Rags", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Leather", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("TrollLeather", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Root", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Berserker", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Fenrir", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Fenring", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Lox", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Clothes", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Wolf", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Vilebone", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Padded", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Carapace", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Mage", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Eitr", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Ashlands", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Asksvin", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Embla", System.StringComparison.OrdinalIgnoreCase);
        }

        private static int WithinSection(string bucket)
        {
            // Weapons
            if (bucket.Equals("Arrows", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Bows", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (bucket.Equals("Crossbows", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (bucket.Equals("Stone", System.StringComparison.OrdinalIgnoreCase))
                return 3;
            if (bucket.Equals("Wood", System.StringComparison.OrdinalIgnoreCase))
                return 4;
            if (bucket.Equals("Flint", System.StringComparison.OrdinalIgnoreCase))
                return 5;
            if (bucket.Equals("Chitin", System.StringComparison.OrdinalIgnoreCase))
                return 6;
            if (bucket.Equals("Carapace", System.StringComparison.OrdinalIgnoreCase))
                return 7;
            if (bucket.Equals("Flametal", System.StringComparison.OrdinalIgnoreCase))
                return 8;
            if (bucket.Equals("Ashlands", System.StringComparison.OrdinalIgnoreCase))
                return 9;
            if (bucket.Equals("Gold", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Bloodgold", System.StringComparison.OrdinalIgnoreCase))
                return 10;
            if (bucket.Equals("Berzerkr", System.StringComparison.OrdinalIgnoreCase))
                return 20;
            if (bucket.Equals("Jotun", System.StringComparison.OrdinalIgnoreCase))
                return 21;
            if (bucket.Equals("Skull", System.StringComparison.OrdinalIgnoreCase))
                return 22;
            if (bucket.Equals("SkollHati", System.StringComparison.OrdinalIgnoreCase))
                return 23;
            if (bucket.Equals("Eldner", System.StringComparison.OrdinalIgnoreCase))
                return 24;
            if (bucket.Equals("Demolisher", System.StringComparison.OrdinalIgnoreCase))
                return 25;
            if (bucket.Equals("Splitnir", System.StringComparison.OrdinalIgnoreCase))
                return 26;
            if (bucket.Equals("Krom", System.StringComparison.OrdinalIgnoreCase))
                return 27;
            if (bucket.Equals("Niedhogg", System.StringComparison.OrdinalIgnoreCase))
                return 28;
            if (bucket.Equals("Slayer", System.StringComparison.OrdinalIgnoreCase))
                return 29;
            if (bucket.Equals("Himmin", System.StringComparison.OrdinalIgnoreCase))
                return 30;
            if (bucket.Equals("Mist", System.StringComparison.OrdinalIgnoreCase))
                return 31;
            if (bucket.Equals("Fire", System.StringComparison.OrdinalIgnoreCase))
                return 32;
            if (bucket.Equals("Special", System.StringComparison.OrdinalIgnoreCase))
                return 40;
            if (bucket.Equals("Materials", System.StringComparison.OrdinalIgnoreCase))
                return 50;
            if (bucket.Equals("Bear", System.StringComparison.OrdinalIgnoreCase))
                return 41;
            if (bucket.Equals("Staffs", System.StringComparison.OrdinalIgnoreCase))
                return 80;

            // Shields - single label
            if (bucket.Equals("Shields", System.StringComparison.OrdinalIgnoreCase))
                return 0;

            // Tools
            if (bucket.Equals("Tools", System.StringComparison.OrdinalIgnoreCase))
                return 0;

            // Armor
            if (bucket.Equals("Rags", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Leather", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (bucket.Equals("TrollLeather", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (bucket.Equals("Root", System.StringComparison.OrdinalIgnoreCase))
                return 3;
            if (bucket.Equals("Berserker", System.StringComparison.OrdinalIgnoreCase))
                return 4;
            if (bucket.Equals("Fenrir", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Fenring", System.StringComparison.OrdinalIgnoreCase))
                return 5;
            if (bucket.Equals("Lox", System.StringComparison.OrdinalIgnoreCase))
                return 6;
            if (bucket.Equals("Wolf", System.StringComparison.OrdinalIgnoreCase))
                return 7;
            if (bucket.Equals("Vilebone", System.StringComparison.OrdinalIgnoreCase))
                return 8;
            if (bucket.Equals("Clothes", System.StringComparison.OrdinalIgnoreCase))
                return 9;

            // Utility tail
            if (bucket.Equals("Cape", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Bomb", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Firework", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Skol", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Mist", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Feasts", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Prep", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (bucket.Equals("Fish", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (bucket.Equals("Bait", System.StringComparison.OrdinalIgnoreCase))
                return 3;
            if (bucket.Equals("Health", System.StringComparison.OrdinalIgnoreCase))
                return 0;
            if (bucket.Equals("Stamina", System.StringComparison.OrdinalIgnoreCase))
                return 1;
            if (bucket.Equals("Eitr", System.StringComparison.OrdinalIgnoreCase))
                return 2;
            if (bucket.Equals("Cast", System.StringComparison.OrdinalIgnoreCase))
                return 6;
            if (bucket.Equals("Potion", System.StringComparison.OrdinalIgnoreCase))
                return 7;
            if (bucket.Equals("Furniture", System.StringComparison.OrdinalIgnoreCase))
                return 8;
            if (bucket.Equals("Misc", System.StringComparison.OrdinalIgnoreCase))
                return 9;

            // Other weapon/armor materials (Bronze, Iron, …) - biome progression
            return 100 + MaterialProgression.Tier("Weapon" + bucket);
        }

        public static string Label(string bucket)
        {
            if (string.IsNullOrEmpty(bucket))
                return null;
            if (bucket.Equals("Arrows", System.StringComparison.OrdinalIgnoreCase))
                return "Arrows";
            if (bucket.Equals("Ammo", System.StringComparison.OrdinalIgnoreCase))
                return "Ammo";
            if (bucket.Equals("Bait", System.StringComparison.OrdinalIgnoreCase))
                return "Bait";
            if (bucket.Equals("Health", System.StringComparison.OrdinalIgnoreCase))
                return "Health";
            if (bucket.Equals("Stamina", System.StringComparison.OrdinalIgnoreCase))
                return "Stamina";
            if (bucket.Equals("Eitr", System.StringComparison.OrdinalIgnoreCase))
                return "Eitr";
            if (bucket.Equals("Cast", System.StringComparison.OrdinalIgnoreCase))
                return "Cast";
            if (bucket.Equals("Prep", System.StringComparison.OrdinalIgnoreCase))
                return "Prep";
            if (bucket.Equals("Fish", System.StringComparison.OrdinalIgnoreCase))
                return "Fish";
            if (bucket.Equals("Trinkets", System.StringComparison.OrdinalIgnoreCase))
                return "Trinkets";
            if (bucket.Equals("Tools", System.StringComparison.OrdinalIgnoreCase))
                return "Tools";
            if (bucket.Equals("Bows", System.StringComparison.OrdinalIgnoreCase))
                return "Bows";
            if (bucket.Equals("Crossbows", System.StringComparison.OrdinalIgnoreCase))
                return "Crossbows";
            if (bucket.Equals("Staffs", System.StringComparison.OrdinalIgnoreCase))
                return "Staffs";
            if (bucket.Equals("Feasts", System.StringComparison.OrdinalIgnoreCase))
                return "Feasts";
            if (bucket.Equals("Gold", System.StringComparison.OrdinalIgnoreCase)
                || bucket.Equals("Bloodgold", System.StringComparison.OrdinalIgnoreCase))
                return "Bloodgold";
            if (bucket.Equals("Berzerkr", System.StringComparison.OrdinalIgnoreCase))
                return "Berzerkr";
            if (bucket.Equals("Materials", System.StringComparison.OrdinalIgnoreCase))
                return "Materials";
            if (bucket.Equals("Jotun", System.StringComparison.OrdinalIgnoreCase))
                return "Jotun";
            if (bucket.Equals("Skull", System.StringComparison.OrdinalIgnoreCase))
                return "Skull";
            if (bucket.Equals("SkollHati", System.StringComparison.OrdinalIgnoreCase))
                return "SkollHati";
            if (bucket.Equals("Eldner", System.StringComparison.OrdinalIgnoreCase))
                return "Eldner";
            if (bucket.Equals("Demolisher", System.StringComparison.OrdinalIgnoreCase))
                return "Demolisher";
            if (bucket.Equals("Splitnir", System.StringComparison.OrdinalIgnoreCase))
                return "Splitnir";
            if (bucket.Equals("Krom", System.StringComparison.OrdinalIgnoreCase))
                return "Krom";
            if (bucket.Equals("Niedhogg", System.StringComparison.OrdinalIgnoreCase))
                return "Niedhogg";
            if (bucket.Equals("Slayer", System.StringComparison.OrdinalIgnoreCase))
                return "Slayer";
            if (bucket.Equals("Shields", System.StringComparison.OrdinalIgnoreCase))
                return "Shields";
            if (bucket.Equals("Vilebone", System.StringComparison.OrdinalIgnoreCase))
                return "Vilebone";
            if (bucket.Equals("Bomb", System.StringComparison.OrdinalIgnoreCase))
                return "Bomb";
            if (bucket.Equals("Firework", System.StringComparison.OrdinalIgnoreCase))
                return "Firework";
            if (bucket.Equals("Potion", System.StringComparison.OrdinalIgnoreCase))
                return "Potion";
            if (bucket.Equals("Skol", System.StringComparison.OrdinalIgnoreCase))
                return "Skol";
            if (bucket.Equals("Cape", System.StringComparison.OrdinalIgnoreCase))
                return "Cape";
            if (bucket.Equals("Clothes", System.StringComparison.OrdinalIgnoreCase))
                return "Clothes";
            if (bucket.Equals("Furniture", System.StringComparison.OrdinalIgnoreCase))
                return "Furniture";
            if (bucket.Equals("Misc", System.StringComparison.OrdinalIgnoreCase))
                return "Misc";
            if (bucket.Equals("Special", System.StringComparison.OrdinalIgnoreCase))
                return "Special";
            if (bucket.Equals("Stone", System.StringComparison.OrdinalIgnoreCase))
                return "Stone";
            if (bucket.Equals("Bear", System.StringComparison.OrdinalIgnoreCase))
                return "Bear";
            if (bucket.Equals("Berserker", System.StringComparison.OrdinalIgnoreCase))
                return "Berserker";
            if (bucket.Equals("Fenrir", System.StringComparison.OrdinalIgnoreCase))
                return "Fenrir";
            if (bucket.Equals("Lox", System.StringComparison.OrdinalIgnoreCase))
                return "Lox";

            string name = SplitCamel(Canonical(bucket));
            return ToSentenceCase(name);
        }

        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            const string clone = "(Clone)";
            if (name.EndsWith(clone, System.StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - clone.Length).Trim();
            return name;
        }

        private static string TryLocalize(string token)
        {
            if (string.IsNullOrEmpty(token))
                return "";
            try
            {
                var t = System.Type.GetType("Localization, assembly_valheim")
                    ?? System.Type.GetType("Localization");
                if (t == null)
                    return token;
                var inst = t.GetProperty("instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                object loc = inst != null ? inst.GetValue(null, null) : null;
                if (loc == null)
                    return token;
                var m = t.GetMethod("Localize", new[] { typeof(string) });
                if (m == null)
                    return token;
                object r = m.Invoke(loc, new object[] { token });
                return r as string ?? token;
            }
            catch
            {
                return token;
            }
        }

        private static bool ContainsAny(string blob, params string[] needles)
        {
            if (string.IsNullOrEmpty(blob) || needles == null)
                return false;
            for (int i = 0; i < needles.Length; i++)
            {
                if (string.IsNullOrEmpty(needles[i]))
                    continue;
                if (blob.IndexOf(needles[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string ToSentenceCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            s = s.Trim().ToLowerInvariant();
            if (s.Length == 0)
                return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string Canonical(string tok)
        {
            if (tok.Equals("Blackmetal", System.StringComparison.OrdinalIgnoreCase))
                return "BlackMetal";
            if (tok.Equals("Finewood", System.StringComparison.OrdinalIgnoreCase))
                return "FineWood";
            if (tok.Equals("Banded", System.StringComparison.OrdinalIgnoreCase))
                return "Bronze";
            if (tok.Equals("Serpent", System.StringComparison.OrdinalIgnoreCase))
                return "Serpentscale";
            if (tok.Equals("Rag", System.StringComparison.OrdinalIgnoreCase))
                return "Rags";
            if (tok.Equals("Fenris", System.StringComparison.OrdinalIgnoreCase)
                || tok.Equals("Fenring", System.StringComparison.OrdinalIgnoreCase))
                return "Fenrir";
            if (tok.Equals("VileBone", System.StringComparison.OrdinalIgnoreCase)
                || tok.Equals("Vilebone", System.StringComparison.OrdinalIgnoreCase))
                return "Vilebone";

            if (tok.Length > 0 && char.IsLower(tok[0]))
                return char.ToUpperInvariant(tok[0]) + tok.Substring(1);
            return tok;
        }

        private static string SplitCamel(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            var sb = new System.Text.StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && char.IsLower(s[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
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
    }
}
