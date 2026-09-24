# Workbenches+

**Crafting menus that stay readable.**  
Sort and filter station recipes so craftable items rise first, categories sit under the repair button, and armor sets and weapon materials stay together in biome order. Dismantle station-crafted items back into their materials from a tab beside Craft / Upgrade. Does not change recipes or crafting logic.

**Client-side** · Valheim 1.0 · BepInExPack 5.4.2350+. Console players via crossplay cannot load the mod.

**Bug reports:** https://github.com/morda0511/WorkbenchesPlus/issues

**Discord:** https://discord.gg/aVKVVmyzj

---

## Features

### Dismantle

- New tab beside Craft / Upgrade on workbenches.
- Break station-crafted items back into their crafting materials.

### Material labels

- Materials are sorted by labels.

### Category chips

- Chips under the repair button — **per station** (e.g. cauldron: Health / Stamina / Eitr; prep table: Feasts / Prep / Bait; galdr: Magic / Cast).
- Empty chips stay hidden. Fishing baits and feasts are separate from Weapons.
- **All** shows recipes for the current station only. The Forge of Potential (Schmiede / Werkbank des Potenzials) uses the vanilla upgrade list (no extra filters).

### Armor set grouping

- Helmet, chest, legs, and cape from the same set stay together.
- Piece order is configurable.

### Weapon material grouping

- Weapons of the same material stay together (Iron with Iron, Black Metal with Black Metal).
- Groups follow biome / material tier order (Copper, Bronze, Iron, …).

### Sort modes

- `CraftableFirst`, `CategoryThenCraftable`, `Progression`, `Alphabetical`, `Vanilla`.

### Craft multiplier

- Up / down arrows beside the Craft button set how many crafts to run.
- Caps at `MaxCraftMultiplier` (default 99).

### Build range

- Look at a station that has a hammer/build radius (workbench, stonecutter, artisan table, …).
- **Shift+E** (vanilla AltPlace + Use) adds **+50 m**, then +100, +150, **+200 m**, then back to default.
- Saved on that station. Hover shows the current range.

---

## Config

**One file only** (no YAML):

`BepInEx/config/com.morda.workbenchesplus.cfg`

| Setting | Meaning |
|---|---|
| `EnableMod` | Master switch |
| `EnableCategories` | Category chips |
| `EnableCraftableFirst` | Prioritize craftable recipes |
| `EnableArmorSetGrouping` | Keep armor sets together |
| `EnableWeaponSetGrouping` | Keep weapons by material together |
| `SortMode` | CraftableFirst / CategoryThenCraftable / Progression / Alphabetical / Vanilla |
| `ArmorPieceOrder` | Order inside an armor set |
| `WeaponPieceOrder` | Order inside a weapon material group |
| `GroupModdedArmorSets` | Heuristics for modded armor |
| `GroupModdedWeaponSets` | Heuristics for modded weapons |
| `RefreshOnInventoryChange` | Re-sort when inventory changes |
| `RefreshOnStationChange` | Reset category to All when switching stations |
| `ShowCraftabilityIndicators` | Green check on fully craftable rows |
| `EnableCraftMultiplier` | Multi-craft arrows beside Craft |
| `MaxCraftMultiplier` | Max multi-craft amount |
| `EnableDismantle` | Dismantle tab beside Craft / Upgrade |
| `EnableBuildRangeCycle` | Shift+E on a build-radius station: +50 m up to +200 m, then default |
| `DebugLogging` | Extra logs |

---

## Install

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Drop `WorkbenchesPlus.dll` into `BepInEx/plugins/WorkbenchesPlus/` (or install via Thunderstore / r2modman).
3. Launch once to generate the config file.
