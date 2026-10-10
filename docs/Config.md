# Config

File: `BepInEx/config/com.morda.workbenchesplus.cfg`  
Loaded in `Plugin.Awake` via `new ModConfig(Config)` then `Config.Save()`. `SettingChanged` rebuilds the crafting panel while it is open. No server sync.

Only real choices are in the file. Everything else is a `Fixed<T>` in `ModConfig` (same `.Value` shape, so call sites did not change): sort order, set grouping, material headers, piece orders, refresh behaviour, craft multiplier cap (99), dismantle rules (no consumables, no durability scaling), build range ward check, auto-repair timing (scan 5 s, 1.5 s between repairs, below 90 %).

| Section | Name | Type | Default | Effect |
|---|---|---|---|---|
| General | `EnableMod` | bool | true | Master switch |
| Crafting menu | `EnableCategories` | bool | true | Category chips (`CategoryBar`) |
| Crafting menu | `ShowCraftabilityIndicators` | bool | false | Green check on craftable rows |
| Crafting menu | `EnableCraftMultiplier` | bool | true | Arrows beside Craft (`CraftMultiplierBar`) |
| Crafting menu | `EnableUpgradeCompare` | bool | true | `UpgradeCompare` |
| Dismantle | `EnableDismantle` | bool | true | Dismantle tab (`DismantleMode`) |
| Dismantle | `DismantleRefundPercent` | int 0-100 | 100 | `DismantleMode.ScaleRefund` |
| Build range | `EnableBuildRangeCycle` | bool | true | Shift+E cycle (`BuildRangeBonus`) |
| Build range | `BuildRangeMaxMeters` | int 50-500 | 200 | Highest cycle step |
| Auto repair | `EnableAutoRepair` | bool | false | `AutoRepair.Tick` from `Plugin.Update` |
| Debug | `DebugLogging` | bool | false | Sort dump / build range stations |

Old config files: entries that no longer exist are dropped the next time the file is saved.
