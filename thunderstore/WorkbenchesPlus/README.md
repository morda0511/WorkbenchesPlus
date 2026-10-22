![Workbenches+ - Crafting menus that stay readable](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/workbenches-plus-banner.png)

<br>

**Craftable first. Sorted by material and biome. Found in a second.**

Every station sorts its recipes the same way, with headers by material and biome, armor sets and weapon materials kept together. Category chips, a weapon filter, search, favorites, a Dismantle tab, build range and auto repair. Does not change recipes or crafting logic.

> **Client-side.** Valheim 1.0 · BepInExPack 5.4.2350+ · Console players via crossplay cannot load the mod.

[Bug reports](https://github.com/morda0511/WorkbenchesPlus/issues) · [Discord](https://discord.gg/aVKVVmyzj)

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/divider-norse.png)

## At a glance

| ![Sorting](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-sorting-160.png) | ![Categories](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-categories-160.png) | ![Search](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-search-160.png) | ![Favorites](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-favorites-160.png) |
|:---:|:---:|:---:|:---:|
| **Sorting**<br>Craftable first, then material and biome | **Categories**<br>Chips per station, weapon filter symbols | **Search**<br>Type a name or a material | **Favorites**<br>Right-click, always on top |

| ![Dismantle](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-dismantle-160.png) | ![Multi-craft](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-multicraft-160.png) | ![Build range](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-buildrange-160.png) | ![Auto repair](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/icon-repair-160.png) |
|:---:|:---:|:---:|:---:|
| **Dismantle**<br>Break gear back into its materials | **Multi-craft**<br>Arrows beside Craft, up to 99 | **Build range**<br>Shift+E: 50 to 200 m per station | **Auto repair**<br>Worn gear fixed one by one |

**Works with:** Path of Valheiman · crafting stations from other mods (Warfare, ...)

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/divider-norse.png)

## Sorting

- Recipes you can craft now sit at the top, with their own headers.
- Below that: headers by material and biome (Wood, Bronze, Iron, Silver, Black Metal, Flametal, ...), in progression order.
- Armor sets stay together (chest, helmet, legs, cape). Weapons of one material stay together, ordered by weapon type.
- Variants (Blood, Lightning, Frost/Fire, ...) sit directly behind the weapon they are made from. Deep North casts sit behind the item they cast.
- Food: Health, Stamina and Eitr headers, strongest food first. Feasts and fishing bait follow the biomes.
- Gear headers carry their biome, for example `Iron [Swamp]`. Food shows its strongest bar after the name: Health red, Stamina yellow, Eitr blue.
- The Forge of Potential list is sorted too. Nothing to set up: one order for everything.

## Categories and search

- **Chips** under the repair button: Weapons, Armor, Shields, Tools, Ammo, Magic, Health, Stamina, Eitr, Feasts, Prep, Fish, Bait, Potions, Cast, Materials, Trinkets, Clothes, Misc. Empty chips are hidden. **All** shows the current station only.
- **Weapons** opens a box of symbols: 1-Hand, 2-Hand, Knives, Swords, Clubs, Axes, Spears, Polearms, Bows, Crossbows, Fists, Bombs. One symbol at a time; bows and crossbows only count under their own symbol.
- **Search** at the right end of the tab row: filter by recipe name or header (type "iron" for the Iron block).
- **Favorites:** right-click a recipe. Favorites form a block at the top of every list.

## Crafting

- **Upgrade tab** shows the current value next to the upgraded one, for example `Durability: 400 (now: 200)`.
- **Multi-craft arrows** beside the Craft button set how many crafts to run (up to 99).

## Dismantle

- A tab beside Craft / Upgrade: break station-crafted items back into their materials.
- Shows exactly which materials you get back before you dismantle. The refund percentage is configurable.
- Food and potions are never listed. Not available at the Forge of Potential.

## Build range and auto repair

- Look at a station the hammer needs (workbench, forge, ...) and press **Shift+E** to cycle its build range: default, 50, 100, 150, 200 m. Saved on the station, not possible inside someone else's ward.
- **Auto repair** (off by default): worn armor, weapons and tools are repaired one at a time while you stand in a matching station's build range, once they drop to `AutoRepairBelowPercent` (default 90 %). Needs roof and fire like normal repair.

## Path of Valheiman

- While its Salvage / Disenchant tab or its category grid is open, Workbenches+ steps aside.
- Hover a recipe row to see the full Path of Valheiman item tooltip (rarity, stat ranges, tags).
- The search field sits behind its extra tab and never sticks out of the window.
- Optional `HidePathOfValheimanSalvage`: only Dismantle at normal stations. Disenchant at the Rune Carver Table always stays.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/divider-norse.png)

## Config

One file, `BepInEx/config/com.morda.workbenchesplus.cfg`:

| Setting | Meaning |
|:---|:---|
| `EnableMod` | Master switch |
| `EnableCategories` | Category chips under the Repair button |
| `ShowCraftabilityIndicators` | Green check on fully craftable rows (default off) |
| `EnableCraftMultiplier` | Multi-craft arrows beside Craft |
| `EnableUpgradeCompare` | Upgrade tab: current stat next to the upgraded one |
| `EnableDismantle` | Dismantle tab beside Craft / Upgrade |
| `DismantleRefundPercent` | Percent of materials returned (0-100, default 100) |
| `EnableBuildRangeCycle` | Shift+E build range cycle |
| `BuildRangeMaxMeters` | Highest build range (50-500, default 200) |
| `EnableAutoRepair` | Auto repair at stations (default off) |
| `AutoRepairBelowPercent` | Auto repair at or below this durability (1-99, default 90) |
| `HidePathOfValheimanSalvage` | Path of Valheiman only: hide its Salvage tab at normal stations (default off) |
| `DebugLogging` | Writes a recipe dump for bug reports |

**Servers:** the mod is client-side and works on any server. Dismantle, build range and auto repair are decided on your client; each can be switched off.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/divider-norse.png)

## More mods by Morda

| Mod | What it does |
|:---|:---|
| **[StoreAndCraft](https://thunderstore.io/c/valheim/p/Morda/StoreAndCraft/)** | A base that works with you: auto-store, dump, craft and build from chests, storage displays, feed trough, F10 settings panel |
| **[Morda's Gearhold](https://thunderstore.io/c/valheim/p/Morda/MordasGearhold/)** | Three switchable equipment sets, shared inventory, 8 hotkey quickslots, ammo slots, trinkets, a weightless coin slot, transmog looks, armour dyes and weapon glow |
| **[Forager's Garden](https://thunderstore.io/c/valheim/p/Morda/ForagersGarden/)** | Berry bushes, wild herbs and mushrooms with the Cultivator, plus Ice, Royal Jelly and Crystal beehives |
| **[Valheim Mannequins](https://thunderstore.io/c/valheim/p/Morda/ValheimMannequins/)** | Player-shaped mannequins: dress them with real gear, pose and name them, keep several gear sets |
| **[ServerRates](https://thunderstore.io/c/valheim/p/Morda/ServerRates/)** | Server-side rates for dedicated servers (PC and console), live apply, presets. Joining players need no install |

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/divider-norse.png)

## Install

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
2. Put `WorkbenchesPlus.dll` in `BepInEx/plugins/WorkbenchesPlus/` (or use Thunderstore / r2modman)
3. Launch once to create the config file

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/workbenches-plus-footer.png)
