# Parking Jam

A sliding-block puzzle game where the player clears levels by moving vehicles along their orientation axis on a discrete grid.

## Language

### Core Concepts

**Vehicle**:
A car, truck, or bus on the grid that occupies 1–3 tiles and has a fixed orientation.
Vehicles are the primary pieces the player interacts with. They occupy a contiguous line of tiles orthogonal to the grid and cannot change orientation during a level.
_Avoid_: Car (when referring to non-player-blocking pieces), block, piece

**Vehicle Orientation**:
The axis (horizontal or vertical) a vehicle is locked to. A horizontal vehicle moves left/right; a vertical vehicle moves up/down. Orientation is set at level-design time and never changes during play.
_Avoid_: Direction, facing

**Grid** (also Inner Grid):
The discrete 2D rectangle of tiles where gameplay occurs. The origin (tile 0,0) is at the bottom-left of the parking lot; positive X extends right, positive Y extends up. Vehicles, obstacles, and pedestrians are placed on grid tiles. Beyond the grid edges lies the Exit Lane.
_Avoid_: Board, map, lot, outer grid

**Exit Edge**:
The boundary of the inner grid. A vehicle that reaches the exit edge during a Move has left the parking lot — it is indestructible, no longer interactable, and enters the Clear animation. The exit edge is not a tile; it is the line between the inner grid and the exit lane.
_Avoid_: Goal, finish, end

**Exit Lane**:
The strip beyond the inner grid edges where vehicles travel during the exit animation. A vehicle enters the exit lane automatically when dragged past the inner grid edge. Once in the exit lane the vehicle is indestructible and no longer part of gameplay — an auto-drive animation carries it forward and off-screen.
_Avoid_: Road, path, driveway

**Tile**:
A single cell on the inner grid. Every vehicle, obstacle, pedestrian, or empty space occupies exactly one tile. Vehicles occupy contiguous adjacent tiles along their orientation axis.
_Avoid_: Square, block

### Gameplay

**Move**:
A single slide of a vehicle along its orientation axis from its current position to a new position. The player initiates a move by selecting a vehicle and dragging along its axis; the vehicle slides until it reaches a blocked tile or the grid edge. A move ends when the vehicle stops, regardless of distance travelled. A move that completes advances the Game Tick; a move erased by a Collision is a Cancelled Move and does not.
_Avoid_: Swipe, turn

**Drag**:
The primary input interaction. The player selects a vehicle then drags along its orientation axis to move it. The interaction is uniform across input devices — a click-and-drag or touch-and-drag produces the same result.
_Avoid_: Swipe, flick

**Clear**:
The win condition: every vehicle is moved off the inner grid, through the Exit Lane, past the Barrier, and off-screen. The Barrier must be unlocked (via Mini-game) before any vehicle can exit — vehicles cannot enter the exit lane until the barrier is open. Each vehicle's exit triggers a brief auto-drive animation through the exit lane.
_Avoid_: Finish, complete, win

**Level Data**:
The definition of a level. Contains grid size, vehicle placements (position, orientation, length, type), obstacle positions, pedestrian routes, exit edge location, level constraints, barrier presence, associated mini-game, and undo count.
_Avoid_: Level config, map file

**Game Save**:
The record of a player's persistent progress. Stores coin balance, key balance, unlocked skins, equipped skin, last cleared level, and daily login streak. Updated on state changes and restored on subsequent sessions.
_Avoid_: Save file, profile

**Collision**:
A grid-space check that occurs when a vehicle, mid-Move, stops one tile short of a tile occupied by an obstacle or another vehicle. Collision is evaluated logically before movement — there is no physics simulation. A drag that cannot slide at all does not collide. Each collision consumes one Undo and cancels the Move, turning it into a Cancelled Move. When the Undo pool is empty, the next collision restarts the level.
_Avoid_: Hit, crash, bump, damage

**Undo**:
A limited, per-level resource consumed by collisions. Each level grants a fixed number of undos authored in its Level Data (typically 1–5); daily login bonus undos are added on top (consumed first) and persist through level restarts. An undo is spent only when a Collision cancels a Move — there is no manual undo. When the pool is empty, the next collision restarts the level as a fresh attempt: the level's undos refill and any unspent bonus undos carry over.
_Avoid_: Life, health, continue, retry

**Cancelled Move**:
A Move that a Collision erased. The world is restored to its exact pre-move snapshot — the vehicle returns to its previous position, pedestrians and the timer return to their prior state, and the move does not advance the Game Tick. A cancelled move is never player-initiated; it is always the consequence of a Collision.
_Avoid_: Undo (the resource), revert, snap back

**Game Tick**:
The level's turn counter. One tick elapses per completed Move; pedestrians advance one tile per tick and recheck their blocking wait once per tick. A Cancelled Move does not advance the tick.
_Avoid_: Turn, step

**Welcome Gift**:
A one-time reward granted on the player's first login: one free Vehicle skin and one free Pedestrian skin. Separate from the recurring Daily Login Bonus.
_Avoid_: Starter pack, new-player reward

**Level Constraint**:
An optional restriction on a level: a move limit, a time limit, or neither. Only one constraint type applies to a given level; they are never combined.
_Avoid_: Difficulty, rule, condition

### Obstacles

**Obstacle**:
A non-vehicle entity on the grid that occupies a tile and cannot be moved. Vehicles cannot pass through obstacle tiles.
_Avoid_: Block, barrier (when not referring to the exit barrier)

**Static Obstacle**:
An obstacle that occupies a fixed tile permanently and never moves during a level. Walls and construction signs are static obstacles.
_Avoid_: Wall, object

**Pedestrian**:
An obstacle that follows a fixed patrol route defined as an ordered list of tile coordinates in the level data. Moves one tile per game tick along its route. When it reaches the end of its route it reverses direction. If a vehicle blocks the next tile, the pedestrian waits one tick and rechecks; it never pathfinds around vehicles or alters its route.
_Avoid_: Mobile obstacle, NPC, walker, enemy

### Progression

**Barrier**:
A gate, locked at level start, that occupies the outermost exit tile of the inner grid and blocks vehicles from leaving the parking lot. A level contains at most one barrier. The player must complete a Mini-game to unlock it; once unlocked, the barrier stays open for the duration of the level. While a barrier is locked it gates the whole lot: the exit edge is closed across all exit tiles, no vehicle can leave the inner grid, and a vehicle dragged toward any exit stops on the last inner-grid tile — bumper-to-gate in front of the barrier. After unlock, each vehicle that reaches the exit edge drives automatically through the exit lane and off-screen.
_Avoid_: Gate, lock, door

**Mini-game**:
A short challenge drawn from a fixed pool, triggered by interacting with a Barrier. When a mini-game starts, the main game state is preserved (timer and move counter are paused). On completion the mini-game unlocks the Barrier, allowing vehicles to leave the inner grid. If the mini-game is not completed, no vehicle can exit. The player can retry freely with no cost.
_Avoid_: Puzzle, challenge, side-quest

### Economy

**Coin**:
The in-game currency earned by completing levels, challenges, and daily missions. Spent on common Vehicle skins and Barrier skips.
_Avoid_: Gold, cash, gems, money

**Key**:
The premium currency bought with real money. Spent on exclusive Vehicle and Pedestrian skins. Cannot be earned through gameplay.
_Avoid_: Gem, ticket, token

**Skin**:
A purely cosmetic appearance for a Vehicle or a Pedestrian obstacle. The equipped skin is applied level-wide: it re-colours every vehicle in the level, regardless of model. Skin unlocks are granted via login rewards, level completion, Coin purchases, and Key purchases. They have no effect on gameplay.
_Avoid_: Costume, paint, outfit
