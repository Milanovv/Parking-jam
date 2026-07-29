# Unity Testing Seams for a 2D Puzzle Game (Parking Jam)

Analysis of optimal test seams, modes, and patterns for a Unity 2D puzzle game with pure C# domain models (OccupancyMap, UndoSystem) and thin MonoBehaviour views orchestrated by GameManager.

---

## 1. Edit Mode vs Play Mode Tests — Official Recommendations

| Aspect | Edit Mode | Play Mode |
|--------|-----------|-----------|
| **Execution context** | Editor scripting context; code runs in `EditorApplication.update` callback loop | Full Unity runtime — physics, scenes, MonoBehaviours, coroutines |
| **Unity runtime access** | No runtime scene/GameObject lifecycle. Can instantiate `GameObject` and add components, but no frame stepping without `EditorApplication` tricks | Full access: `SceneManager.LoadScene`, `Object.Instantiate`, physics, `Time`, coroutines via `[UnityTest]` |
| **Performance** | Instant — no domain reload. Tests run synchronously in milliseconds | Slower — enters Play Mode, loads scenes, processes frames |
| **CI cost** | No build step. Runs headless in Editor process only | Run in Editor Play Mode (no build) or on Player (requires build). Editor Play Mode is ~3–5x slower than Edit Mode |
| **Target** | Editor-only code + any non-MonoBehaviour game code | Game code that depends on Unity runtime (MonoBehaviours, scenes, input) |

**Primary source:** Unity Manual — *"Edit mode tests (also known as Editor tests) only run in the Unity Editor and have access to Editor code and runtime application code"* ([UTF Edit Mode vs Play Mode](https://docs.unity3d.com/6000.4/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html)). The manual further recommends: *"Use the NUnit Test attribute instead of the UnityTest attribute, unless you need to yield special instructions, in Edit Mode, or if you need to skip a frame or wait for a certain amount of time in Play Mode"* (ibid).

**For a 2D puzzle game:** Edit Mode tests are the default choice for all pure logic (occupancy, undo, level validation). Play Mode tests should be reserved for integration scenarios: verifying that a drag input reaches the GameManager and mutates the OccupancyMap, or that additive scene loading/unloading of mini-games works without errors. Unity's own guidance echoes this: *"Edit mode tests (also known as Editor tests) are only run in the Unity Editor and have access to Editor code and runtime application code"* and are suitable for *"testing any of your Editor extensions"* but also implicitly for any non-MonoBehaviour runtime code ([UTF 1.1 manual](https://docs.unity.cn/Packages/com.unity.test-framework@1.1//manual/edit-mode-vs-play-mode-tests.html)).

The Unity Blog's testing guide reinforces: *"You should use the NUnit Test attribute instead of the UnityTest attribute in Edit mode, unless you need to yield special instructions, need to skip a frame, or wait for a certain amount of time in Play mode"* ([How to run automated tests](https://unity.com/how-to/automated-tests-unity-test-framework)).

---

## 2. Pure C# Seams — OccupancyMap and UndoSystem in Edit Mode

Both `OccupancyMap` (Dictionary-based grid) and `UndoSystem` (Memento stack) are **pure C# classes with no MonoBehaviour dependency**. They can — and should — be tested exclusively in Edit Mode using NUnit alone.

**Unity's documentation is explicit:** Edit Mode tests have access to *"runtime application code"* in addition to Editor code. The blog post series by Tomek Paszek (Unity Tools team) specifically advises: *"Move logic away from MonoBehaviour and don't test the imperative scripting part. Insert seams for the important logic and test the plain C# classes"* ([Unit testing part 2](https://unity.com/blog/technology/unit-testing-part-2-unit-testing-monobehaviours)). The "Humble Object Pattern" is the recommended approach — extract all testable logic into non-MonoBehaviour classes and test those in Edit Mode.

A Unity Forum verified solution thread concurs: *"We have a lot of code that is not MonoBehaviour based but still used at runtime in our game. This makes it perfectly suitable to be run as an edit mode test"* ([Unity Discussions](https://discussions.unity.com/t/confusion-between-unit-tests-editmode-tests-and-playmode-tests/897828)).

**For Parking Jam:**
- **OccupancyMap** — Test grid bounds, collision detection, vehicle placement/removal, edge-of-map clamping, overlapping checks. All pure NUnit `[Test]` methods.
- **UndoSystem** — Test push/pop/peek on the memento stack, depth limits, empty-stack behavior. All pure NUnit `[Test]` methods.
- **Level deserialization** — Test `JsonUtility.FromJson<T>()` against known-good JSON strings. No file I/O needed; pass JSON strings directly. Pure NUnit.

**Assembly setup:** Create an `Editor`-only test assembly definition that references the runtime assembly containing these classes. No `UnityEngine` dependency required beyond the test framework itself.

---

## 3. Integration Seam at GameManager — Highest Practical Seam

Unity's recommended approach is to test from the **highest possible seam**. The Unity Manual states: *"Integration testing is a technique that tests different components of a system together to ensure they work correctly. This can include testing how different GameObjects, scripts, or systems interact with one another"* ([Testing and QA best practices](https://unity.com/how-to/testing-and-quality-assurance-tips-unity-projects)).

**Does GameManager qualify as the highest seam?** Yes — in the Parking Jam architecture, GameManager is the orchestrator that receives parsed input, mutates the OccupancyMap, records undo snapshots, and signals views to update. Testing through GameManager exercises the entire **logical integration surface** (input → occupancy → undo → state notification) without involving the rendering or input hardware layers.

However, GameManager is a `MonoBehaviour`. To test it in Edit Mode, you must:
1. Create a `GameObject` with `new GameObject("TestRoot")`
2. Add the GameManager component with `gameObject.AddComponent<GameManager>()`
3. Manually inject the OccupancyMap and UndoSystem dependencies (or use a mocking framework for them)

The Unity Blog confirms this approach works: *"This works just fine in edit mode tests: `var character = gameObject.AddComponent<Character>();`"* ([Unity Discussions](https://discussions.unity.com/t/how-to-set-up-code-for-unit-tests-in-a-practical-way/927037)).

**Play Mode integration path** (recommended for full-stack tests):
- Derive from `InputTestFixture` (Input System package) to get isolated input simulation
- Load a minimal test scene with `SceneManager.LoadScene()`
- Simulate Pointer actions with `Press(pointer)`, `Release(pointer)`, `Set(position, value)`
- Assert that GameManager's internal state (OccupancyMap) changed as expected

The Input System manual states: *"Use InputTestFixture to create an isolated version of the Input System for tests. The fixture sets up a blank, default-initialized version of the Input System for each test, and restores the Input System to its original state after the test completes"* ([Input testing docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Testing.html)). The fixture provides `Press()`, `Release()`, `PressAndRelease()`, `Set()`, and `Trigger()` helper methods.

**For Parking Jam's Pointer-based drag:** You can simulate a drag sequence:
```csharp
var mouse = InputSystem.AddDevice<Mouse>();
Set(mouse.position, new Vector2(startX, startY));
Press(mouse.leftButton);
yield return null;
Set(mouse.position, new Vector2(endX, endY));
yield return null;
Release(mouse.leftButton);
```

The Unity Blog tutorial on automated tests demonstrates this exact pattern for character input testing ([How to run automated tests](https://unity.com/how-to/automated-tests-unity-test-framework)).

---

## 4. Play Mode Test Patterns for Additive Scene Mini-Games

Mini-games loaded as additive scenes require Play Mode tests. The recommended approach per Unity documentation:

**Official pattern:**
1. Add the mini-game scenes to `EditorBuildSettings.scenes` (or use `IPrebuildSetup` / `ITestPlayerBuildModifier` for dynamic registration)
2. In `[SetUp]` or `[UnitySetUp]`, load the base scene with `SceneManager.LoadScene("BaseScene", LoadSceneMode.Single)`
3. Load the mini-game additively: `SceneManager.LoadSceneAsync("MiniGame", LoadSceneMode.Additive)`
4. `yield return` until the scene is fully loaded
5. Assert that the mini-game's root GameObjects, managers, and occupancy state are present
6. In `[TearDown]`, unload: `SceneManager.UnloadSceneAsync("MiniGame")`
7. Use `[OneTimeTearDown]` or `IPostBuildCleanup` to restore `EditorBuildSettings.scenes`

**Key considerations from Unity Discussions:**
- `SceneManager.LoadScene` with `LoadSceneMode.Single` destroys the test bootstrapping scene — this is safe and expected ([Play mode tests SceneHandling](https://discussions.unity.com/t/play-mode-tests-scenehandling/759597))
- For scenes not in build settings (development-only mini-game scenes), use `EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/MiniGame.unity")` — this works during Play Mode in the Editor without requiring the scene in Build Settings ([Stack Overflow](https://stackoverflow.com/questions/65105419/editorscenemanager-using-scenemanager-in-play-mode-test))
- The scene must be `yield return null` after loading before accessing objects, since the load is asynchronous

**Primary sources:**
- Unity Manual — Scene-based tests: *"The EditorSceneManager allows for loading and saving scenes. In combination with the test framework, this allows for the implementation of tests that verify a scene"* ([Scene-based tests](https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/course/scene-based-tests.html))
- Unity Scripting API — [`LoadSceneMode.Additive`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html)
- Unity Scripting API — [`EditorSceneManager.LoadSceneInPlayMode`](https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager.LoadSceneInPlayMode.html)

**For Parking Jam mini-games:** Create one Play Mode test fixture per mini-game. In `[UnitySetUp]`, load the main parking scene, then load the mini-game additively. Verify that the mini-game scene is active, its GameManager is found, and the occupancy grid is initialized. In `[TearDown]`, unload the mini-game scene.

---

## 5. CI Performance: Edit Mode vs Play Mode

| Metric | Edit Mode | Play Mode (Editor) | Play Mode (Player build) |
|--------|-----------|--------------------|--------------------------|
| **Domain reload** | None | ~2–10 seconds (full re-initialization) | N/A (build time dominates) |
| **Test execution** | Sub-millisecond per test | ~50–500ms per test (scenes, frames) | Same as Editor PM + build (~2–10 min) |
| **CI wall time for 100 tests** | ~1–3 seconds | ~30–120 seconds | ~3–15 minutes |
| **Build required** | No | No | Yes |
| **Runner headless support** | `-batchmode -runTests -testPlatform EditMode` | `-batchmode -runTests -testPlatform PlayMode` | `-batchmode -runTests -testPlatform Standalone` |

**Unity's official recommendation:** Prioritize Edit Mode tests for the bulk of coverage. The game-ci documentation notes: *"Edit mode tests run in the editor context without entering Play Mode. Great for pure C# utilities, data pipelines, and editor tools. No access to GameObject lifecycle or scene management"* while Play Mode tests are *"Full runtime environment"* but slower ([game-ci test modes](https://deepwiki.com/game-ci/unity-test-runner/2.2-test-modes)).

The wallstop/unity-tips CI guide recommends: *"Limit Play Mode duration — Focus on short (under 10s) integration tests so CI stays fast"* and advises caching the `Library/` folder to keep pipelines under 15 minutes ([Testing & CI guide](https://wallstop.github.io/unity-tips/best-practices/16-automated-testing-ci/)).

**For Parking Jam CI strategy:**
- **90%+ test count in Edit Mode** — OccupancyMap, UndoSystem, level deserialization, validation logic. All run in <5 seconds total.
- **~10% in Play Mode** — One fixture per mini-game (additive scene loading + basic smoke test), one fixture for input → GameManager integration. Run these as a separate CI job with a shorter timeout.
- **Run Edit Mode and Play Mode as parallel CI jobs** — game-ci supports matrix strategy for parallel execution ([game-ci running tests](https://deepwiki.com/game-ci/unity-actions/2.2-running-tests)).

---

## 6. Mocking in Unity — Official Guidance

**Unity does not ship its own mocking library** in the Test Framework package. The official blog post on unit testing MonoBehaviours explicitly uses **NSubstitute**:

*"I used NSubstitute for the mocking object. We also ship a version of it with the Unity Test Tools"* ([Unit testing part 2](https://unity.com/blog/technology/unit-testing-part-2-unit-testing-monobehaviours)).

The recommended pattern is not to mock MonoBehaviours at all, but to use the **Humble Object Pattern**:
1. Extract all logic from MonoBehaviours into pure C# classes
2. Depend on interfaces for Unity-specific operations
3. Test the pure C# classes with NSubstitute (or Moq) mocks of the interfaces
4. Leave the MonoBehaviour thin shell untested, or cover it in a small number of Play Mode integration tests

**MonoBehaviour mocking constraints:**
- `new MonoBehaviour()` is illegal — they must be created via `AddComponent()` or `Instantiate()`
- Static Unity APIs (`Time`, `Physics`, `Input`) are not mockable unless wrapped in interfaces
- Third-party mocking frameworks (NSubstitute, Moq, FakeItEasy) work with interfaces and virtual methods only — MonoBehaviours are concrete, so they require `AddComponent()` in Play Mode

**Community consensus (Unity Forum, 200+ upvote thread):**
- NSubstitute is preferred for new Unity projects due to cleaner syntax and no telemetry controversy ([Moq vs NSubstitute](https://discussions.unity.com/t/any-proper-mocking-frameworks-for-unity-unit-testing/901320))
- *"Move logic away from MonoBehaviour and don't test the imperative scripting part. Insert seams for the important logic and test the plain C# classes with mocked dependencies via interface injection"* (ibid)
- Moq remains a solid choice with the largest community, but its 4.20 SponsorLink incident reduced trust ([QASkills.sh comparison](https://qaskills.sh/blog/moq-vs-nsubstitute-vs-fakeiteasy-2026))

**For Parking Jam:**
- OccupancyMap and UndoSystem need **no mocking** — pure C# with no Unity dependencies
- GameManager needs its OccupancyMap and UndoSystem injected (constructor or property) so these can be real implementations (not mocks) during testing
- Input handling: wrap the Input System Pointer action behind an `IInputHandler` interface. In Edit Mode tests, pass a manual-test double. In Play Mode tests, use `InputTestFixture`

---

## 7. Prior Art — Unity Sample Projects

### Unity 2D Roguelike Tutorial
The most relevant sample — it has a `GameManager` singleton that coordinates turn logic, level loading, and game state. **It has zero tests.** The project focuses on teaching game structure, not testing ([2D Roguelike GitHub](https://github.com/SpaceMadness/unity-tutorial-2d-roguelike/blob/master/Assets/Completed/Scripts/GameManager.cs)). Its architecture is representative of untutored Unity: a monolithic `GameManager` with hard dependencies on `GameObject.Find`, `GetComponent`, and static `Application.LoadLevel` calls.

### Unity 2D UFO / Ruby's Adventure
These official Learn tutorials also ship without tests. They use the same pattern: `GameObject.Find` for cross-component communication, singleton managers, and direct coupling to Unity APIs.

### Dapper Dino's Testing Series (Community)
Dapper Dino's unit testing tutorials demonstrate:
- Testing pure C# logic classes in Edit Mode with NUnit
- Using NSubstitute for interface mocking
- Play Mode testing with scene loading and `InputTestFixture`

His approach matches the official Unity Blog recommendations exactly. His repos (Dapper-Tools, Item-System-Tutorial) show practical Unity test organization with assembly definitions ([DapperDino GitHub](https://github.com/DapperDino/)).

### Official Unity "Level Up Your Code With Design Patterns" Sample
This Unity 6 sample project includes testing examples and demonstrates:
- Test assembly definitions separated from runtime
- Interface-based dependency injection for testability
- Pure logic extracted from MonoBehaviours ([Unity 6 Resources](https://unity.com/campaign/unity-6-resources))

### Gap analysis
**No official Unity 2D sample tests a GameManager or occupancy/board system.** The 2D Roguelike's `BoardManager` (which procedurally places tiles on a grid) is the closest analogue to an OccupancyMap, but it's untested. This means Parking Jam's testing approach would be ahead of all official Unity 2D tutorials — a reasonable outcome given they are teaching materials, not production codebases.

---

## Recommendation Table

| Module | Type | Optimal Seam | Test Mode | Reasoning |
|--------|------|-------------|-----------|-----------|
| **OccupancyMap** | Pure C# class | Direct instantiation, no mocking needed | **Edit Mode** (`[Test]`) | Zero Unity dependencies. NUnit alone is sufficient. Test grid bounds, collision, placement. |
| **UndoSystem** | Pure C# class (Memento stack) | Direct instantiation, no mocking needed | **Edit Mode** (`[Test]`) | Zero Unity dependencies. Test push/pop/peek/clear, depth limits, empty state. |
| **Level JSON deserialization** | Pure C# `JsonUtility` call | Pass JSON string directly, no file I/O | **Edit Mode** (`[Test]`, `[TestCase]`) | Parameterized tests for valid/invalid/malformed JSON. No StreamingAssets needed. |
| **GameManager logic** (MonoBehaviour orchestrator) | MonoBehaviour with injected OccupancyMap + UndoSystem | Create `GameObject` + `AddComponent<GameManager>` in test, inject deps | **Edit Mode** (`[Test]`) | GameManager's logic is just forwarding calls — test the coordination. Inject real OccupancyMap/UndoSystem (not mocks). |
| **GameManager + Input integration** | Full stack: Input System → GameManager → OccupancyMap | `InputTestFixture` + minimal test scene in Play Mode | **Play Mode** (`[UnityTest]`, `InputTestFixture`) | Verify that simulated Pointer drag reaches OccupancyMap. One fixture, 3–5 tests. |
| **Vehicle/Pedestrian views** (thin Sprites) | MonoBehaviour with visual-only logic | Play Mode: instantiate prefab, verify transform changes | **Play Mode** (`[UnityTest]`) | Minimal — test that view reflects model state after GameManager updates. 1–2 smoke tests. |
| **Mini-game additive scene loading** | Scene loaded via `LoadSceneMode.Additive` | `SceneManager.LoadSceneAsync` + yield + assert scene objects | **Play Mode** (`[UnityTest]`) | One fixture per mini-game. Verify scene loads, GameManager is found, occupancy initialized. Unload in teardown. |
| **Level victory/blocked detection** | Logic in GameManager or helper class | Pure C# — feed OccupancyMap states and check win condition | **Edit Mode** (`[Test]`) | Test every win/loss edge case: all exits blocked, partial occupancy, one vehicle remaining. |
| **Undo + redo workflow** | UndoSystem mutations via GameManager | Create GameManager + inject UndoSystem, perform moves, undo | **Edit Mode** (`[Test]`) | Test the full undo lifecycle without entering Play Mode. Verify OccupancyMap state after undo. |

### Test Distribution Target

| Tier | Count | Mode | Purpose |
|------|-------|------|---------|
| **Unit** (pure C#) | ~50–80 tests | Edit Mode | OccupancyMap, UndoSystem, level validation, win detection |
| **Integration** (GameManager) | ~15–20 tests | Edit Mode | GameManager coordinating deps, undo/redo via GameManager |
| **Integration** (Input → GameManager) | ~5–10 tests | Play Mode | Pointer drag simulation via InputTestFixture |
| **Scene/smoke** (mini-games) | ~5–10 tests | Play Mode | Additive scene load/unload, mini-game initialization |
| **View** (MonoBehaviour thin) | ~3–5 tests | Play Mode | Vehicle/Pedestrian/Barrier visual state sync |

**Total CI runtime estimate:** Edit Mode tests — ~2 seconds. Play Mode tests — ~20–40 seconds (cached Library/). Combined CI job — under 1 minute.

---

## Sources Index

1. Unity Manual — Edit Mode vs Play Mode tests: https://docs.unity3d.com/6000.4/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html
2. UTF Package 1.1 Manual — Edit Mode vs Play Mode tests: https://docs.unity.cn/Packages/com.unity.test-framework@1.1//manual/edit-mode-vs-play-mode-tests.html
3. UTF Package 2.0 Manual — Requiring Play Mode: https://docs.unity.cn/Packages/com.unity.test-framework@2.0/manual/edit-mode-vs-play-mode-tests.html
4. Unity Blog — Unit testing part 1 – Unit tests by the book: https://blog.unity.com/technology/unit-testing-part-1-unit-tests-book
5. Unity Blog — Unit testing part 2 – Unit testing MonoBehaviours (Humble Object Pattern, NSubstitute): https://unity.com/blog/technology/unit-testing-part-2-unit-testing-monobehaviours
6. Unity.com — How to run automated tests with UTF: https://unity.com/how-to/automated-tests-unity-test-framework
7. Unity.com — Testing and QA best practices: https://unity.com/how-to/testing-and-quality-assurance-tips-unity-projects
8. Input System — Input testing (InputTestFixture): https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Testing.html
9. Input System 1.14 — Input testing: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/Testing.html
10. Unity Manual — Scene-based tests: https://docs.unity3d.com/6000.2/Documentation/Manual/test-framework/course/scene-based-tests.html
11. Unity Scripting API — LoadSceneMode.Additive: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html
12. Unity Scripting API — EditorSceneManager.LoadSceneInPlayMode: https://docs.unity3d.com/ScriptReference/SceneManagement.EditorSceneManager.LoadSceneInPlayMode.html
13. game-ci — Running Tests (Test Modes): https://deepwiki.com/game-ci/unity-actions/2.2-running-tests
14. game-ci — Test Modes (editmode, playmode, standalone): https://deepwiki.com/game-ci/unity-test-runner/2.2-test-modes
15. wallstop/unity-tips — Testing & CI: https://wallstop.github.io/unity-tips/best-practices/16-automated-testing-ci/
16. Unity Discussions — Mocking frameworks in Unity: https://discussions.unity.com/t/any-proper-mocking-frameworks-for-unity-unit-testing/901320
17. Unity Discussions — EditMode vs PlayMode confusion: https://discussions.unity.com/t/confusion-between-unit-tests-editmode-tests-and-playmode-tests/897828
18. Unity Discussions — How to set up code for unit tests: https://discussions.unity.com/t/how-to-set-up-code-for-unit-tests-in-a-practical-way/927037
19. Unity Discussions — Play mode tests scene handling: https://discussions.unity.com/t/play-mode-tests-scenehandling/759597
20. Stack Overflow — EditorSceneManager in Play Mode: https://stackoverflow.com/questions/65105419/editorscenemanager-using-scenemanager-in-play-mode-test
21. Unity 2D Roguelike tutorial (GameManager, no tests): https://github.com/SpaceMadness/unity-tutorial-2d-roguelike
22. DapperDino GitHub (testing examples): https://github.com/DapperDino/Dapper-Tools
23. QASkills.sh — Moq vs NSubstitute vs FakeItEasy 2026: https://qaskills.sh/blog/moq-vs-nsubstitute-vs-fakeiteasy-2026
24. Unity 6 Resources Hub — Design Patterns sample: https://unity.com/campaign/unity-6-resources
25. NUnit documentation — Assert class: https://github.com/nunit/docs/wiki/Classic-Model
