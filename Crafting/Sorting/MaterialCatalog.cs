using System.Collections.Generic;

namespace WorkbenchesPlus
{
    /// <summary>
    /// What the ingredients of a recipe say about it. Tier = biome of the hardest ingredient
    /// (0 Meadows, 1 Black Forest, 2 Swamp, 3 Mountain, 4 Plains, 5 Mistlands, 6 Ashlands,
    /// 7 Deep North). Family = the "defining" material, e.g. Iron for an iron sword.
    /// Unknown ingredients (food, trophies, ...) are ignored.
    /// </summary>
    internal static class MaterialCatalog
    {
        public const int MaxTier = 7;

        private static readonly Dictionary<string, int> TierByIngredient = BuildTiers();

        // Progression order: the HIGHEST entry present in a recipe names its family.
        // Index = family order inside a tier.
        private static readonly string[][] Defining =
        {
            new[] { "Wood", "Wood" },
            new[] { "Stone", "Stone" },
            new[] { "Flint", "Flint" },
            new[] { "BjornPaw", "Bear" },
            new[] { "HardAntler", "Antler" },
            new[] { "FineWood", "Fine Wood" },
            new[] { "RoundLog", "Fine Wood" },
            new[] { "Copper", "Copper" },
            new[] { "Tin", "Bronze" },
            new[] { "Bronze", "Bronze" },
            new[] { "Chitin", "Chitin" },
            new[] { "Iron", "Iron" },
            new[] { "SerpentScale", "Serpent" },
            new[] { "Silver", "Silver" },
            new[] { "Crystal", "Crystal" },
            new[] { "YggdrasilWood", "Yggdrasil" },
            new[] { "BlackMetal", "Black Metal" },
            new[] { "Needle", "Needle" },
            new[] { "Carapace", "Carapace" },
            new[] { "CharredBone", "Charred" },
            new[] { "FlametalNew", "Flametal" },
            new[] { "Gold", "Bloodgold" },
        };

        public static int TierOf(string ingredient)
        {
            int t;
            return TierByIngredient.TryGetValue(ingredient, out t) ? t : -1;
        }

        /// <summary>Highest known ingredient tier of a recipe (0 when nothing is known).</summary>
        public static int ItemTier(RecipeFacts f)
        {
            int best = -1;
            for (int i = 0; i < f.Ingredients.Count; i++)
            {
                if (f.Ingredients[i].Idol)
                    continue;
                int t = TierOf(f.Ingredients[i].Prefab);
                if (t > best)
                    best = t;
            }
            return best < 0 ? 0 : best;
        }

        /// <summary>Family of the best defining ingredient; false when the recipe has none.</summary>
        public static bool TryFamily(RecipeFacts f, out string family, out int order, out int tier)
        {
            family = null;
            order = -1;
            tier = 0;
            for (int d = Defining.Length - 1; d >= 0; d--)
            {
                if (f.HasIngredient(Defining[d][0]))
                {
                    family = Defining[d][1];
                    order = FirstIndexOf(family);
                    tier = FamilyTier(family);
                    return true;
                }
            }
            return false;
        }

        private static int FirstIndexOf(string family)
        {
            for (int d = 0; d < Defining.Length; d++)
            {
                if (Defining[d][1] == family)
                    return d;
            }
            return Defining.Length;
        }

        /// <summary>Tier of the family = tier of its first (cheapest) ingredient.</summary>
        private static int FamilyTier(string family)
        {
            return TierOf(Defining[FirstIndexOf(family)][0]);
        }

        /// <summary>Wood / stone style families: a pricier extra ingredient moves the item out of them.</summary>
        public static bool IsBasicFamily(string family)
        {
            return family == "Wood" || family == "Stone" || family == "Flint" || family == "Antler"
                || family == "Fine Wood" || family == "Bear";
        }

        private static Dictionary<string, int> BuildTiers()
        {
            var m = new Dictionary<string, int>();
            Add(m, 0, "Wood", "Stone", "Flint", "LeatherScraps", "Leatherstraps", "DeerHide", "BoneFragments",
                "Resin", "Feathers", "Coal", "HardAntler");
            Add(m, 1, "FineWood", "RoundLog", "Copper", "Tin", "Bronze", "TrollHide", "BjornHide", "BjornPaw",
                "GreydwarfEye", "AncientSeed");
            Add(m, 2, "Iron", "ElderBark", "Root", "WrithanRoots", "Chitin", "SerpentScale", "Ooze", "Bloodbag",
                "YmirRemains", "SurtlingCore", "Chain", "Guck", "Entrails", "Wisp");
            Add(m, 3, "Silver", "Crystal", "Obsidian", "WolfPelt", "WolfFang", "WolfClaw", "WolfHairBundle",
                "FreezeGland", "Ice");
            Add(m, 4, "BlackMetal", "Needle", "LinenThread", "LoxPelt");
            Add(m, 5, "Carapace", "YggdrasilWood", "Eitr", "ScaleHide", "Mandible", "BlackMarble", "BlackCore",
                "Sap", "MushroomMagecap", "BugMeat", "Fiddleheadfern", "GemstoneRed", "GemstoneGreen",
                "GemstoneBlue", "DvergrKeyFragment", "BellFragment");
            Add(m, 6, "DyrnwynBladeFragment", "DyrnwynHiltFragment", "DyrnwynTipFragment", "FlametalNew", "CharredBone", "AskHide", "AskBladder", "Blackwood", "MorgenSinew",
                "MorgenHeart", "ProustitePowder", "SulfurStone", "Grausten", "MoltenCore", "FaderEmber",
                "BarkaBranch", "ElakingHairBundle");
            Add(m, 7, "Gold", "Frostwood", "MooseHide", "MooseSinew", "SealHide", "NornThread", "SealBlubber",
                "OrbFrostFire", "OrbThunderBlood", "CrownJewel");
            return m;
        }

        private static void Add(Dictionary<string, int> m, int tier, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                m[names[i]] = tier;
        }
    }
}
