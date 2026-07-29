# Parking Jam

A sliding-block puzzle game built in Unity where the player clears cars from a crowded parking lot by moving them along their orientation axis on a discrete grid.

## Language

### Core Concepts

**Vehicle**:
A car, truck, or bus on the grid that occupies 1–3 tiles and has a fixed orientation.
Vehicles are the primary pieces the player interacts with. They occupy a contiguous line of tiles orthogonal to the grid and cannot change orientation during a level.
_Avoid_: Car (when referring to non-player-blocking pieces), block, piece

**Vehicle Orientation**:
The axis (horizontal or vertical) a vehicle is locked to. A horizontal vehicle moves left/right; a vertical vehicle moves up/down. Orientation is set at level-design time and never changes during play.
_Avoid_: Direction, facing

**Grid**:
A discrete 2D rectangle of tiles aligned to Unity's Grid component. The grid origin (tile 0,0) is at the bottom-left of the parking lot; positive X extends right, positive Y extends up. Each tile occupies one unit cell in Unity grid space. Vehicles and obstacles are placed on grid tiles and their positions are stored as `Vector3Int` cell coordinates.
_Avoid_: Board, map, lot

**Exit**:
The edge tile(s) on the grid where vehicles drive out. The primary goal is to get all (or a designated) vehicle to the exit.
_Avoid_: Goal, finish, end

**Tile**:
A single cell on the grid. Every vehicle, obstacle, or empty space occupies exactly one tile. Vehicles occupy contiguous adjacent tiles along their orientation axis.
_Avoid_: Square, block

### Gameplay

**Move**:
A single slide of a vehicle along its orientation axis from its current position to a new position. The player initiates a move by touching and dragging the vehicle along its axis; the vehicle slides until it reaches the next blocked tile or the grid edge. A move ends when the vehicle stops, regardless of distance travelled.
_Avoid_: Swipe, turn

**Collision**:
A grid-space check that occurs when a vehicle attempts to move into a tile occupied by an obstacle or another vehicle. Collision is evaluated logically before movement — there is no physics simulation. Each collision consumes one Undo. When the Undo pool is empty, the next collision restarts the level.
_Avoid_: Hit, crash, bump, damage

**Undo**:
A limited per-level resource consumed by collisions. Each level grants 3–5 undos (randomised at level load). Daily login bonus undos are added on top (consumed first) and persist through level restarts. When the pool is empty, the next collision forces a level restart.
_Avoid_: Life, health, continue, retry

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

**Mobile Obstacle**:
An obstacle that follows a fixed patrol route defined as an ordered list of tile coordinates in the level data. The pedestrian moves one tile per game tick along its route. When it reaches the end of its route it reverses direction. If a vehicle blocks the next tile, the pedestrian waits one tick and rechecks; it never pathfinds around vehicles or alters its route. Pedestrians are mobile obstacles.
_Avoid_: NPC, walker, enemy

### Progression

**Barrier**:
An entity occupying the exit tile that blocks the exit until removed. The barrier is visible from the start of the level and is removed by completing a Mini-game. Once unlocked it stays unlocked for the duration of the level. Unlike static and mobile obstacles, the barrier can be removed through gameplay.
_Avoid_: Gate, lock, door

**Mini-game**:
A short challenge drawn from a fixed pool of scene prefabs, triggered by tapping a Barrier. When a mini-game starts, the main game state is preserved (timer and move counter are paused). The mini-game scene loads additively over the main scene using `SceneManager.LoadSceneAsync` with `LoadSceneMode.Additive`. On completion it signals back to the main game via a UnityEvent and unloads itself. The player can retry freely with no cost.
_Avoid_: Puzzle, challenge, side-quest

### Economy

**Coin**:
The in-game currency earned by completing levels, challenges, and daily missions. Spent on common Vehicle skins and Barrier skips.
_Avoid_: Gold, cash, gems, money

**Key**:
The premium currency bought with real money. Spent on exclusive Vehicle and Pedestrian skins. Cannot be earned through gameplay.
_Avoid_: Gem, ticket, token

**Skin**:
A purely cosmetic appearance for a Vehicle or a Pedestrian obstacle. Skin unlocks are granted via login rewards, level completion, Coin purchases, and Key purchases. They have no effect on gameplay.
_Avoid_: Costume, paint, outfit
