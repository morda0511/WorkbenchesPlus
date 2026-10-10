# Sorting (v1.0.15 architecture)

One rule set for every station. A recipe is classified by **what it is**, not by its name.

```
Valheim Recipe
  -> FactsReader            (Crafting/Sorting/SortEngine.cs)   only place that touches Valheim types
  -> RecipeFacts            plain data: station, prefab, itemType, skill, food bars, ingredients
  -> RecipeClassifier       category (chip), group (header), order keys
  -> RecipeInfo             cached per recipe by SortEngine.Info(recipe)
  -> RecipeOrder.Compare    the one comparer
  -> RecipeSort / MaterialSectionHeaders / CategoryBar   (UI glue)
```

`CraftCategory`, `RecipeFacts`, `MaterialCatalog`, `RecipeInfo`, `RecipeClassifier` and `RecipeOrder`
contain no Valheim types, so they are compiled into `tools/SortPreview` as well.

## Order

1. craftable now (tier 0) before everything else (tier 1)
2. group: band (biome tier, or a fixed slot) -> order (material progression) -> label
3. inside the group: kind (ammo, weapon, shield, tool, armor, trinket) -> class (knife, sword, mace, axe,
   battleaxe, spear, atgeir, sledge, bow, crossbow, fist) -> piece (chest, helmet, legs, cape)
   -> food value (highest bar first) -> tier -> station level -> name -> variants after their base

## Rules (RecipeClassifier)

| Signal | Result |
|---|---|
| `itemType` Ammo / skill Bows / Crossbows | Ammo: Arrows, Bolts, Turret bolts (first in the list) |
| ingredient `FishingBait`, `Feast*_Material`, `*Uncooked` with food bars, `FishRaw` | Bait, Feasts, Prep, Fish |
| `MeadBase*` / `BarleyWineBase` | Health / Stamina / Eitr by name, else Potions |
| `Consumable` | Health / Stamina / Eitr = dominant bar (ties: Health, Stamina, Eitr) |
| `Material` + prefab `*Uncooked` without food | Cast (sorted behind the item it casts, found via its `Mold*` ingredient) |
| Helmet / Chest / Legs / Shoulder | Armor; header = set (`ArmorFenringChest` -> Fenrir); capes join an existing set |
| skill ElementalMagic / BloodMagic | Magic (Staffs) |
| `Tool`, `Torch`, skill Pickaxes / Farming, saddles | Tools |
| weapon types | Weapons; header = material of the best defining ingredient (Iron, Flametal, Bloodgold ...) |
| ingredient is another recipe's item (`AxeGold_FrostFire` needs `AxeGold`) | variant: sorted right behind its base |

Material tier and family come from the ingredient tables in `MaterialCatalog` (Meadows 0 ... Deep North 7).
Cheap families (Wood, Stone, Flint, Antler, Fine Wood, Bear) with a pricier extra ingredient go to *Special*.

## Check a change without starting the game

Set `DebugLogging = true`, start Valheim once in a world: `BepInEx/WorkbenchesPlus_recipes.tsv` is written.

```text
dotnet build tools/SortPreview -c Release
dotnet tools/SortPreview/bin/Release/net8.0/SortPreview.dll recipes.tsv [station] [category]
```

It prints every station exactly as the mod orders it (nothing craftable), with headers.
