# Barrier & Exit Mechanics in Sliding-Block Puzzle Games

Research conducted July 2026. Sources: primary docs, official store pages, gameplay
videos, wiki entries, and developer rulebooks.

---

## 1. Parking Jam (original mobile / web versions)

| Property | Finding |
|----------|---------|
| **Exit location** | Edge of the grid. Cars drive off the board onto a road/path. |
| **Barrier type** | Two distinct things: (a) a **bar gate** at the exit that is purely visual — cars pass through it when they leave; (b) **road barriers / cones / fixed obstacles** placed on grid tiles that block movement |
| **Exit mechanism** | Cars slide until they clear the grid edge. The "bar gate" is the decorative frame around the exit lane. |
| **Barrier removal** | No mini-game. Barriers are static obstacles you navigate around. If a car hits a barrier it cannot pass. |
| **Source** | silvergames.com, crazygames.com, agame.com (all show "pass through the bar gate"). The app store copy repeatedly warns: *"carefully lead them through barriers and onto the road"* |

### Barrier spatial position
Road barriers sit **on grid tiles** inside the lot. They are immovable obstacles. The "bar
gate" sits at the **grid edge** (exit boundary).

### Exit spatial position
Varies per level. Usually one tile-wide lane on the right or bottom edge. In the "Parking
Jam Online" variant the exit lane wraps around the lot perimeter.

---

## 2. Rush Hour / ThinkFun

| Property | Finding |
|----------|---------|
| **Exit location** | Right edge of the 6×6 grid, **row 3 only** (the 3rd row from the top). A physical notch/"exit hole" in the board. |
| **Barrier type** | None. No barrier, gate, or obstacle blocks the exit. The puzzle is purely about clearing vehicles. |
| **Exit mechanism** | The red car slides horizontally out through the notch. All other vehicles stay on the board. |
| **Source** | Wikipedia, ThinkFun official rulebook, Rush Hour manual (manua.ls), UC Berkeley GamesCrafters |

### Exit spatial position
**Edge-based, row 3 only.** The notch is a permanent opening in the right wall. It is not
a tile — it is the absence of a wall at that position. Vehicles do not occupy the exit
space; they slide past the rightmost grid column.

---

## 3. Unblock Me

| Property | Finding |
|----------|---------|
| **Exit location** | Right edge of the 6×6 grid, **row 3** (the red block always sits on row 3). |
| **Barrier type** | None. |
| **Exit mechanism** | The red block slides right off the board edge. The exit is defined by the grid boundary, not a tile. |
| **Source** | MobyGames, thanassis.space (solver article), funhub1.com guide, official App Store page |

### Exit spatial position
**Edge-based, row 3.** No tile or marker — the red block simply clears the right edge. In
Daily Puzzle mode, alternative exit locations appear (described as "alternative exit
locations on the board"), but these are still edge-based.

---

## 4. Block Puzzle / BlockuDoku / Block Slide / Wood Blocks Jam

| Property | Finding |
|----------|---------|
| **Exit location** | Internal **color-coded gates** on the grid. Each colored block must reach its matching gate. |
| **Barrier type** | Gates act as blockers: a block can only exit through its matching coloured gate. Other blocks cannot use it. |
| **Exit mechanism** | Drag block onto its matching gate tile — block is consumed and removed. |
| **Source** | plix.gg (Blockibo), 2games.io (Block Slide), 61673.com (Wood Blocks Jam) |

### Exit spatial position
**Tile-based (internal).** Gates sit on specific grid tiles. They are not on the edge.
This is a significant departure from the Rush Hour model.

---

## 5. Other sliding-block puzzle games

### Gridlock (MiniGames World)
- Rush Hour clone reskinned as a starport. Exit is the **right edge**. No barrier.
- Source: minigames.world

### Block Escape (wugames.io)
- Rush Hour clone. Exit is the **right edge**. No barrier.
- Source: wugames.io

### Kinetic Puzzle (br0wer.com)
- Has **edge portals** as exits. Blocks are removed by sliding them through portals.
- No barrier mechanic blocking a portal.
- Source: br0wer.com

### Pull-out Block (freejoy.games)
- Rush Hour clone. "Exit gate" is the right edge of the board. No barrier.
- Source: freejoy.games

### Tiny Gate (itch.io)
- Box-sliding puzzle. Uses **gates** that blocks pass through to reach new areas, but
  this is a different genre (Sokoban-like, not Rush Hour-like).

---

## Summary table

| Game | Exit is ... | Barrier mechanic? | Barrier position |
|------|-------------|-------------------|------------------|
| **Parking Jam (original)** | Edge of grid (varies per level) | Static obstacles on grid tiles + decorative bar gate at edge | Grid interior (obstacles) + edge (bar gate) |
| **Rush Hour** | Right edge, row 3 only | None | — |
| **Unblock Me** | Right edge, row 3 | None | — |
| **BlockuDoku / Block Slide** | Internal color-coded tiles | Gate tiles themselves act as barriers until reached | Internal grid tiles |
| **Gridlock / Block Escape / Pull-out Block** | Right edge | None | — |

---

## Existing project design

The current Parking Jam spec (`docs/specs/unity-implementation.md`) defines:

- `exitTiles` as a list of `[x, z]` coordinates in level JSON (tile-based on edge)
- `barriers` as objects placed on specific tiles that, when tapped, load a mini-game scene
- On mini-game completion, the barrier is removed from the occupancy map, freeing the exit

This design is **not found in any existing game**. No current sliding-block puzzle uses a
tap-to-mini-game barrier that blocks the exit. The closest analog is BlockuDoku-style
color-coded gates, but those are destination tiles, not interruptive blockers.

---

## Recommendation

Place the barrier on **the outermost exit tile(s) on the grid edge** (one cell before
the vehicle would leave the board), not on an interior tile. This is because:

1. Every game surveyed puts the exit on the grid **edge** (right side, row 3 in Rush
   Hour tradition, or varying per level in Parking Jam). Players expect the exit to be
   at the boundary.
2. A barrier on the edge tile is visually intuitive: the vehicle is at the exit but a
   gate blocks it. An interior tile barrier reads as just another obstacle.
3. The current `exitTiles` + `barriers` data model already supports this naturally —
   `barriers[].tile` can reference any tile, including an exit tile.
4. This matches the thematic "boom gate at the parking lot exit" that real parking lots
   use.
