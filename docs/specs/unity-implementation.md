# Parking Jam — Unity Implementation Spec

## 1. Project Structure

```
Assets/
  Scenes/
    Main.unity              -- HUD, grid, game loop
    MiniGames/               -- one scene per mini-game
      MiniGame_Pipes.unity
      MiniGame_Memory.unity
      ...
  Scripts/
    Core/
      GameManager.cs         -- main game loop, tick counter, undo stack
      MiniGameManager.cs     -- DontDestroyOnLoad, additive scene bridge
      InputHandler.cs        -- touch drag → grid movement (3D raycast)
    Grid/
      GridController.cs      -- Grid component setup, cell transforms (XZ plane)
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
      CameraController.cs    -- perspective camera, fixed angle, orbit/dolly
    UI/
      HUDController.cs       -- settings, currency, bottom buttons
      DailyMissionsUI.cs
      ChallengesUI.cs
      CollectionUI.cs
    Economy/
      EconomyManager.cs      -- coins, keys, skin unlocks
      SaveManager.cs         -- JSON load/save to persistentDataPath
    Skins/
      SkinController.cs      -- model swap by skin ID
      SkinDatabase.cs        -- ScriptableObject: skin ID → Mesh/Material mapping
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
    Models/
      Vehicles/
        Car.fbx
        Truck.fbx
        Bus.fbx
      Pedestrians/
        Pedestrian.fbx
      Barriers/
        BarrierGate.fbx
      ParkingLot/
        Ground.fbx
        Walls.fbx
    Materials/
    Textures/
    UI/
```

## 2. Core Systems

### 2.1 Grid

- Unity `Grid` component with **Rectangle** cell layout on the **XZ plane**
- Cell size: `(1, 1, 1)` — one unit per tile on X and Z; Y is unused (ground plane)
- Origin at bottom-left of the parking lot, aligned so tiles extend along X (right) and Z (forward)
- Tile positions stored as `Vector3Int(x, 0, z)` — Y is always 0, representing the ground plane. `Grid.WorldToCell()` and `Grid.CellToWorld()` handle the XZ transform naturally
- Grid dimensions defined per-level in JSON (e.g., `gridWidth`, `gridHeight`)
- All gameplay logic references the XZ plane — Y is reserved for model height (visual only)

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

1. **Touch down**: `InputHandler` runs a 3D `Physics.Raycast` from camera through touch position. Hit vehicle → lock axis to its orientation.
2. **Drag**: Convert touch world position to cell via `Grid.WorldToCell()`. The hit point is on the XZ plane; Y is discarded. Project onto the locked axis (clamp X to the vehicle's current row for horizontal, clamp Z for vertical).
3. **Touch release**: Sweep from current position along the locked axis in the drag direction. For each tile step, check `OccupancyMap.IsTileFree()`. The destination is the last free tile before a blocked tile or grid edge.
4. **Snap**: Lerp vehicle transform from current cell to destination cell over ~0.15 seconds. Snap to `Grid.CellToWorld(cell)` with the model's Y offset preserved.
5. **Commit**: `OccupancyMap.Remove(vehicle)` → update `Vehicle.GridPosition` → `OccupancyMap.Place(vehicle)`. Push snapshot to undo stack.

### 2.4 Collision

- Evaluated **before** movement, during the sweep (step 3 above)
- Not a physics callback — purely grid-space
- If the sweep hits an occupied tile on the **first** step (vehicle can't move at all), no collision — the vehicle stays put
- If the sweep hits an occupied tile after moving at least one step, that's a **collision**
  - Consumes one Undo
  - Vehicle snaps back to the previous free tile (the destination found during sweep)
  - If Undo pool is empty after deduction, the level restarts immediately

### 2.5 Undo Stack

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

- `GridSnapshot` is a JSON-serializable class containing all vehicle positions, pedestrian positions, and timer value
- Snapshot pushed **before** every move attempt (not after collision — the snapshot holds the state to revert *to*)
- On collision: pop the top snapshot, restore positions, decrement Remaining
- Bonus undos (daily login) decremented first

## 3. Gameplay Systems

### 3.1 Tick Counter

- `GameManager.Tick` increments by 1 after each completed vehicle move
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

- Route defined in level JSON as an array of `[x, z]` pairs
- Pedestrian reverses at route ends, never loops
- When blocked by a vehicle, skips movement for that tick — no alternative behavior

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

- Barrier is placed on the exit tile(s) in the level JSON
- Tapping the barrier triggers the mini-game
- Barrier does NOT implement `IOccupant` as an obstacle — vehicles can pass through its tile once removed. During gameplay the exit tile is blocked; removal means the vehicle can drive off.

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
    { "miniGameScene": "MiniGame_Pipes", "tile": [7, 3] }
  ]
}
```

- `moveLimit` or `timeLimit`: 0 means no constraint of that type
- `timeLimit` is in seconds
- `vehicles[].tiles` defines the starting position; the vehicle occupies all listed tiles
- `orientation` determines which axis the vehicle can move on

## 2.6 2.5D Rendering

### Camera

- **Perspective camera**, positioned at ~45 degrees above the grid, looking down at the XZ plane
- Field of view: ~40–50 degrees for a natural parking lot view
- Camera locked to the grid bounds — no free rotation (but optional dolly zoom for UI polish)
- `CameraController.cs` handles positioning: `transform.position = new Vector3(gridCenter.x, height, gridCenter.z - distance)`

### Sorting

- No manual sorting layers — 3D models sort automatically via **Z-buffer**
- The perspective camera naturally renders models closer to the lens on top
- Ground plane (parking lot) renders behind all vehicles via depth

### Lighting

- **Directional light** at an angle matching the camera for consistent shadows
- Soft shadows on vehicles cast onto the ground plane
- Ground plane receives shadows; vehicles cast and receive
- URP 3D Renderer with shadows enabled (Shadow Resolution: Medium on mobile)

### Vehicle Scale

- Vehicle prefab has a `BoxCollider` (3D, not 2D). A script sets the collider size based on tile count
- 1-tile model: `1x1x0.5`, 2-tile: `2x1x0.5`, 3-tile: `3x1x0.5`
- Model is centred on the grid cell(s) with a Y-offset for wheel height (~0.25 units above ground)

## 7. Input Handling

```csharp
public class InputHandler : MonoBehaviour
{
    private Vehicle _selectedVehicle;
    private Vector3 _dragStartWorld;
    private Vector3Int _dragStartCell;
    private Camera _camera;

    void Awake() { _camera = Camera.main; }

    void Update()
    {
        if (!Input.GetMouseButton(0)) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Input.GetMouseButtonDown(0))
        {
            // 3D raycast for vehicle hit
            if (Physics.Raycast(ray, out hit) &&
                hit.collider.TryGetComponent<Vehicle>(out var vehicle))
            {
                _selectedVehicle = vehicle;
                Vector3 hitOnGround = new Vector3(hit.point.x, 0, hit.point.z);
                _dragStartCell = _grid.WorldToCell(hitOnGround);
            }
        }
        else if (_selectedVehicle != null && Input.GetMouseButtonUp(0))
        {
            // Project ray onto XZ ground plane for grid cell
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 worldPoint = ray.GetPoint(enter);
                Vector3Int cellPos = _grid.WorldToCell(worldPoint);
                Vector3Int dragDelta = cellPos - _dragStartCell;

                Vector3Int direction = Vector3Int.zero;
                if (_selectedVehicle.Orientation == Orientation.Horizontal)
                    direction.x = System.Math.Sign(dragDelta.x);
                else
                    direction.z = System.Math.Sign(dragDelta.z);

                if (direction != Vector3Int.zero)
                    _selectedVehicle.TryMove(direction);
            }
        }
    }
}
```

- `TryMove(direction)` runs the sweep, checks occupancy, triggers animation
- On mobile: replace `Input.GetMouseButton` with `Input.touches[0]`
- Edge case: if the vehicle can't move in the dragged direction, do nothing (no feedback other than staying put)

## 8. Build Settings

- **Render Pipeline**: URP (Universal Render Pipeline) **3D Renderer**
- **Shadows**: Enabled (soft shadows, medium resolution)
- **Scripting Backend**: IL2CPP
- **Target Architectures**: ARM64 (both Android and iOS)
- **Minimum API Level**: Android API 24
- **Minimum iOS Version**: iOS 13
- **Texture Compression**: ASTC (Android), PVRT (iOS fallback)
- **Multithreaded Rendering**: Enabled
- **Strip Engine Code**: Enabled
- **Optimize Mesh Data**: Enabled
- **Scenes in Build**: Main.unity + all mini-game scenes (not marked active)
