# Parking Jam — Level Progression Design

## Principles

1. **One new mechanic per zone** — each zone introduces one element, then later zones combine it with others
2. **Tutorial by doing** — mechanics are taught via play, not text overlays (except the first barrier encounter)
3. **Mini-game type rotates** — no two consecutive levels share the same mini-game type
4. **Easy in, hard out** — each zone starts easier than it ends
5. **Grid scales with complexity** — small grids for learning, larger grids for challenge

## Overview

| Zone | Levels | Focus | Grid range | Mini-game type |
|------|--------|-------|------------|----------------|
| 1 | 1–2 | Drag & move | 5×5 | None |
| 2 | 3–4 | Static obstacles | 6×6 | None |
| 3 | 5–6 | Pedestrians | 6×6 | None |
| 4 | 7–8 | Barrier + mini-game | 7×7 | Pipe Puzzle (Easy → Medium) |
| 5 | 9–10 | Grid complexity | 8×8 | Pattern Lock Easy |
| 6 | 11–12 | Constraints (moves/time) | 8×8 | Pattern Lock Medium |
| 7 | 13–15 | All mechanics combined | 8×8–9×9 | Pattern Lock Medium |
| 8 | 16–17 | Memory challenge | 8×8 | Memory Flip Medium |
| 9 | 18–20 | Tight constraints | 9×9 | Memory Flip Medium |
| 10 | 21–25 | Master | 10×10–12×12 | Any Hard |

## Zone details

### Zone 1: Drag & Move (Levels 1–2)

Purpose: Teach the core interaction — drag a vehicle along its axis.

| | Level 1 | Level 2 |
|--|---------|---------|
| **Grid** | 5×5 | 5×5 |
| **Vehicles** | 2 (1 horizontal, 1 vertical) | 3 (2 horizontal, 1 vertical) |
| **Obstacles** | None | None |
| **Pedestrians** | None | None |
| **Barrier** | No | No |
| **Constraint** | None | None |
| **Undos** | 5 | 5 |
| **Mini-game** | — | — |
| **Exit tiles** | Right edge, 1 tile | Right edge, 1 tile |
| **Design** | Both vehicles must exit. Single clear path — no blocking. Player drags each vehicle to edge. | One vehicle blocks another. Player must choose order. |

Tutorial: Level 1 starts with a pulsing arrow on each vehicle and a "drag me" tooltip on first touch. No fail state.

### Zone 2: Static Obstacles (Levels 3–4)

Purpose: Introduce immovable obstacles that force path planning.

| | Level 3 | Level 4 |
|--|---------|---------|
| **Grid** | 6×6 | 6×6 |
| **Vehicles** | 3 | 4 |
| **Obstacles** | 2 (corners) | 4 (wall segment) |
| **Pedestrians** | None | None |
| **Barrier** | No | No |
| **Constraint** | None | None |
| **Undos** | 4 | 4 |
| **Mini-game** | — | — |
| **Exit tiles** | Right edge, 1 tile | Right edge, 2 tiles |
| **Design** | Exit visible but obstacles narrow the path. Player learns to work around fixed tiles. | Multiple exit tiles give flexibility. Obstacles form a corridor. |

Obstacles are visually distinct (orange cones, construction barriers). They never move, never break.

### Zone 3: Pedestrians (Levels 5–6)

Purpose: Introduce moving obstacles with predictable patrol routes.

| | Level 5 | Level 6 |
|--|---------|---------|
| **Grid** | 6×6 | 6×6 |
| **Vehicles** | 3 | 4 |
| **Obstacles** | None | 2 |
| **Pedestrians** | 1 (horizontal, 3 tiles) | 2 (one horizontal, one vertical) |
| **Barrier** | No | No |
| **Constraint** | None | None |
| **Undos** | 4 | 3 |
| **Mini-game** | — | — |
| **Exit tiles** | Right edge, 1 tile | Right edge, 1 tile |
| **Design** | Single pedestrian on a short route — player sees the patrol pattern and times moves between passes. | Two pedestrians cross paths. Player must coordinate movement to avoid both. |

Pedestrians have a visual "safety vest" colour and a pause animation when blocked.

### Zone 4: Barrier & Mini-Game (Levels 7–8)

Purpose: Introduce the barrier mechanic and the mini-game unlock flow.

| | Level 7 (Tutorial) | Level 8 |
|--|--------------------|---------|
| **Grid** | 7×7 | 7×7 |
| **Vehicles** | 4 | 5 |
| **Obstacles** | 2 | 3 |
| **Pedestrians** | None | 1 (short route) |
| **Barrier** | Yes, 1 tile, right edge | Yes, 2 tiles, right edge |
| **Constraint** | None | None |
| **Undos** | 5 | 4 |
| **Mini-game** | Pipe Puzzle Easy | Pipe Puzzle Medium |
| **Exit tiles** | Right edge, 1 tile (same as barrier) | Right edge, 2 tiles (barrier on one) |
| **Design** | Barrier is the only thing blocking the exit. Mini-game is trivial (2 rotatable tiles, no timer). Vehicles line up behind barrier, waiting. Player taps barrier → mini-game → cleared. | Barrier blocks one exit tile; the other is open but harder to reach. Player must decide: take the long path or unlock the barrier. |

Level 7 tutorial: barrier has a subtle pulsing glow and a "Tap to unlock" tooltip on first approach. Mini-game opens with a brief overlay: "Connect the pipes to open the gate."

### Zone 5: Grid Complexity (Levels 9–10)

Purpose: Larger grids, more vehicles, new mini-game type.

| | Level 9 | Level 10 |
|--|---------|----------|
| **Grid** | 8×8 | 8×8 |
| **Vehicles** | 5 | 6 |
| **Obstacles** | 3 | 5 |
| **Pedestrians** | 1 | 2 |
| **Barrier** | Yes | Yes |
| **Constraint** | None | None |
| **Undos** | 4 | 3 |
| **Mini-game** | Pattern Lock Easy | Pattern Lock Easy |
| **Exit tiles** | Right edge, 2 tiles | Right edge, 2 tiles |
| **Design** | More open space but many vehicles. Player must untangle multiple blocking chains. Pattern Lock replaces Pipe Puzzle for variety. | Tighter packing. Multiple pedestrians create timing pressure. |

### Zone 6: Constraints (Levels 11–12)

Purpose: Introduce move limits and time limits.

| | Level 11 | Level 12 |
|--|---------|----------|
| **Grid** | 8×8 | 8×8 |
| **Vehicles** | 6 | 7 |
| **Obstacles** | 4 | 4 |
| **Pedestrians** | 2 | 3 |
| **Barrier** | Yes | Yes |
| **Constraint** | Move limit: 25 | Time limit: 90s |
| **Undos** | 3 | 3 |
| **Mini-game** | Pattern Lock Medium | Pattern Lock Medium |
| **Exit tiles** | Right edge, 2 tiles | Top edge, 2 tiles |
| **Design** | Move limit is generous for optimal play but punishes wasted moves. Player learns efficient pathing. | Time limit adds urgency. Exit on top edge forces a different spatial approach. |

Constraint is shown prominently in the HUD: countdown number (moves) or timer bar (time). Turns red when <25% remaining.

### Zone 7: All Mechanics Combined (Levels 13–15)

Purpose: Every mechanic active at once. Player must juggle all systems.

| | Level 13 | Level 14 | Level 15 |
|--|---------|----------|----------|
| **Grid** | 8×8 | 9×9 | 9×9 |
| **Vehicles** | 6 | 7 | 8 |
| **Obstacles** | 4 | 5 | 6 |
| **Pedestrians** | 2 | 2 | 3 |
| **Barrier** | Yes | Yes | Yes |
| **Constraint** | Move limit: 30 | None | Time limit: 120s |
| **Undos** | 3 | 3 | 2 |
| **Mini-game** | Pattern Lock Medium | Pattern Lock Medium | Pattern Lock Medium |
| **Exit tiles** | Right edge, 2 tiles | Top edge, 2 tiles | Right + top, 1 tile each |
| **Design** | First level with all mechanics. Generous limits to let player adjust. | No constraint — pure puzzle solving. Dense layout. | Time pressure + limited undos. Exit on two edges forces split attention. |

### Zone 8: Memory Challenge (Levels 16–17)

Purpose: Mini-game rotates to Memory Flip. Levels are simpler but mini-game is harder.

| | Level 16 | Level 17 |
|--|---------|----------|
| **Grid** | 8×8 | 8×8 |
| **Vehicles** | 5 | 6 |
| **Obstacles** | 3 | 4 |
| **Pedestrians** | 1 | 2 |
| **Barrier** | Yes | Yes |
| **Constraint** | None | Move limit: 25 |
| **Undos** | 4 | 3 |
| **Mini-game** | Memory Flip Medium | Memory Flip Medium |
| **Exit tiles** | Right edge, 1 tile | Right edge, 2 tiles |
| **Design** | Level layout is moderate — the main challenge is the memory mini-game. Player isn't overwhelmed on both fronts. | Move limit adds pressure to solve the grid efficiently before tackling memory. |

### Zone 9: Tight Constraints (Levels 18–20)

Purpose: High-pressure levels where every move and second counts.

| | Level 18 | Level 19 | Level 20 |
|--|---------|----------|----------|
| **Grid** | 9×9 | 9×9 | 9×9 |
| **Vehicles** | 7 | 8 | 8 |
| **Obstacles** | 5 | 6 | 6 |
| **Pedestrians** | 2 | 3 | 3 |
| **Barrier** | Yes | Yes | Yes |
| **Constraint** | Move limit: 20 | Time limit: 90s | Move limit: 18 |
| **Undos** | 2 | 2 | 2 |
| **Mini-game** | Memory Flip Medium | Memory Flip Medium | Memory Flip Medium |
| **Exit tiles** | Top edge, 2 tiles | Right edge, 2 tiles | Right + top, 1 tile each |
| **Design** | Few moves + limited undos. Near-perfect play required. | Tight timer + pedestrians force fast decisions. | Hardest of the memory zone. Minimal room for error. |

### Zone 10: Master (Levels 21–25)

Purpose: Hardest levels in the game. Any mini-game type at Hard difficulty. Maximum grid size. All mechanics.

| | Level 21 | Level 22 | Level 23 | Level 24 | Level 25 |
|--|---------|----------|----------|----------|----------|
| **Grid** | 10×10 | 10×10 | 11×11 | 11×11 | 12×12 |
| **Vehicles** | 8 | 9 | 9 | 10 | 10 |
| **Obstacles** | 6 | 7 | 8 | 8 | 10 |
| **Pedestrians** | 3 | 3 | 4 | 4 | 4 |
| **Barrier** | Yes | Yes | Yes | Yes | Yes |
| **Constraint** | Move limit: 30 | Time limit: 120s | Move limit: 25 | Time limit: 90s | Move limit: 20 |
| **Undos** | 2 | 2 | 1 | 1 | 1 |
| **Mini-game** | Pipe Puzzle Hard | Pattern Lock Hard | Memory Flip Hard | Pipe Puzzle Hard | Pattern Lock Hard |
| **Exit tiles** | Right, 2 tiles | Top, 2 tiles | Right + top | Right, 2 tiles | Right + top |
| **Design** | Large grid, many vehicles, limited undos. Pipes mini-game is timed. | Pattern Lock at max difficulty (6 buttons, seq len 8). Combines grid complexity with memory challenge. | Memory Flip at max (8 pairs, 16 moves). Only 1 undo for the main grid. | Dense obstacles + timed pipe puzzle. Near-perfect execution on both fronts. | Final level. Largest grid, most vehicles, most obstacles, hardest mini-game, only 1 undo, move limited. |

## Design patterns

### Blocking chain

The most common puzzle pattern: vehicle A blocks vehicle B, which blocks vehicle C, which is the only one that can reach the exit. Player must work backward through the chain.

Used in: Levels 2, 5, 8, 10, 13, 15, 18, 22, 25.

### Pedestrian timing puzzle

A pedestrian patrols a critical path. Player must move a vehicle into position while the pedestrian is on the far end of its route, then complete the move before it returns.

Used in: Levels 5, 6, 10, 12, 14, 19, 23.

### Multi-exit routing

Exit tiles on two edges. Player must decide which vehicle goes to which exit. Creates branching solution paths.

Used in: Levels 15, 20, 23, 25.

### Corridor squeeze

Obstacles form a narrow corridor that only one vehicle can pass at a time. Order matters.

Used in: Levels 4, 7, 11, 17, 21, 24.

### Undo trap

A deceptively simple layout where one wrong move causes a chain collision. Limited undos force careful play. Often combined with a blocking chain.

Used in: Levels 13, 18, 20, 23, 25.

## Difficulty curve reference

```
Difficulty
^
|                              L25
|                          L24
|                      L23
|                  L22
|              L21
|          L19 L20
|      L17 L18
|  L15 L16
|L13 L14
|L11 L12
|L9 L10
|L7 L8
|L5 L6
|L3 L4
|L1 L2
+------------------------------> Level
  1 3 5 7 9 11 13 15 17 19 21 23 25
```

Ramps gently through zones 1–5 (levels 1–10), steepens at zone 6 (constraints introduced), plateaus slightly in zone 8 (new mini-game type), then climbs steeply through zones 9–10.
