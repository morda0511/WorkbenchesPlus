![Workbenches+ - Crafting menus that stay readable](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/WB+%20Banner.png)

<br>

**Find it in a second. Build farther. Break it back down.**

Every station sorts the same way — material, biome, sets together. Search, categories, craft multiplier, and a Dismantle tab that returns gear to its materials. Shift+E stretches a station’s build range up to 200 m. Does not change recipes or crafting logic.

> **Client-side.** Valheim 1.0 · BepInExPack 5.4.2350+ · Console players via crossplay cannot load the mod.

[Bug reports](https://github.com/morda0511/WorkbenchesPlus/issues) · [Discord](https://discord.gg/aVKVVmyzj)

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-at-a-glance.png)

| ![Sorting](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-sorting-192.png) | ![Search](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-search-192.png) | ![Categories](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-categories-192.png) | ![Upgrade](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-upgrade-192.png) |
|:---:|:---:|:---:|:---:|
| **Sorting**<br>By material, biome and set | **Search & favorites**<br>Type a name, right-click to pin | **Categories**<br>Chips per station, weapon filter symbols | **Upgrade**<br>New value next to the current one |

| ![Dismantle](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-dismantle-192.png) | ![Multiplier](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-multiplier-192.png) | ![Build range](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-range-192.png) | ![Auto repair](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/glance-repair-192.png) |
|:---:|:---:|:---:|:---:|
| **Dismantle**<br>Break gear back into its materials | **Multiplier**<br>Arrows beside Craft, up to 99 | **Build range**<br>Shift+E: 50 to 200 m per station | **Auto repair**<br>Worn gear fixed one by one |

**Works with:** Path of Valheiman · crafting stations from other mods (Warfare, ...)

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-sorting.png)

- Headers by material and biome (Wood, Bronze, Iron, Silver, Black Metal, Flametal, ...), in progression order.
- Armor sets stay together (chest, helmet, legs, cape). Weapons of one material stay together, ordered by weapon type.
- Variants (Blood, Lightning, Frost/Fire, ...) sit directly behind the weapon they are made from. Deep North casts sit behind the item they cast.
- Food: Health, Stamina and Eitr headers, strongest food first. Feasts and fishing bait follow the biomes.
- Gear headers carry their biome, for example `Iron [Swamp]`. Food shows its strongest bar after the name: Health red, Stamina yellow, Eitr blue.
- The Forge of Potential list is sorted too. Nothing to set up: one order for everything.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-categories-and-search.png)

- **Chips** under the repair button: Weapons, Armor, Shields, Tools, Ammo, Magic, Health, Stamina, Eitr, Feasts, Prep, Fish, Bait, Potions, Cast, Materials, Trinkets, Clothes, Misc. Empty chips are hidden. **All** shows the current station only.
- **Weapons** opens a box of symbols: 1-Hand, 2-Hand, Knives, Swords, Clubs, Axes, Spears, Polearms, Bows, Crossbows, Fists, Bombs. One symbol at a time; bows and crossbows only count under their own symbol.
- **Gamepad:** right stick up / down picks the chip, left / right steps through the Weapons symbols.
- **Search** at the right end of the tab row: filter by recipe name or header (type "iron" for the Iron block).
- **Favorites:** right-click a recipe. Favorites form a block at the top of every list.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-crafting.png)

- **Upgrade tab** shows the current value next to the upgraded one, for example `Durability: 400 (now: 200)`.
- **Multi-craft arrows** beside the Craft button set how many crafts to run (up to 99). Gamepad: D-pad left / right, hold to repeat.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-dismantle.png)

- A tab beside Craft / Upgrade: break station-crafted items back into their materials.
- **Gamepad:** LT / RT step through the tabs: Craft, Upgrade, Dismantle.
- Shows exactly which materials you get back before you dismantle. The refund percentage is configurable.
- Food and potions are never listed. Not available at the Forge of Potential.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-build-range-and-auto-repair.png)

- Look at a station the hammer needs (workbench, forge, ...) and press **Shift+E** to cycle its build range: default, 50, 100, 150, 200 m. Saved on the station, not possible inside someone else's ward.
- **Auto repair** (off by default): worn armor, weapons and tools are repaired one at a time while you stand in a matching station's build range, once they drop to `AutoRepairBelowPercent` (default 90 %). Needs roof and fire like normal repair.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-path-of-valheiman.png)

- While its Salvage / Disenchant tab or its category grid is open, Workbenches+ steps aside.
- Hover a recipe row to see the full Path of Valheiman item tooltip (rarity, stat ranges, tags).
- The search field sits behind its extra tab and never sticks out of the window.
- Optional `HidePathOfValheimanSalvage`: only Dismantle at normal stations. Disenchant at the Rune Carver Table always stays.

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-config.png)

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

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-more-mods-by-morda.png)

| [![Store and Craft](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-storeandcraft-192.png)](https://thunderstore.io/c/valheim/p/Morda/StoreAndCraft/) | [![Forager's Garden](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-foragers-192.png)](https://thunderstore.io/c/valheim/p/Morda/ForagersGarden/) | [![Morda's Gearhold](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-gearhold-192.png)](https://thunderstore.io/c/valheim/p/Morda/MordasGearhold/) | [![Valheim Mannequins](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-mannequins-192.png)](https://thunderstore.io/c/valheim/p/Morda/ValheimMannequins/) |
|:---:|:---:|:---:|:---:|
| **[Store and Craft](https://thunderstore.io/c/valheim/p/Morda/StoreAndCraft/)** | **[Forager's Garden](https://thunderstore.io/c/valheim/p/Morda/ForagersGarden/)** | **[Morda's Gearhold](https://thunderstore.io/c/valheim/p/Morda/MordasGearhold/)** | **[Valheim Mannequins](https://thunderstore.io/c/valheim/p/Morda/ValheimMannequins/)** |

| [![ServerRates](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-serverrates-192.png)](https://thunderstore.io/c/valheim/p/Morda/ServerRates/) | [![Smooth Build Repair](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-smoothbuildrepair-192.png)](https://thunderstore.io/c/valheim/p/Morda/SmoothBuildRepair/) | [![Any Angle](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-anyangle-192.png)](https://thunderstore.io/c/valheim/p/Morda/AnyAngle/) | [![Easy Build Cam](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/logo-buildcam-192.png)](https://thunderstore.io/c/valheim/p/Morda/EasyBuildCam/) |
|:---:|:---:|:---:|:---:|
| **[ServerRates](https://thunderstore.io/c/valheim/p/Morda/ServerRates/)** | **[Smooth Build Repair](https://thunderstore.io/c/valheim/p/Morda/SmoothBuildRepair/)** | **[Any Angle](https://thunderstore.io/c/valheim/p/Morda/AnyAngle/)** | **[Easy Build Cam](https://thunderstore.io/c/valheim/p/Morda/EasyBuildCam/)** |

![](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/dividers/divider-install.png)

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
2. Put `WorkbenchesPlus.dll` in `BepInEx/plugins/WorkbenchesPlus/` (or use Thunderstore / r2modman)
3. Launch once to create the config file

![Workbenches+](https://raw.githubusercontent.com/morda0511/WorkbenchesPlus/main/thunderstore-assets/workbenches-plus/WB+%20Footer.png)
