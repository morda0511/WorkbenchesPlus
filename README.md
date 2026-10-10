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

### Search and favorites

- Search field at the right end of the tab row: type to filter the list by recipe name or header (Iron, Fenrir, ...). Esc or Enter leaves the field.
- **Right-click** a recipe to add or remove it from your favorites. Favorites form a "Favorites" block at the top of every list. They are saved in `BepInEx/config/WorkbenchesPlus_favorites.txt`.

### Biome tags and food values

- Gear headers carry the biome of their material, for example `Iron [Swamp]`, `Black Metal [Plains]`.
- Food shows its strongest bar after the name: Health red, Stamina yellow, Eitr blue.

### Material labels

- Materials are sorted by labels.

### Category chips

- Chips under the repair button, **per station** (cauldron: Health / Stamina / Eitr; prep table: Feasts / Prep / Bait; galdr: Magic / Cast).
- Empty chips stay hidden. Fishing baits and feasts are separate from Weapons.
- Click **Weapons** and a small filter panel opens under it: a grid of symbols (1-Hand, 2-Hand, Knives, Swords, Clubs, Axes, Spears, Polearms, Bows, Crossbows, Fists, Bombs). Hover an icon for its name. Hand and type combine (2-Hand + Axes). A weapon sits in several filters, a 1-handed axe is in 1-Hand and in Axes. Click a selected filter again, or Weapons, to clear.
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
- **Shift+E** (vanilla AltPlace + Use) sets build range: **default** (vanilla, about 20-36 m) → **50** → **100** → **150** → **200** → default.
- Absolute metres when overridden, not added on top of vanilla. Saved on that station. Hover shows the current range.
- Not allowed inside someone else's ward (`BuildRangeRequireAccess`). Highest value is `BuildRangeMaxMeters`.

---

## Config

**One file only** (no YAML):

`BepInEx/config/com.morda.workbenchesplus.cfg`

| Setting | Meaning |
|---|---|
| `EnableMod` | Master switch |
| `EnableCategories` | Category chips under the Repair button |
| `ShowCraftabilityIndicators` | Green check on fully craftable rows (default off) |
| `EnableCraftMultiplier` | Multi-craft arrows beside Craft |
| `EnableUpgradeCompare` | Upgrade tab: current stat value next to the upgraded one |
| `EnableDismantle` | Dismantle tab beside Craft / Upgrade |
| `DismantleRefundPercent` | Percent of materials returned (0-100, default 100) |
| `EnableBuildRangeCycle` | Shift+E on a station the hammer needs: default, 50, 100, 150, 200 m. Not inside someone else's ward |
| `BuildRangeMaxMeters` | Highest build range the cycle reaches (50-500, default 200) |
| `EnableAutoRepair` | Auto-repair worn gear inside a matching station's build range, one item at a time (default off) |
| `DebugLogging` | Extra logs |

Sorting has no settings: craftable recipes first, then sections by material / category in progression order, armor sets and weapon materials kept together.


---

## Install

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Drop `WorkbenchesPlus.dll` into `BepInEx/plugins/WorkbenchesPlus/` (or install via Thunderstore / r2modman).
3. Launch once to generate the config file.
