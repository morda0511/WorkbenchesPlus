# Workbenches+

Valheim QoL mod that sorts and filters crafting station recipe lists. Craftable recipes first, category chips, armor and weapon material groups, biome/tier ordering.

Does **not** change recipes, crafting logic, or duplicate list entries.

## Features (v1.0.0)

- **Craftable first** — only fully craftable rows rise to the top; uncraftable set mates stay below
- **Category chips** — All / Weapons / Armor / Shields / Tools / Building / Furniture / Food / Potions / Materials / Misc (station-scoped)
- **Armor set grouping** — helmet / chest / legs / cape from the same set stay together
- **Weapon material grouping** — Iron with Iron, Black Metal with Black Metal, etc.
- **Biome / tier order** — Copper → Bronze → Iron → Silver → Black Metal → …
- **Sort modes** — `CraftableFirst`, `CategoryThenCraftable`, `Progression`, `Alphabetical`, `Vanilla`
- Optional craftability checkmarks (off by default)

## Install

1. Install BepInEx for Valheim
2. Copy `WorkbenchesPlus.dll` into `BepInEx/plugins/WorkbenchesPlus/`
3. Launch once to generate `BepInEx/config/com.morda.workbenchesplus.cfg`

Thunderstore / Hexium: install the zip via your mod manager.  
Nexus / Vortex: use the `-Nexus` zip (plugins folder layout).

## Config

| Key | Default | Meaning |
|-----|---------|---------|
| EnableMod | true | Master switch |
| EnableCategories | true | Category chips under the repair button |
| EnableCraftableFirst | true | Prioritize craftable recipes |
| EnableArmorSetGrouping | true | Keep armor sets together |
| EnableWeaponSetGrouping | true | Keep weapons by material together |
| SortMode | CraftableFirst | CraftableFirst / CategoryThenCraftable / Progression / Alphabetical / Vanilla |
| ArmorPieceOrder | Chest,Helmet,Legs,Cape,Other | Order inside an armor set |
| WeaponPieceOrder | Knife,Sword,… | Order inside a weapon material group |
| GroupModdedArmorSets | true | Heuristics for modded armor prefabs |
| GroupModdedWeaponSets | true | Heuristics for modded weapon prefabs |
| RefreshOnInventoryChange | true | Re-sort when inventory changes |
| RefreshOnStationChange | true | Reset category to All when switching stations |
| ShowCraftabilityIndicators | false | Green check on fully craftable rows |
| DebugLogging | false | Extra logs |

## Build

Requires Valheim + BepInEx paths in `Directory.Build.props` (defaults to Steam Valheim and Thunderstore profile **Solo Gameplay**).

```bat
dotnet build WorkbenchesPlus.csproj -c Release
```

DLL deploys to Solo Gameplay; Thunderstore / Hexium / Nexus zips land on the Desktop mod folder.

## Known limitations

- Building / furniture chips mainly help when those recipes appear in station lists (hammer build menu is separate)
- Modded armor / weapon sets depend on prefab naming; odd names may not group
- Other mods that fully replace `UpdateRecipeList` may fight over order

## License

Use freely with credit to Morda.
