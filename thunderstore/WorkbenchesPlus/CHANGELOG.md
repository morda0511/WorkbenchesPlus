# Changelog

## 1.0.16

### UI IMPROVEMENTS
- Weapon filter symbols sit in their own box (2 rows) while Weapons is selected; the recipe list moves down to make room
- Only one weapon symbol at a time
- Bows and crossbows only count under their own symbol, not as 2-Hand
- Bolts from mods are sorted as bolts even when the mod gives them the bow skill
- Dismantle shows the materials you get back, no station level badge, and the stats of the item as it is
- Search field height fits the tab row

### FIX
- Dismantle took the item of another row (the game re-sorts the rows); now always the selected one
- Dismantle with two identical items: the item was removed but no materials were given
- Dismantle skips "any one ingredient" recipes (it would have refunded every listed ingredient)

## 1.0.15

### NEW FEATURES
- Search field at the right end of the tab row
- Favorites: right-click a recipe, favorites sit in their own block at the top
- Auto repair (off by default): repairs worn gear one item at a time inside a matching station's build range
- Upgrade tab shows the current stat value next to the upgraded one
- Food shows its strongest bar after the name (Health red, Stamina yellow, Eitr blue)

### UI IMPROVEMENTS
- New sorting for every station: craftable first, then headers by material and biome, variants and casts behind their base item
- Gear headers show their biome, for example Iron [Swamp]; the Forge of Potential list is sorted too
- Weapons filter panel: a grid of symbols for 1-Hand, 2-Hand and the weapon types (Swords, Axes, Bows ...), combinable
- Recipes of modded crafting stations are found the same way the game finds them
- Category chips stay clickable and keep their color when materials are missing
- Shift+E build range only on stations the hammer needs, not inside someone else's ward
- Dismantle: exact inventory-space check, no food or potions, refund percent in the config
- English and German texts
- Much shorter config

### FIX
- List no longer scrolls past its last entry
- No more duplicate headers when nothing is craftable

## 1.0.14

### UI IMPROVEMENTS
- Build range cycle is absolute: default, then 50, 100, 150, 200 m (not added on top of vanilla)

## 1.0.13

### FIX
- Opening a chest no longer crashes when category chips were already destroyed

## 1.0.12

### UI IMPROVEMENTS
- Better category filters per crafting station

### FIX
- Forge of Potential works again

## 1.0.2

### NEW FEATURES
- Dismantle: break station-crafted items back into their crafting materials

### UI IMPROVEMENTS
- New Dismantle tab beside Craft / Upgrade on workbenches
- Materials are sorted by labels
