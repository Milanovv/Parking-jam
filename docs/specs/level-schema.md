# Parking Jam — Level JSON Schema

## File format

Each level is a single `.json` file in `StreamingAssets/Levels/`, named `level_{id:03d}.json`. UTF-8 encoding, no BOM. Loaded via `File.ReadAllText` + `JsonUtility.FromJson<LevelData>()` (see ADR-0003).

## Schema (v1)

```json
{
  "id": 1,
  "name": "Getting Started",
  "gridWidth": 8,
  "gridHeight": 8,
  "moveLimit": 0,
  "timeLimit": 0,
  "levelUndos": 4,
  "exitTiles": [[7, 3], [7, 4]],
  "vehicles": [
    {
      "id": "car_red",
      "tiles": [[0, 3], [1, 3]],
      "orientation": "horizontal"
    }
  ],
  "staticObstacles": [
    { "tile": [2, 2] }
  ],
  "pedestrians": [
    {
      "route": [[5, 0], [5, 1], [5, 2], [5, 3], [5, 4]]
    }
  ],
  "barriers": [
    {
      "miniGameScene": "MiniGame_Pipes_Easy",
      "tile": [7, 3]
    }
  ],
  "exitCurve": [
    { "x": 7, "y": 3 },
    { "x": 8, "y": 3 },
    { "x": 9, "y": 4 },
    { "x": 11, "y": 3 }
  ]
}
```

## Field reference

### Root object

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `id` | integer | yes | — | Unique level identifier. 1-based, sequential. Must not collide with any other level file. |
| `name` | string | yes | — | Human-readable level name. Shown in level select and loading screens. Max 32 characters. |
| `gridWidth` | integer | yes | — | Number of tiles on the X axis. Min: 5, Max: 12. |
| `gridHeight` | integer | yes | — | Number of tiles on the Y axis. Min: 5, Max: 12. |
| `moveLimit` | integer | no | 0 | Maximum moves allowed. 0 = no limit. Never combined with `timeLimit` (only one constraint per level). |
| `timeLimit` | integer | no | 0 | Time limit in seconds. 0 = no limit. Never combined with `moveLimit`. |
| `levelUndos` | integer | no | 3 | Undos granted at level start (before daily login bonus). Min: 1, Max: 5. |
| `exitTiles` | array of [x, y] | yes | — | Tiles that form the exit edge. At least 1 tile. All must be on the grid boundary (x = 0, x = gridWidth-1, y = 0, or y = gridHeight-1). |
| `vehicles` | array of Vehicle | yes | — | At least 1 vehicle. |
| `staticObstacles` | array of StaticObstacle | no | [] | Static obstacles placed at level start. |
| `pedestrians` | array of Pedestrian | no | [] | Pedestrian patrol routes. |
| `barriers` | array of Barrier | no | [] | Exit barrier. At most **one** entry per level; if present, its `tile` must match an `exitTile`. |
| `exitCurve` | array of {x, y} | no | null | 4 control points (tile-space coordinates, may lie off-grid) for the exit-lane auto-drive curve during Clear. First two points should start at the exit edge. When omitted, the game uses a default straight-then-arc shape (ADR-0011 / T8). Runtime JSON uses object form `{"x":..,"y":..}` — `JsonUtility` (ADR-0003) cannot parse short pair arrays `[x, y]`. |

### Vehicle

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `id` | string | yes | — | Unique vehicle identifier within the level. Used for debug logging and skin assignment. Convention: `{type}_{colour}` e.g. `car_red`, `truck_blue`. |
| `tiles` | array of [x, y] | yes | — | Tiles this vehicle occupies. Must be contiguous along a single axis. Length = vehicle size (1–3 tiles). |
| `orientation` | string | yes | — | `"horizontal"` or `"vertical"`. Must match the axis of `tiles`: horizontal = same Y across all tiles, vertical = same X across all tiles. |

### StaticObstacle

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `tile` | [x, y] | yes | — | Grid position. Must not overlap with any vehicle, pedestrian route, barrier, or another obstacle. |

### Pedestrian

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `route` | array of [x, y] | yes | — | Patrol waypoints in order. Min length: 2. Max length: 16. Route must stay within grid bounds. Pedestrian reverses at route ends (never loops). |

### Barrier

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `miniGameScene` | string | yes | — | Scene name in Build Settings, e.g. `"MiniGame_Pipes_Easy"`. Must match one of the 9 mini-game scene names (see docs/specs/mini-games.md). |
| `tile` | [x, y] | yes | — | Grid position. Must be on the outermost exit tile (one cell before the vehicle would leave the grid). Must match one of the `exitTiles`. |

## Coordinate system

- `[x, y]` pairs use the same XY plane as the Grid component in `GridController.cs`
- Origin (0, 0) is bottom-left of the grid
- X increases to the right, Y increases upward
- All coordinates are 0-indexed
- Valid range: x ∈ [0, gridWidth-1], y ∈ [0, gridHeight-1]

## Validation rules

All checks run at level load. Invalid levels log an error and are skipped (level select shows them as locked/unplayable).

### 1. Tile uniqueness

No two entities (vehicles, obstacles, pedestrians, barriers) may occupy the same tile at level start. Overlapping `exitTiles` with vehicles/obstacles is allowed (exit tiles overlap with vehicles that need to reach them), but barriers must not overlap with vehicles or obstacles.

### 2. Vehicle integrity

```csharp
foreach (var vehicle in level.vehicles) {
    // Must be contiguous along one axis
    if (vehicle.orientation == "horizontal") {
        int y = vehicle.tiles[0].y;
        List<int> xs = vehicle.tiles.Select(t => t.x).OrderBy(x => x).ToList();
        for (int i = 1; i < xs.Count; i++)
            if (xs[i] != xs[i-1] + 1) → INVALID  // gap in tiles
        for (int i = 0; i < xs.Count; i++)
            if (vehicle.tiles[i].y != y) → INVALID  // mixed Y
    }
    if (vehicle.orientation == "vertical") {
        int x = vehicle.tiles[0].x;
        List<int> ys = vehicle.tiles.Select(t => t.y).OrderBy(y => y).ToList();
        for (int i = 1; i < ys.Count; i++)
            if (ys[i] != ys[i-1] + 1) → INVALID  // gap in tiles
        for (int i = 0; i < ys.Count; i++)
            if (vehicle.tiles[i].x != x) → INVALID  // mixed X
    }
    // Length 1-3
    if (vehicle.tiles.Length < 1 || vehicle.tiles.Length > 3) → INVALID
}
```

### 3. Exit tile boundary

Every `exitTile` must be on the grid boundary:

```csharp
bool IsOnBoundary(int x, int y, int w, int h) =>
    x == 0 || x == w - 1 || y == 0 || y == h - 1;
```

### 4. Constraint exclusivity

`moveLimit` and `timeLimit` must not both be > 0 in the same level file.

```csharp
if (level.moveLimit > 0 && level.timeLimit > 0) → INVALID
```

### 5. Barrier-exit alignment

If `barriers` is non-empty, it must contain exactly one barrier whose tile appears in `exitTiles`.

```csharp
if (barriers.Length > 1) → INVALID  // at most one barrier per level
if (barriers.Length == 1) {
    bool aligned = exitTiles.Any(e => e.x == barriers[0].tile.x && e.y == barriers[0].tile.y);
    if (!aligned) → INVALID  // barrier must sit on an outermost exit tile
}
```

### 6. Pedestrian route bounds

```csharp
foreach (var p in level.pedestrians)
    foreach (var tile in p.route)
        if (tile.x < 0 || tile.x >= gridWidth || tile.y < 0 || tile.y >= gridHeight)
            → INVALID
```

## Level file checklist

Before adding a level file, verify:

- [ ] `id` is unique and sequential
- [ ] `gridWidth` and `gridHeight` are 5–12
- [ ] At least 1 vehicle, max 3 tiles per vehicle
- [ ] Vehicle tiles are contiguous
- [ ] `orientation` matches tile layout
- [ ] No overlapping start positions between vehicles/obstacles/pedestrians
- [ ] `exitTiles` are on the grid boundary
- [ ] Only one of `moveLimit` / `timeLimit` is set
- [ ] `levelUndos` is 1–5
- [ ] At most one barrier; `barriers[0].tile` matches an `exitTile` (if barriers present)
- [ ] Pedestrian routes are within bounds and have ≥2 waypoints
- [ ] `miniGameScene` is one of the 9 valid scene names
- [ ] `exitCurve` (if present) has exactly 4 points, first near the exit edge

## C# class definition

```csharp
[System.Serializable]
public class LevelData
{
    public int id;
    public string name;
    public int gridWidth;
    public int gridHeight;
    public int moveLimit;      // 0 = no limit
    public int timeLimit;      // 0 = no limit
    public int levelUndos = 3; // default
    public Vector2Int[] exitTiles;
    public VehicleData[] vehicles;
    public StaticObstacleData[] staticObstacles = System.Array.Empty<StaticObstacleData>();
    public PedestrianData[] pedestrians = System.Array.Empty<PedestrianData>();
    public BarrierData[] barriers = System.Array.Empty<BarrierData>();
    public Vector2Int[] exitCurve; // optional; 4 control points, null = default shape
}

[System.Serializable]
public class VehicleData
{
    public string id;
    public Vector2Int[] tiles;
    public string orientation; // "horizontal" | "vertical"
}

[System.Serializable]
public class StaticObstacleData
{
    public Vector2Int tile;
}

[System.Serializable]
public class PedestrianData
{
    public Vector2Int[] route;
}

[System.Serializable]
public class BarrierData
{
    public string miniGameScene;
    public Vector2Int tile;
}
```

Note: coordinate fields use the object form `{"x":..,"y":..}` in runtime JSON. `JsonUtility` (ADR-0003) maps an array of numbers like `[7, 3]` onto neither `Vector2Int` nor a custom struct — every `[x, y]` shown above is authoring notation for `{"x": x, "y": y}`.
