# Git history

Repository: **present** (this folder is a git work tree). Remote: `origin/main` at last `git status`.

## Commits on `main` (oldest → newest)

| Hash | Date | Subject |
|---|---|---|
| `7a6ac7a` | 2026-09-17 | Initial release 1.0.0 — craftable-first sort, category chips, armor/weapon groups, Thunderstore packs |
| `b92b34f` | 2026-09-17 | Shorten README |
| `64f8e29` | 2026-09-17 | README features/links |
| `294fb62` | 2026-09-17 | Release 1.0.1 |
| `5a82b18` | 2026-09-17 | Changelog player-facing only |
| `532c35b` | 2026-09-17 | Changelog headings NEW FEATURES / UI IMPROVEMENTS / FIX |
| `279192a` | 2026-09-18 | Release 1.0.2 — Dismantle |
| `734cdec` | 2026-09-18 | README mentions Dismantle |

## Range feature

`git log -S "m_useDistance" --oneline` → no hits.  
`StationFilter.cs` history: **created in 1.0.0** with name matching only; **no later commit** on that file until the uncommitted 1.0.3 working tree.

**No removed range implementation. No range revert.**

## Harmony / config

Initial commit already Harmony-patched `InventoryGui`. 1.0.2 added `DismantlePatches` and `EnableDismantle`. No Harmony unpatch/repatch cycles in history beyond normal releases.

## Working tree (not committed)

At analysis time, `main` was **dirty**: 1.0.3 Forge of Potential `StationFilter` skip, dismantle block at `m_upgrader`, `Craftability.CountsTowardCraft`, version/changelog/README, `Pack == true` gate on zip target, plus other uncommitted UI/sort files. Thunderstore DLL copy modified.

Untracked: `.cursor/`, `_stations.json`, `_workbench_recipes.json`, `_workbench_label_map.txt`, `nexus/`.

## Prefabs

No prefab files in git. Icon is under `thunderstore/WorkbenchesPlus/icon.png` (packaging).
