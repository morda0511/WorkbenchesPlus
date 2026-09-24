# Station categories (Workbenches+)

Chip sets are per crafting station. Empty chips stay hidden.

Source recipe groups: `_stations.json` (asset PathIDs).

## Chip sets

| Station | Prefab / token | Chips |
|---|---|---|
| Workbench | `$piece_workbench` | All, Weapons, Armor, Shields, Tools, Ammo, Clothes, Misc |
| Forge | `$piece_forge` | All, Weapons, Armor, Shields, Tools, Ammo, Materials, Trinkets |
| Black Forge | `$piece_blackforge` | All, Weapons, Armor, Shields, Ammo, Tools, Materials, Trinkets |
| Cauldron | `$piece_cauldron` | All, Health, Stamina, Eitr |
| Food Preparation Table | `$piece_preptable` | All, Feasts, Prep, Fish, Bait |
| Mead Ketill | `$piece_meadcauldron` | All, Health, Stamina, Eitr, Potions |
| Galdr Table | `$piece_magetable` | All, Magic, Cast, Armor, Materials |
| Artisan Table | `$piece_artisanstation` | All, Materials, Ammo, Misc |
| Stonecutter | `$piece_stonecutter` | All, Materials, Tools, Misc |
| Stone Oven | `$piece_oven` | All, Health, Stamina, Eitr, Feasts |
| Black Forge | `$piece_blackforge` | All, Weapons, Armor, Shields, Ammo, Cast, Tools, Trinkets, Materials |
| Forge of Potential | `UpgradeStation` | *(no chips — vanilla list)* |

Black Forge weapon list headers: **Bows**, **Crossbows**, Carapace/Flametal/**Gold**, named sets (Berzerkr, Jotun, Skull, SkollHati, Eldner, Demolisher, Splitnir, Krom, Niedhogg, Slayer, …). `*Uncooked` → **Cast**.

| Artisan Table | `$piece_artisanstation` | All, Materials, Ammo, Misc |
| Stonecutter | `$piece_stonecutter` | All, Materials, Tools, Misc |
| Stone Oven | `$piece_oven` | All, Health, Stamina, Eitr, Feasts |
| Forge of Potential | `UpgradeStation` | *(no chips — vanilla list)* |

## Recipe groups (from dump)

### Hand / basic (`path=0`)
Clothes (Dress/Tunic/Hats), Hammer, Club, Adze, Chisel, Feaster, Snowball, minor potions, Torch, stone axe, …

### Workbench
Wood/flint weapons, leather/troll/root/berserker/fenrir armor, wooden shields, arrows (flint/wood/…), bombs, fireworks, tankards, hoe, antler pickaxe, saddles, demister, …

### Forge
Metal weapons/armor/shields, metal arrows, bronze/iron nails, cultivator, scythe, metal pickaxes, trinkets (bronze/iron/silver/black), butcher knife, …

### Black Forge
Mistlands/Ashlands/Deep North gear, carapace/flametal/bloodgold, bolts, crossbows, staff-adjacent melee, grappling hook, lantern, Asksvin/Moose saddles, trinkets, …

### Cauldron (soups / stews)
CarrotSoup, DeerStew, Sausages, WolfSkewer, SerpentStew, BoarJerky, BloodPudding, YggdrasilPorridge, …

### Cauldron / oven-style food group (feasts, bait, pies)
Feast*, FishingBait*, Bread, LoxPie, MeatPlatter, MisthareSupreme, VikingCupcake, …

### Mead Ketill
MeadBase*, BarleyWineBase, OatMilk

### Galdr Table
Staff*, Mage armor/helmets/capes, KnifeVoid, DvergrKey, TorchMist

### Artisan Table
CeramicPlate, MechanicalSpring, ShieldCore, TurretBolt*

### Stonecutter (dump incomplete)
SharpeningStone

## Classification rules

- `FishingBait*` → **Bait**
- `Feast*` → **Feasts** (not the Feaster serving tray)
- `Mold*` / Mould / Gussform → **Cast** (Deep North moulds at Galdr)
- `Uncook*` / Dough → **Prep** (Food Preparation Table)
- Raw fish prefabs → **Fish**
- Food consumables → dominant bar from vanilla `m_food` / `m_foodStamina` / `m_foodEitr` → **Health** / **Stamina** / **Eitr** (ties: Health → Stamina → Eitr); within a chip, sorted high → low
- `Mead*` / potions → name contains Health / Stamina / Eitr when possible; else **Potions**
- `Staff*` / TorchMist → **Magic**
- `Trinket*` → **Trinkets**
- Arrow / Bolt (not TurretBolt) → **Ammo**
- `TurretBolt*` → **Ammo**
- Dress / Tunic / Hat → **Clothes**
