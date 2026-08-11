# Parking Jam — Unity Implementation Spec

## 1. Project Structure

```
Assets/
  Scenes/
    Main.unity              -- HUD, grid, game loop
    MiniGames/               -- one scene per mini-game variant
      MiniGame_Memory_Easy.unity
      MiniGame_Memory_Medium.unity
      MiniGame_Memory_Hard.unity
      MiniGame_Pipes_Easy.unity
      MiniGame_Pipes_Medium.unity
      MiniGame_Pipes_Hard.unity
      MiniGame_Pattern_Easy.unity
      MiniGame_Pattern_Medium.unity
      MiniGame_Pattern_Hard.unity
  Scripts/
    Core/
      GameManager.cs         -- main game loop, tick counter, undo stack
      MiniGameManager.cs     -- DontDestroyOnLoad, additive scene bridge
      InputHandler.cs        -- touch drag → grid movement (2D raycast)
    Grid/
      GridController.cs      -- Grid component setup, cell transforms (XY plane)
      OccupancyMap.cs        -- Dictionary<Vector3Int, IOccupant>
      IOccupant.cs           -- interface for vehicles, obstacles, etc.
    Vehicles/
      Vehicle.cs             -- grid position, orientation, occupied tiles
      VehicleMovement.cs     -- drag sweep, destination calculation
    Obstacles/
      Pedestrian.cs          -- patrol route, tick movement
      StaticObstacle.cs      -- occupies tiles, never moves
    Barriers/
      Barrier.cs             -- exit blocker, triggers mini-game
    Camera/
      CameraController.cs    -- orthographic camera, grid-framing
    UI/
      HUDController.cs       -- settings, currency, bottom buttons
      DailyMissionsUI.cs
      ChallengesUI.cs
      CollectionUI.cs
    Economy/
      EconomyManager.cs      -- coins, keys, skin unlocks
      SaveManager.cs         -- JSON load/save to persistentDataPath
    Skins/
      SkinController.cs      -- sprite swap by skin ID
      SkinDatabase.cs        -- ScriptableObject: skin ID → Sprite mapping
  Resources/
    SkinDatabase.asset
  StreamingAssets/
    Levels/
      level_001.json
      level_002.json
      ...
    PedestrianRoutes/
      route_001.json
      ...
  Prefabs/
    Vehicle.prefab
    Pedestrian.prefab
    Barrier.prefab
  Art/
    Sprites/
      Vehicles/
        Car.png
        Truck.png
        Bus.png
      Pedestrians/
        Pedestrian.png
      Barriers/
        BarrierGate.png
      ParkingLot/
        Ground.png
        Walls.png
    UI/
```

## 2. Core Systems

### 2.1 Grid

- Unity `Grid` component with **Rectangle** cell layout on the **XY plane**
- Cell size: `(1, 1, 0)` — one unit per tile on X and Y; Z is unused
- Origin at bottom-left of the parking lot, aligned so tiles extend along X (right) and Y (up)
- Tile positions stored as `Vector3Int(x, y, 0)` — Z is always 0. `Grid.WorldToCell()` and `Grid.CellToWorld()` handle the XY transform naturally
- Grid dimensions defined per-level in JSON (e.g., `gridWidth`, `gridHeight`)
- **Parking lot background**: rendered via a `Tilemap` component on a separate layer (`Background`). Static tiles (asphalt, markings) use `Tilemap` for batching. Vehicles are separate non-Tilemap GameObjects with `SpriteRenderer` snapped to grid positions via `Grid.CellToWorld()`

### 2.2 Occupancy Map

```csharp
public interface IOccupant
{
    Vector3Int[] OccupiedTiles { get; }
}

public class OccupancyMap
{
    private Dictionary<Vector3Int, IOccupant> _map = new();

    public bool IsTileFree(Vector3Int tile) => !_map.ContainsKey(tile);
    public void Place(IOccupant occupant) { foreach (var t in occupant.OccupiedTiles) _map[t] = occupant; }
    public void Remove(IOccupant occupant) { foreach (var t in occupant.OccupiedTiles) _map.Remove(t); }
    public IOccupant GetOccupant(Vector3Int tile) => _map.GetValueOrDefault(tile);
}
```

- Rebuilt incrementally: `Remove(oldTiles)` → `Place(newTiles)` on each move
- No iteration — O(1) lookup

### 2.3 Vehicle Movement

1. **Pointer down**: `InputHandler` uses the Input System's `Pointer` action to get the screen position, then runs a 2D `Physics2D.Raycast` (or `Physics2D.GetPointCollider`). Hit vehicle → lock axis to its orientation.
2. **Drag**: Convert pointer world position to cell via `Grid.WorldToCell()`. Project onto the locked axis (clamp X to the vehicle's current column for horizontal, clamp Y for vertical).
3. **Pointer release**: Sweep from current position along the locked axis in the drag direction. For each tile step, check `OccupancyMap.IsTileFree()`. The destination is the last free tile before a blocked tile or grid edge.
4. **Snap**: Lerp vehicle transform from current cell to destination cell over ~0.15 seconds. Snap to `Grid.CellToWorld(cell)`.
5. **Commit**: `OccupancyMap.Remove(vehicle)` → update `Vehicle.GridPosition` → `OccupancyMap.Place(vehicle)`. Push snapshot to undo stack.

### 2.4 Collision

- Evaluated **before** movement, during the sweep (step 3 above)
- Not a physics callback — purely grid-space
- If the sweep hits an occupied tile on the **first** step (vehicle can't move at all), no collision — the vehicle stays put and nothing is consumed
- If the sweep hits an occupied tile after moving at least one step, that's a **collision**
  - Consumes one Undo
  - **Cancels the Move**: the pre-move GridSnapshot is fully restored — vehicle, pedestrians, timer, and tick all return to their pre-move state; the cancelled move does not count as a tick
  - If the Undo pool is empty after deduction, the level restarts as a fresh attempt: the level's undos refill and any unspent bonus undos carry over

### 2.5 Undo Pool (collision-rollback store — there is no manual undo)

```csharp
public class UndoSystem
{
    private Stack<GridSnapshot> _snapshots = new();
    public int Remaining { get; private set; }

    public void Init(int levelUndos, int bonusUndos)
    {
        Remaining = levelUndos + bonusUndos;
        _snapshots.Clear();
    }

    public void SnapshotBeforeMove(GridSnapshot state)
    {
        _snapshots.Push(state);
    }

    public bool TryConsumeUndo(out GridSnapshot revertState)
    {
        if (Remaining <= 0 || _snapshots.Count == 0)
        {
            revertState = null;
            return false;
        }
        Remaining--;
        revertState = _snapshots.Pop();
        return true;
    }
}
```

- `GridSnapshot` is a JSON-serializable class containing all vehicle positions, pedestrian positions, the timer value, and the tick count
- Snapshot pushed **before** every move attempt (not after collision — the snapshot holds the state to revert *to*)
- On collision: pop the top snapshot, restore the **full snapshot** (vehicle, pedestrians, timer, tick), decrement Remaining
- There is no player-initiated undo: this stack exists solely to restore the pre-move world on a Cancelled Move
- Bonus undos (daily login) decremented first
- On a collision-forced level restart, the pool is re-initialised: the level's authored undos refill and any unspent bonus undos carry over

## 3. Gameplay Systems

### 3.1 Tick Counter

- `GameManager.Tick` increments by 1 after each completed vehicle move
- A Cancelled Move (collision) does not increment the tick — the tick is restored as part of the snapshot rollback
- Pedestrians advance one tile per tick
- Tick count is the number of moves the player has made this level
- Snapshot includes the current tick for replay/debug accuracy

### 3.2 Pedestrian Movement

```csharp
public class Pedestrian : MonoBehaviour, IOccupant
{
    public Vector3Int[] Route;       // patrol waypoints from level data
    public int CurrentRouteIndex;    // current position in route
    public bool Reversing;           // false → forward, true → backward

    public void OnGameTick()
    {
        int nextIndex = Reversing ? CurrentRouteIndex - 1 : CurrentRouteIndex + 1;
        if (nextIndex < 0 || nextIndex >= Route.Length)
        {
            Reversing = !Reversing;
            return;
        }
        Vector3Int nextTile = Route[nextIndex];
        if (_occupancyMap.IsTileFree(nextTile))
        {
            _occupancyMap.Remove(this);
            CurrentRouteIndex = nextIndex;
            transform.position = _grid.CellToWorld((Vector3Int)nextTile);
            _occupancyMap.Place(this);
        }
        // If blocked, wait one tick and recheck (no pathfinding)
    }
}
```

- Route defined in level JSON as an array of `[x, y]` pairs
- Pedestrian reverses at route ends, never loops
- When blocked by a vehicle, skips movement for that tick — no alternative behavior
- **Animation**: sprite flipbook via Unity `Animator Controller` (4-directional walk cycle or simple 2-directional for horizontal-only patrols). For v1, flipbook animation is sufficient; upgrade to `2D Animation` package (skeletal) only if pedestrian variety demands it

### 3.3 Barrier

```csharp
public class Barrier : MonoBehaviour, IOccupant
{
    public string MiniGameSceneName;  // set in level data

    public void OnTapped()
    {
        GameManager.Instance.Pause();       // freeze timer, disable input
        MiniGameManager.Instance.LoadMiniGame(MiniGameSceneName);
    }

    public void OnMiniGameCompleted()
    {
        _occupancyMap.Remove(this);         // barrier gone, exit free
        Destroy(gameObject);
        GameManager.Instance.Resume();
    }
}
```

- Barrier is placed on the outermost exit tile in the level JSON (at most one per level)
- While locked, the barrier blocks the **entire exit edge**: the sweep stops at the last inner-grid tile for *every* exit tile, not just the barrier's own — the level-wide locked check (below) runs alongside tile occupancy
- The barrier occupies its own tile while locked, so a vehicle dragged toward it stops one step short (bumper-to-gate)
- Tapping the barrier triggers the mini-game
- On unlock (mini-game completed or coin skip), the barrier is removed, the locked check is cleared, and vehicles can drive off through any exit tile

### 3.4 Mini-Game Bridge

```csharp
public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }
    public UnityEvent OnMiniGameCompleted;

    private void Awake() { DontDestroyOnLoad(gameObject); Instance = this; }

    public void LoadMiniGame(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
    }

    // Called by the mini-game scene on completion
    public void CompleteMiniGame()
    {
        OnMiniGameCompleted?.Invoke();
        SceneManager.UnloadSceneAsync(currentMiniGameScene);
    }
}
```

- Mini-game scenes are in Build Settings but not marked as the active scene
- Each mini-game prefab has a `MiniGameController` that calls `MiniGameManager.CompleteMiniGame()` on win
- The player can retry the mini-game freely — resets the mini-game scene without unloading/reloading
- See `docs/specs/mini-games.md` for the full designs of all three mini-game types (Pipe Puzzle, Pattern Lock, Memory Flip) and their difficulty variants

## 4. UI Layout

### 4.1 Canvas Structure

```
Canvas (Screen Space - Overlay)
  Canvas Scaler: Scale With Screen Size, Reference 1080x1920
  ├── TopBar
  │   ├── SettingsButton (Anchor: top-left)
  │   └── CurrencyDisplay (Anchor: top-right)
  │       ├── CoinIcon + CoinText
  │       └── KeyIcon + KeyText
  ├── BottomBar (Anchor: bottom-center)
  │   └── HorizontalLayoutGroup (distribute evenly)
  │       ├── DailyMissionsButton
  │       ├── ChallengesButton
  │       ├── CollectionButton
  │       └── EventsButton
  └── GameOverlay (fills screen, blocks input during pause/mini-game)
```

### 4.2 Screen Adaptation

- `Canvas Scaler`: Scale With Screen Size, 1080x1920 reference, Match = 1 (height-based)
- Bottom buttons use `HorizontalLayoutGroup` with `ChildAlignment: MiddleCenter`
- Safe area should be respected on iOS notch devices via `Screen.safeArea`

## 5. Economy and Persistence

### 5.1 Save Data Schema

```csharp
[System.Serializable]
public class SaveData
{
    public int coins;
    public int keys;
    public List<string> unlockedVehicleSkins = new();
    public List<string> unlockedPedestrianSkins = new();
    public string equippedVehicleSkinId;
    public string equippedPedestrianSkinId;
    public int lastCompletedLevel;
    public int dailyLoginStreak;
    public string lastLoginDate;             // "YYYY-MM-DD" for streak calc
    public int bonusUndosRemaining;          // from daily login
}
```

### 5.2 Storage

- **Settings** (SFX volume, music volume): `PlayerPrefs`
- **Game data**: `JsonUtility.ToJson()` → `File.WriteAllText()` to `Application.persistentDataPath + "/save.json"`
- **Economy Manager** loads on app start, saves on every transaction (coin earned/spent, skin unlocked)

### 5.3 Economy Rules

| Action | Effect |
|--------|--------|
| Complete a level | +Coins (base + bonus for remaining undos) |
| Complete a daily mission | +Coins, +bonusUndos |
| Complete a challenge | +Coins (higher than standard level) |
| Buy common skin | -Coins, +unlockedSkin |
| Buy exclusive skin | -Keys, +unlockedSkin |
| Skip barrier (pay) | -Coins, barrier removed without mini-game |
| Daily login | +bonusUndos (persist through restarts), +Skin on first login |

## 6. Level Data Format

Each level is a JSON file in `StreamingAssets/Levels/`:

```json
{
  "id": 1,
  "name": "Getting Started",
  "gridWidth": 8,
  "gridHeight": 8,
  "moveLimit": 20,
  "timeLimit": 0,
  "levelUndos": 4,
  "exitTiles": [[7, 3], [7, 4]],
  "vehicles": [
    {
      "id": "car_red",
      "tiles": [[0, 3], [1, 3]],
      "orientation": "horizontal"
    },
    {
      "id": "truck_blue",
      "tiles": [[3, 0], [3, 1], [3, 2]],
      "orientation": "vertical"
    }
  ],
  "staticObstacles": [
    { "tile": [2, 2] },
    { "tile": [2, 3] }
  ],
  "pedestrians": [
    {
      "route": [[5, 0], [5, 1], [5, 2], [5, 3], [5, 4]]
    }
  ],
  "barriers": [
    { "miniGameScene": "MiniGame_Pipes_Easy", "tile": [7, 3] }
  ]
}
```

- `moveLimit` or `timeLimit`: 0 means no constraint of that type
- `timeLimit` is in seconds
- `vehicles[].tiles` defines the starting position; the vehicle occupies all listed tiles
- `orientation` determines which axis the vehicle can move on
- See `docs/specs/level-schema.md` for the full schema reference, validation rules, and C# class definition

## 7. 2D Rendering

### 7.1 Camera

- **Orthographic camera**, looking down at the XY plane (top-down)
- Camera size tuned to fit the grid bounds with padding — no perspective distortion
- `CameraController.cs` locks the camera to grid center on level load
- No rotation, no dolly zoom

### 7.2 Sorting

- **Sorting Layers** for depth: `Background` (ground), `Gameplay` (vehicles, obstacles, pedestrians), `Overlay` (effects)
- Vehicles on the same layer render in order of their Y position (top-to-bottom) via **Sorting Group** or dynamic sortingOrder

### 7.3 Lighting

- **No dynamic lighting** — all sprites use unlit materials (URP 2D Renderer's default)
- 2D lights optional for visual polish (not required for v1)

### 7.4 Vehicle Scale

- Each vehicle is a `SpriteRenderer` with a **BoxCollider2D** matching its tile footprint
- 1-tile sprite: `1x1` units, 2-tile: `2x1`, 3-tile: `3x1`
- Pivot at bottom-center of the sprite, aligned to the grid cell origin
- Sprite pixel size is authored at 128-256 PPI; world unit scale set via PPU on the Sprite import

## 8. Input Handling

```csharp
public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference _pointerAction;
    private Vehicle _selectedVehicle;
    private Vector3Int _dragStartCell;
    private Camera _camera;

    void Awake() { _camera = Camera.main; }

    void OnEnable() => _pointerAction.action.performed += OnPointerPerformed;
    void OnDisable() => _pointerAction.action.performed -= OnPointerPerformed;

    private void OnPointerPerformed(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = ctx.ReadValue<Vector2>();
        Vector3 worldPos = _camera.ScreenToWorldPoint(screenPos);

        if (ctx.phase == InputActionPhase.Started)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null && hit.TryGetComponent<Vehicle>(out var vehicle))
            {
                _selectedVehicle = vehicle;
                _dragStartCell = _grid.WorldToCell(worldPos);
            }
        }
        else if (ctx.phase == InputActionPhase.Performed && _selectedVehicle != null)
        {
            Vector3Int cellPos = _grid.WorldToCell(worldPos);
            Vector3Int dragDelta = cellPos - _dragStartCell;

            Vector3Int direction = Vector3Int.zero;
            if (_selectedVehicle.Orientation == Orientation.Horizontal)
                direction.x = System.Math.Sign(dragDelta.x);
            else
                direction.y = System.Math.Sign(dragDelta.y);

            if (direction != Vector3Int.zero)
                _selectedVehicle.TryMove(direction);

            _selectedVehicle = null;
        }
    }
}
```

- `TryMove(direction)` runs the sweep, checks occupancy, triggers animation
- The `Pointer` action is bound to both `Mouse` and `Touch` devices — no per-platform branching
- Touch simulation via `InputSystemUIInputModule` enables mouse testing in Editor
- Edge case: if the vehicle can't move in the dragged direction, do nothing (no feedback other than staying put)

## 9. Project & Build Settings

- **Project Mode**: 2D (set in Editor → Project Settings → Editor → Default Behavior Mode)
- **Render Pipeline**: URP (Universal Render Pipeline) **2D Renderer**
- **Shadows**: Disabled (2D sprites, no dynamic lighting)
- **Scripting Backend**: IL2CPP
- **Target Architectures**: ARM64 (both Android and iOS)
- **Minimum API Level**: Android API 24
- **Minimum iOS Version**: iOS 13
- **Texture Compression**: ASTC (Android), PVRT (iOS fallback)
- **Graphics APIs**: Vulkan (Android primary), Metal (iOS), OpenGL ES 3.0 (fallback)
- **Multithreaded Rendering**: Enabled
- **Strip Engine Code**: Enabled
- **Optimize Mesh Data**: Enabled
- **Scenes in Build**: Main.unity + all mini-game scenes (not marked active)
- **iOS**: requires Mac with Xcode for final build; Apple Developer Program membership for distribution
- **Full mobile build reference**: `docs/research/unity-requirements.md` §5

## 10. References

| Topic | Source |
|-------|--------|
| Grid + Tilemap | `docs/research/unity-requirements.md` §1, [Grid Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/tilemaps/grid-reference.html) |
| 2D mode vs 3D | `docs/research/unity-requirements.md` §2, [2D/3D Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/2Dor3D.html) |
| UI (uGUI) | `docs/research/unity-requirements.md` §3, [UI comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) |
| Data persistence | `docs/research/unity-requirements.md` §4, [JsonUtility](https://docs.unity3d.com/6000.5/Documentation/Manual/json-serialization.html) |
| Mobile builds | `docs/research/unity-requirements.md` §5 |
| Animation | `docs/research/unity-requirements.md` §6, [Animator Controller](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AnimatorController.html) |
| Mini-game scenes | `docs/research/unity-requirements.md` §7, [Additive scenes](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html) |
| Economy | `docs/research/unity-requirements.md` §8, [Unity Economy](https://docs.unity.com/en-us/economy) |
| Level JSON schema | `docs/specs/level-schema.md` |
| Mini-game designs | `docs/specs/mini-games.md` |
| All ADRs | `docs/adr/` (ADR-0001 through ADR-0010) |
