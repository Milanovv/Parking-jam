# Easiest Architecture Patterns for a 2D Sliding-Block Puzzle (Parking Jam)

**Goal:** Pick the *simplest viable* patterns for a solo dev building Parking Jam in weeks, not months.

---

## 1. MVC (Model / View / Controller)

| Source | Claim |
|--------|-------|
| [Unity — How to architect code as your project scales](https://unity.com/how-to/how-architect-code-your-project-scales) | "Get as much code as possible out of MonoBehaviours. Separate logic from presentation so you can run your codebase in two modes: logic only and logic plus presentation." |
| [Unity — Level up your code with design patterns (official asset)](https://assetstore.unity.com/packages/essentials/tutorial-projects/level-up-your-code-with-design-patterns-and-solid-289616) | Includes MVP / MVC samples. Notes that MVC lets you "build a modular codebase" but warns "these are examples, not recommendations that promise a particular outcome." |
| [Unity Discussions — MVC for 2D games](https://discussions.unity.com/t/mvc-like-approach-for-2d-games-vs-entity-approach/679636) | Community consensus: "I know MVC is not good enough for games but I think I can use them for menus, panels." |
| [Unity Discussions — MVC + Command turn-based](https://discussions.unity.com/t/problem-using-mvc-command-pattern-for-a-turn-based-game-for-animations-events/940981) | "For the grid, store all data in a 2D array. Do logical comparisons in your own data storage — not in Unity objects." |

**Verdict:** Full MVC adds ~3 extra files per feature, requires dependency injection or service locator to wire controller to model to view, and offers little benefit for a solo dev who already knows what every file does. **Skip full MVC.** Instead, use the lightweight variant described below.

---

## 2. Lightweight Separation (The One to Use)

The single simplest pattern that fits this game:

> **Plain C# grid model + MonoBehaviour views + one GameManager coordinator.**

| Pattern | What it means for Parking Jam | Why it's simplest |
|---------|-------------------------------|-------------------|
| **Plain C# grid model** | `OccupancyMap` is a `Dictionary<Vector3Int, IOccupant>` — a pure C# class in its own file, no MonoBehaviour. | No MonoBehaviour overhead, no Unity lifecycle, unit-testable in isolation, ~40 lines of code. |
| **MonoBehaviour views** | `VehicleView.cs` is a thin script on the prefab that reads `Vehicle.Data.Position` and calls `transform.position = Grid.CellToWorld(...)`. | One concern per file. The view only *displays*. |
| **One GameManager coordinator** | `GameManager.cs` has public methods (`TryMoveVehicle`, `Undo`, `CompleteLevel`). It owns the OccupancyMap, the UndoSystem, and the list of views. It calls view methods when model state changes. | A single file you can read top-to-bottom to understand the entire game loop. No event buses, no injection. |

**Sources:**
- [Unity Discussions — Grid-based games: store data in 2D array](https://discussions.unity.com/t/what-s-the-optimal-way-to-represent-a-9x9-block-puzzle-grid-in-unity-for-solver-logic/1642826) — "For any tile-based game, do all logical comparisons in your own data storage. Otherwise you needlessly bind your game logic into Unity objects, making it 10× more complicated."
- [Unity — How to architect code as your project scales](https://unity.com/how-to/how-architect-code-your-project-scales) — "It is beneficial to handle input in a separate, self-contained place. The presentation needs to know what's going on but does not need full access to all systems."

---

## 3. ScriptableObject-Based Architecture

| Source | Claim |
|--------|-------|
| [Unity — ScriptableObject manual](https://docs.unity3d.com/6000.4/Documentation/Manual/class-ScriptableObject.html) | "The main value of a ScriptableObject is as a data store for shared data used by multiple objects at runtime, reducing memory by avoiding copies of values." |
| [Unity — Separate game data and logic with ScriptableObjects](https://unity.com/how-to/separate-game-data-logic-scriptable-objects) | "ScriptableObjects excel at storing static data — game statistics, configuration values for items, level layouts, character attributes." |
| [Unity — Create modular game architecture with ScriptableObjects (resource page)](https://unity.com/resources/create-modular-game-architecture-scriptableobjects-unity-6) | Full e-book + demo showing flyweight pattern, delegate objects, event channels. |
| [Unity — ScriptableObject-based enums](https://unity.com/how-to/scriptableobject-based-enums) | "Use ScriptableObjects to replace traditional enums for better comparison operations." |
| [Unity — ScriptableObject-based runtime sets](https://unity.com/how-to/scriptableobject-based-runtime-set) | "Runtime sets replicate why some devs use singletons — easy global access — without the baggage of dependencies." |
| [Unity — 6 ways ScriptableObjects can benefit your team](https://unity.com/blog/engine-platform/6-ways-scriptableobjects-can-benefit-your-team-and-your-code) | "Offloading data into ScriptableObjects can help with version control and prevent merge conflicts." |

**Verdict:** ScriptableObjects are **excellent for data-only assets** — level definitions, skin databases, economy config — but **overkill as a full architecture layer**. For this game:
- **Use them for:** `SkinDatabase.asset` (vehicle model/materials per skin), `LevelDataSO` (optional alternative to JSON), `EconomyConfigSO` (coin rewards per action).
- **Don't use them for:** Game state, undo stack, runtime vehicle instances (use plain C# for speed and testability).

---

## 4. Event-Driven / Observer Pattern

| Source | Claim |
|--------|-------|
| [Unity — ScriptableObject event channels](https://unity.com/how-to/scriptableobjects-event-channels-game-code) | "Event channels work like a radio tower between broadcaster and listener. They exist at project level so they persist through scene loads and can replace singletons." |
| [Unity — Architect game code with ScriptableObjects](https://unity.com/how-to/architect-game-code-scriptable-objects) | "An event architecture systems lets things respond to a change in state without constantly monitoring it in an update loop." |
| [Unity — Level up your code (official asset)](https://assetstore.unity.com/packages/essentials/tutorial-projects/level-up-your-code-with-design-patterns-and-solid-289616) | Includes Observer pattern demo. |
| [Unity Discussions — Observer pattern](https://discussions.unity.com/t/observer-pattern-who-should-be-responsible-for-finding-their-other-half/944264) | "The subject shouldn't find its observers. Observers know about their subjects — that's what makes them observers." |

**Verdict:** Event channels are useful for **cross-system notifications** (level complete → unlock next level), but for a solo dev they add indirection that makes code harder to follow. **Prefer direct method calls** through the GameManager: `GameManager.OnLevelComplete()` is easier to find and debug than `EventChannel_SO.Raise() → Listener → UnityEvent → method`. Use UnityEvents only for UI button clicks (where the Inspector wiring is genuinely helpful).

---

## 5. State Machine Pattern

| Source | Claim |
|--------|-------|
| [Unity — State machine basics (manual)](https://docs.unity3d.com/Manual/StateMachineBasics.html) | "A state machine is only in one state at a time. It remains in the same state until the conditions for a transition are met." |
| [Unity — State machine behaviours](https://docs.unity3d.com/2023.1/Documentation/Manual/StateMachineBehaviours.html) | "Attach a StateMachineBehaviour to an individual state to run code when the state machine enters, exits or remains in that state." |
| [Unity Discussions — Simple FSM](https://discussions.unity.com/t/a-right-way-to-do-a-gamemanager-with-fsm-in-unity-6/1581169) | Community veterans: "All generic FSM solutions I have seen do not actually improve the problem space. Just use an enum and a switch statement." |
| [Unity Discussions — How to deal with game states](https://discussions.unity.com/t/how-do-you-deal-with-game-states/757644) | "Don't over-engineer. If this is your first time dealing with game states, KISS and use a switch right up front." |

**Verdict:** An **enum + switch** in GameManager is sufficient for this game and adds zero extra files:

```csharp
public enum GameState { Menu, Playing, Paused, MiniGameActive, LevelComplete }
```

Sources urging against abstract FSM frameworks: [Unity Discussions](https://discussions.unity.com/t/a-right-way-to-do-a-gamemanager-with-fsm-in-unity-6/1581169), [Unity Discussions — Simple FSM for Unity](https://discussions.unity.com/t/simple-finite-state-machine-for-unity-manage-multiple-gameobject-behaviours/852662). The consensus is: "Getting a complex state machine operational is something you do not want to mix with inheritance."

---

## 6. Grid Representation

| Source | Claim |
|--------|-------|
| [Unity — Grid component reference](https://docs.unity3d.com/Manual/tilemaps/grid-reference.html) | `Grid` component with Rectangle layout, `Grid.WorldToCell()` and `Grid.CellToWorld()` for coordinate conversion. |
| [Unity — Tilemap.SetTiles scripting API](https://docs.unity3d.com/ScriptReference/Tilemaps.Tilemap.SetTiles.html) | `Vector3Int` positions — the idiomatic Unity type for tile coordinates. |
| [Unity — Scripting API: Grid](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/Grid.html) | "Grid is the base class for plotting a layout of uniformly spaced points and lines." |
| [Unity Discussions — Optimal grid representation](https://discussions.unity.com/t/what-s-the-optimal-way-to-represent-a-9x9-block-puzzle-grid-in-unity-for-solver-logic/1642826) | "Use a 2D array of tiles for all logic. It's the simplest and most testable approach." |
| [Unity Discussions — Grid duplication bug](https://discussions.unity.com/t/getting-duplicate-box-indexes-when-making-shuffle-logic-for-puzzle/1710672) | "For anything grid-based, manage your own data structure. Otherwise you needlessly bind logic into Unity objects, making it 10× more complicated." |

**Simplest possible grid for Parking Jam:**

```csharp
// Pure C# — no MonoBehaviour, no Tilemap component
public class OccupancyMap
{
    private readonly Dictionary<Vector3Int, IOccupant> _map = new();

    public bool IsFree(Vector3Int cell) => !_map.ContainsKey(cell);
    public void Place(IOccupant occupant, IEnumerable<Vector3Int> tiles)
    { foreach (var t in tiles) _map[t] = occupant; }
    public void Remove(IOccupant occupant, IEnumerable<Vector3Int> tiles)
    { foreach (var t in tiles) _map.Remove(t); }
}
```

No Tilemap component needed — the Grid component is used only for `WorldToCell`/`CellToWorld` conversion during drag input. The game logic operates entirely on `Vector3Int` coordinates.

---

## 7. Data Persistence

| Source | Claim |
|--------|-------|
| [Unity — PlayerPrefs API](https://docs.unity3d.com/ScriptReference/PlayerPrefs.html) | "Stores preferences between game sessions. No encryption. Stores string, float, int." |
| [Unity — JSON Serialization](https://docs.unity3d.com/Manual/json-serialization.html) | "Use JsonUtility for converting objects to and from JSON. Supports MonoBehaviour, ScriptableObject, and plain classes with [Serializable]." |
| [Unity — JsonUtility.ToJson](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/JsonUtility.ToJson.html) | "ToJson allocates GC memory only for the returned string. FromJsonOverwrite allocates only when fields are value-typed." |
| [Unity — Serialization best practices](https://docs.unity3d.com/6000.7/Documentation/Manual/script-serialization-best-practices.html) | "Share data between objects using ScriptableObjects. For save data, keep structures flat and simple." |

**Simplest approach:** PlayerPrefs for settings (volume), JsonUtility + file on disk for game data.

```csharp
[System.Serializable]
public class SaveData
{
    public int coins, keys, lastCompletedLevel, dailyLoginStreak;
    public List<string> unlockedSkins = new();
    public string equippedSkinId;
}

// Save
File.WriteAllText(Path.Combine(Application.persistentDataPath, "save.json"),
    JsonUtility.ToJson(saveData));

// Load
if (File.Exists(path))
    JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
```

No third-party libraries, no cloud dependency, no database. The entire persistence layer is ~20 lines. Level data lives as JSON in `StreamingAssets/` — readable in any text editor.

---

## 8. Undo / Command Pattern

| Source | Claim |
|--------|-------|
| [Unity Learn — Command pattern](https://learn.unity.com/course/design-patterns/tutorial/use-the-command-pattern-for-flexible-and-extensible-game-systems?version=6.0) | "The command pattern allows actions to be objects. Each command has Execute and Undo. Store in a stack for undo/redo." |
| [Unity Discussions — Undo/redo approach](https://discussions.unity.com/t/approach-to-creating-an-undo-redo-system/946942) | "Memento pattern or Command pattern. Memento saves full state snapshots. Command records changes and reverses them." |
| [Unity Discussions — Ctrl+Z logic](https://discussions.unity.com/t/ctrl-z-logic-on-building-style-game/918963) | "Simplest way: serialize state into a buffer. On undo, deserialize the topmost state." |

**Simplest for Parking Jam:** **Memento pattern** — snapshot the entire grid state before each move. The grid is small (8×8 = 64 cells) and vehicles are few (< 15 per level), so serializing a full snapshot is cheap:

```csharp
public class GridSnapshot
{
    public List<VehicleState> VehicleStates = new();
    public int Tick;
}

// Before each move:
_undoStack.Push(new GridSnapshot(occupancyMap, currentTick));

// On undo:
if (_undoStack.TryPop(out var snap)) snap.Restore(occupancyMap);
```

No `ICommand` interface, no per-action undo logic, no redo system to maintain. The snapshot is a `[Serializable]` class that can be JSON-serialized for replay/debug. This is the **lowest lines-of-code approach** and matches the Unity Discussions recommendation: "At the scale you're talking about, just allocate copies and put them in a List so you can undo by restoring prior states."

---

## Comparison Table

| Pattern | Lines of code | Cognitive overhead | Unity complexity | Suitability for solo dev |
|---------|:---:|:---:|:---:|:---:|
| **Full MVC** | ~200 extra | High (dependency injection, service locator) | Medium (MonoBehaviour vs POCO wiring) | Overkill |
| **Lightweight separation (chosen)** | Minimal (~40 for grid model) | Low (one file per concern, direct calls) | Low (just write C#) | **Best** |
| **ScriptableObject architecture** | ~100 extra for event channels + runtime sets | Medium (mental indirection of SOs as mediators) | Medium (SO asset creation, inspector wiring) | Good for data only |
| **Event-driven (full)** | ~150 extra for channel SOs | High (broadcaster → channel → listener chain) | Medium (custom SO types + inspector) | Overkill; use direct calls |
| **Abstract state machine** | ~100 extra for IState + FSM class | Medium (state classes, transitions) | Low | Overkill; use enum+switch |
| **Memento (snapshot) undo** | ~50 lines | Low (copy state, restore state) | Low (pure C#) | **Best** |
| **Command pattern undo** | ~100+ lines per command type | Medium (ICommand, invoker, stacks) | Low | More flexible but more code |
| **PlayerPrefs** | 2 lines | None | None | Settings only |
| **JsonUtility + file** | ~20 lines | None (native Unity API) | None | **Best for game data** |

---

## Summary

**The single simplest pattern that fits Parking Jam:**

> A single `GameManager` MonoBehaviour that owns a plain C# `OccupancyMap` (Dictionary-based grid model), a `List<VehicleView>` of thin view MonoBehaviours, an `UndoStack<List<GridSnapshot>>` for undo (Memento pattern), and an `enum GameState` with a switch statement. Level data is JSON in StreamingAssets. Save data is JsonUtility + file on disk. ScriptableObjects are used *only* for skin/vehicle definitions and economy config.

**File path:** `docs/research/easiest-architecture-pattern.md`
