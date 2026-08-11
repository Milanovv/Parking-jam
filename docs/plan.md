# Parking Jam — itch.io v1 Release Plan

## Overview

A free sliding-block puzzle game built in Unity 6 for PC (Windows), released on itch.io. All mechanics from the domain model ship in v1: grid, vehicles, collisions, undo, barriers, mini-games, mobile obstacles (pedestrians), constraints, and economy (Coins/Keys/skins). ~25 hand-crafted levels. Free, no ads.

## Decisions

| # | Decision | Choice | ADR |
|---|----------|--------|-----|
| 1 | Platform order | PC first (itch.io), then mobile | ADR-0001 |
| 2 | Level scope | ~25 levels, all mechanics included | — |
| 3 | Monetization | Free, no ads | ADR-0001 |
| 4 | Storefront | itch.io | ADR-0001 |
| 5 | Unity version | Unity 6 (6000.x) + URP | ADR-0002 |
| 6 | Input system | Unity Input System package (Pointer abstraction) | ADR-0006 |
| 7 | Architecture | GameManager + OccupancyMap (Dictionary) + thin views + enum state + Memento undo | ADR-0004 |
| 8 | Level data | JSON in StreamingAssets | ADR-0003 |
| 9 | Level authoring | Hand-write JSON | ADR-0003 |
| 10 | Visual style | 2.5D — low-poly 3D in-world content, 2D uGUI UI | ADR-0011 |
| 11 | UI framework | Unity Canvas (uGUI) | ADR-0005 |
| 12 | Audio | SFX only, free packs (Mixkit, CC0 freesound) | ADR-0008 |
| 13 | Art source | Free Asset Store packs — 3D low-poly in-world, 2D UI icons | ADR-0009/0011 |
| 14 | Platform targets | Windows only | ADR-0001 |
| 15 | Project structure | Folder-per-type under `_Project/` | ADR-0010 |
| 16 | Release goal | Portfolio + audience building | ADR-0001 |
| 17 | Timeline | 8-12 weeks | — |

## Project Structure

```
Assets/
├── _Project/
│   ├── Scripts/ (Core/, UI/)
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Sprites/
│   ├── Audio/
│   ├── Fonts/
│   └── Settings/
├── Plugins/
└── StreamingAssets/Levels/
```

## Estimated Timeline (12 weeks)

- **Weeks 1-2:** Core grid, vehicles, input handling
- **Weeks 3-4:** Collision detection, undo system, level loading
- **Weeks 5-6:** Barriers, mini-games, pedestrian obstacles, level constraints
- **Weeks 7-8:** Economy (Coins/Keys), skins, UI screens (menu, level select, HUD, shop, settings)
- **Weeks 9-10:** Art assets, SFX, level design (~25 levels, see `docs/specs/level-progression.md`)
- **Weeks 11-12:** Testing, bug fixes, itch.io page, release

## Supporting Research

All decisions validated against primary sources. See:
- `docs/research/pc-vs-mobile-release.md` — platform comparison
- `docs/research/free-pc-game-stores.md` — free storefronts
- `docs/research/easiest-architecture-pattern.md` — code architecture
- `docs/research/level-data-storage.md` — level data approach
- `docs/research/level-authoring-easiest.md` — level authoring
- `docs/research/ui-framework-easiest.md` — UI framework
- `docs/research/project-structure.md` — folder structure
- `docs/research/plan-validation.md` — decision validation
