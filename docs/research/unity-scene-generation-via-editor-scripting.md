# Unity Scene Generation via Editor Scripting (Can an Agent Author a Playable Scene Alone?)

Research note for ticket #29 ("playable level scene + launch seam"). Question: can an agent, CLI-only with no human in the Unity Editor, generate and commit a playable `.unity` level scene for Parking Jam, and what is the canonical lowest-risk way to do it on this exact project?

**Short answer: yes — committed editor scripts executed via `-executeMethod` in batchmode are the canonical path, and this repo already generates scenes that way.** The nine mini-game scenes in `Assets/_Project/Scenes/MiniGames` were produced by exactly this pattern (an `[InitializeOnLoadMethod]` editor script that calls `EditorSceneManager.NewScene` + `SaveScene`, committed in 3df8f69). One caveat specific to this project: the current runtime has **no launch seam** — no code turns a level JSON into scene objects, so "playable scene" means *scene asset + one small runtime bootstrap*, both scriptable and committable by an agent.

---

## 1. In-repo prior art (primary source: the repo itself)

### 1.1 How the nine mini-game scenes were made

- All nine scenes under `Assets/_Project/Scenes/MiniGames/` were added in a single commit, **3df8f69** ("Implement T25: … nine generated empty scenes ship as additive build scenes…") — `git log --oneline --all -- Assets/_Project/Scenes/MiniGames` returns only that commit.
- The generator is `Assets/_Project/Scripts/Editor/MiniGameScenesAssets.cs` (committed in the same 3df8f69). Its anatomy, which is the template for #29:
  - `[InitializeOnLoadMethod] EnsureOnLoad() → Ensure()` — regenerates/repairs scenes on every editor open, with an idempotency guard (skips the scene if the file already contains the controller type name, `MiniGameScenesAssets.cs:49-53`).
  - Per scene: `var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);` (`MiniGameScenesAssets.cs:56`), adds root GameObjects (`MiniGame` controller root with a serialized `Spec`, `MiniGameCanvas` with `Canvas`+`CanvasScaler`+`GraphicRaycaster`, `EventSystem` with `EventSystem`+`InputSystemUIInputModule` — lines 58-89), then `EditorSceneManager.SaveScene(scene, path);` (line 91), restores the previously active scene (lines 92-93), and `AssetDatabase.SaveAssets()` (line 96).
- `Assets/_Project/Scripts/Editor/BuildSweep.cs` keeps the build scene list machine-maintained: `EnsureBuildScenes()` (also `[InitializeOnLoadMethod]`) rewrites `EditorBuildSettings.scenes` to [Main (enabled) + 9 mini-games (disabled)] whenever it drifts (`BuildSweep.cs:65-96`). `Assets/_Project/Tests/EditMode/BuildSettingsTests.cs` hard-asserts this exact list: 10 scenes, `scenes[0]` = `Assets/Scenes/Main.unity` enabled, the 9 mini-games disabled (`BuildSettingsTests.cs:8-33`).
- The scene list in version control matches: `ProjectSettings/EditorBuildSettings.asset` currently holds Main.enabled + the 9 disabled scenes, each with a GUID (`EditorBuildSettings.asset:7-37`).
- The generated `.unity` files are plain text YAML (~357-359 lines each) and their `.meta` files (7 bytes each) were committed alongside — e.g. `MiniGame_Pipes_Easy.unity.meta` carries `guid: 0281cf1d596f69e4e85034e18749fcf7`, which is the same GUID used in `EditorBuildSettings.asset:12-13`.
- Scene-surgery variant already proven in the repo: the pack-asset scripts do **not** create new scenes; they open the existing one, instantiate a prefab, and re-save. `BarrierAssets.EnsureMainScene()` → `EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single)` → `PrefabUtility.InstantiatePrefab(barrier, scene)` → position it → `EditorSceneManager.SaveScene(scene)` → reopen the original scene (`Assets/_Project/Scripts/Editor/BarrierAssets.cs:154-178`), incl. a MainCamera tag guard (`:180-190`). Identical pattern in `ConcretePackAssets.cs:254-274`, `PalmovPackAssets.cs:342-353`, `PeoplePackAssets.cs:239-259`, `CarPackAssets.cs`.
- EditMode tests verify generated scene assets by opening them with `EditorSceneManager.OpenScene` (`Assets/_Project/Tests/EditMode/MiniGameScenesAssetsTests.cs:29,54,78,100`) — i.e. the repo's QA convention is that generated scenes are asserted by editing tests, not human eyeballing.

### 1.2 The actual current state of "no playable scene"

The premise behind #29 needs precision. The project **does** have `Assets/Scenes/Main.unity` — committed since c28c591 ("Ticket 1: project scaffold"), iterated by T4/T7/T8/T9/T11 (latest touch: 3fe0e5c, T11 barrier gate; verified in HEAD via `git cat-file -e HEAD:Assets/Scenes/Main.unity`, working tree clean). It is the *active* build scene (`EditorBuildSettings.asset:8-10`, GUID 5c8ee3386b0eb7243beafeda1771655e matches `Assets/Scenes/Main.unity.meta:2`). **But it is not playable**: its roots are MainCamera (+`GameCameraRig`, 40° pitch), directional `Sun`, `ParkingLotGround` prefab instance, `Backdrop` prefab instance, one `Pedestrian` prefab instance, and one `Barrier` prefab instance at (12,6,0) (`Assets/Scenes/Main.unity:555-563`) — with **no GameManager, GridController, InputHandler, Canvas/EventSystem, or Vehicle objects**. PlayMode tests never load it; they build the rig from scratch at runtime (see `InputMovementTests.Setup()`, `Assets/_Project/Tests/PlayMode/InputMovementTests.cs:15-34`) — consistent with 3df8f69's note that "Unity 6 test runs ignore the scene list".

### 1.3 What a level scene must wire at runtime vs. what the code builds itself

From the actual sources (not the older spec sketch in `docs/specs/unity-implementation.md` §8):

| Piece | Who provides it | Evidence (repo file) |
|---|---|---|
| `GameManager` instance (singleton, `Instance` set in `Awake`) | **scene must contain it** — no `[RuntimeInitializeOnLoadMethod]` bootstrap exists anywhere | `Assets/_Project/Scripts/Core/GameManager.cs:48-63`; grep for `RuntimeInitializeOnLoadMethod` across `Assets/_Project/Scripts` = 0 hits |
| `LevelData` | **not auto-loaded at runtime.** `GameManager.Start()` does `InitializeLevel(_levelData)` on the serialized field (`GameManager.cs:96-100`). `LevelLoader.TryLoad(int)` exists but only tests call it | `Assets/_Project/Scripts/Data/LevelLoader.cs:13-16`; `Assets/_Project/Tests/PlayMode/LevelCampaignTests.cs:60` |
| Grid dims | **not wired from LevelData.** `GameManager` never calls `GridController.SetGridSize`; the grid reads serialized `_gridWidth/_gridHeight` (defaults 5×5). Tests call `SetGridSize(level.gridWidth, level.gridHeight)` manually | `Assets/_Project/Scripts/Grid/GridController.cs:6-12,20-24`; `LevelCampaignTests.cs:60` |
| `Grid` + `Tilemap` on grid | scene must contain a `Grid` component with `GridController` on the same GameObject | `GridController.cs:14-18` (`GetComponent<Grid>()` / `GetComponentInChildren<Tilemap>()` in Awake) |
| `InputHandler` | scene must contain it **and its `[SerializeField] private GridController _gridController` must be wired** (null → all input ignored) | `Assets/_Project/Scripts/Core/InputHandler.cs:6,44` |
| Camera tagged `MainCamera` | scene must contain it (needed by `InputHandler._camera = Camera.main`, `MiniGameManager` pauses etc.) | `InputHandler.cs:16,92` |
| Vehicles | scene must contain `Vehicle`+`VehicleMovement` components. `GameManager.Start()` → `PlaceVehiclesOnMap()` discovers them via `FindObjectsByType<Vehicle>` and registers them ONCE (`GameManager.cs:226-243`) — but `vehicle.Initialize(id, orientation, gridPosition, length)` must have run first; `GridPosition` is `{ get; private set; }` and is **not serialized**, so it cannot be authored in the scene file | `Assets/_Project/Scripts/Vehicles/Vehicle.cs:18-28`; `GameManager.cs:226-243` |
| Pedestrians/obstacles/barrier occupancy | **code builds it from LevelData** (`SpawnObstaclesAndPedestrians`, `SpawnBarrier` create pure-logic occupants); only the *visual* `BarrierGate` (prefab in Main.unity) self-registers via `OnEnable/Start → GameManager.Instance?.RegisterBarrierGate(this)` | `GameManager.cs:140-181`; `Assets/_Project/Scripts/Barriers/BarrierGate.cs:18-31` |
| Canvas + EventSystem | scene must contain them for any uGUI (mini-game scenes are the template) | `MiniGame_Pipes_Easy.unity:122-301` |
| Economy/MiniGameManager/Confetti | self-spawn singletons, nothing to author | `MiniGameManager.EnsureInstance()` (`Assets/_Project/Scripts/MiniGames/MiniGameManager.cs:20-25`), `EconomyManager.EnsureInstance()` (`Assets/_Project/Scripts/Economy/EconomyManager.cs:16`) |

**Consequence for #29:** "playable level scene" is not achievable by scene-asset authoring alone — no code path currently turns level JSON into initialized scene objects. The ticket's "launch seam" must be a small runtime bootstrap (see §4.2), and that's fine: a MonoBeaviour bootstrap with serialized references is exactly as authorable/committable as the scene itself.

### 1.4 Other repo facts that constrain the approach

- Editor version pinned: `ProjectSettings/ProjectVersion.txt` → `6000.5.7f1`.
- `ProjectSettings/ProjectSettings.asset:689` → `activeInputHandler: 2` (**Both** old+new input); `Packages/manifest.json` carries `com.unity.inputsystem 1.20.0`, `com.unity.ugui 2.5.0`, `com.unity.render-pipelines.universal 17.5.0`.
- `Assets/_Project/Scripts/Editor/Editor.asmdef` declares assembly `ParkingJamEditor` (`includePlatforms: ["Editor"]`) referencing `Unity.RenderPipelines.Universal.Runtime`, `Unity.InputSystem`, `UnityEngine.UI`, `ParkingJam` — so generator code touching Input System / uGUI / URP types compiles in the editor assembly.
- Git history shows the editor repeatedly rewrites state on open: 7e6990c notes "the nine mini-game scenes return to EditorBuildSettings (disabled) as the editor re-wrote the scene list" — evidence that `[InitializeOnLoadMethod]` drift-repair scripts are the repo's established mechanism.
- `.gitignore` does **not** ignore scene files (only `InitTestScene*.unity*` from the test runner); scenes are first-class committed assets.
- Meta hygiene is a real concern here: the working tree currently has an **untracked** `Assets/_Project/Tests/EditMode/Levels.meta` (observed via `git status --short`) — a live example of the "forgot the .meta" failure mode (§5).

---

## 2. Unity primary sources (official docs)

### 2.1 Editor scene APIs (Unity 6, 6000.0 scripting reference)

- `EditorSceneManager.NewScene(NewSceneSetup setup, NewSceneMode mode = NewSceneMode.Single)` → returns the new `Scene`. "Create a new Scene." The `setup` parameter selects whether the default GameObject set is added ([NewSceneSetup.EmptyScene](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.NewSceneSetup.html) gives a bare scene — exactly what the nine mini-game scenes use).
  URL: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.NewScene.html
- `EditorSceneManager.SaveScene(Scene scene, string dstScenePath = "", bool saveAsCopy = false)` → `bool`. "All paths are relative to the project folder, such as 'Assets/MyScenes/MyScene.unity'. Folders specified in the path must already exist before calling the function… The function returns false if the save failed." With a `dstScenePath` given, **no save dialog appears** — which is what makes batchmode saving safe (batchmode suppresses dialogs anyway, §2.2).
  URL: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveScene.html
- Companion APIs used by the repo's generators: `EditorSceneManager.OpenScene(path, OpenSceneMode.Single)` (scene surgery), `EditorSceneManager.GetActiveScene()`, `EditorSceneManager.CloseScene(scene, true)` (`MiniGameScenesAssets.cs:92-94`), `PrefabUtility.InstantiatePrefab(prefab, scene)` (`BarrierAssets.cs:168`).

### 2.2 Command-line batchmode (`-batchmode -quit -executeMethod`)

Official manual, "Unity Editor command line arguments reference" (6000.0):
- `-executeMethod <ClassName.MethodName>` — "Execute the static method as soon as Unity opens the project… You can use this for tasks such as continuous integration, performing Unit Tests, making builds or preparing data." The class must live in an **Editor** folder (an Editor-only asmdef like `ParkingJamEditor` is the equivalent). "To return an error from the command line process, either throw an exception which causes Unity to exit with return code 1, or call `EditorApplication.Exit` with a non-zero return code."
- `-batchmode` — "Unity runs command line arguments without the need for human interaction. It also suppresses dialog windows that require human interaction (such as the **Save Scene** window)." Exceptions during script execution → immediate exit with return code 1. "You can't open a project in batch mode while the Editor has the same project open."
- `-quit` — quits after the other commands finish; errors still land in the log. `-quitTimeout` (default 300 s) bounds waiting on async tasks.
- `-logFile <path>` — redirects the full Editor log to a file (batch mode sends only minimal output to the console — always pass `-logFile`).
- `-accept-apiupdate` — API Updater does **not** run in batch mode without it; omitting it can produce compiler errors (i.e. fail the run).
- `-nographics` — no graphics device init; output logs off unless `-logFile` is set. Fine for scene authoring (no GI baking needed here).
  URL: https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html
- Practical invocation pattern for this repo:
  `"<Unity 6000.5.7f1>\Editor\Unity.exe" -batchmode -quit -projectPath "D:\testing-diplomna" -executeMethod MyLevelSceneGenerator.Run -logFile "Logs\gen-scene.log" -accept-apiupdate`

### 2.3 Scene files are text YAML — but not hand-writable

- Manual section "Scenes → Text-based scene files → UnityYAML": scenes are saved as text files using a custom YAML subset, which is why they merge/diff in version control (see the repo's own 357-359-line committed `.unity` files for the concrete structure: header `%YAML 1.1`, `--- !u!<classId> &<fileId>` documents, `SceneRoots` footer).
- **Crucially, the same page states: "You cannot externally produce or edit UnityYAML files."** Hand-writing a `.unity` scene as raw text (option (c)) is therefore explicitly unsupported — GUID/fileID bookkeeping and the setting blocks (OcclusionCullingSettings/RenderSettings/LightmapSettings/NavMeshSettings) make it the fragile option, not just the unsupported one.
  URL: https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html

### 2.4 Canvas / EventSystem / Input System requirements

- uGUI (Unity UI 2.x) manual, "Event System": the Event System "is a way of sending events to objects in the application based on input"; the active **Input Module must be on the same GameObject as the Event System**; **Raycasters** (GraphicRaycaster for UI) are what input modules use to determine what the pointer is over. I.e. interactive uGUI in a standalone build needs an EventSystem + Input Module + GraphicRaycaster set — precisely the two GameObjects the mini-game scene generator emits.
  URLs: https://docs.unity3d.com/6000.0/Documentation/Manual/EventSystem.html (Manual) = https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/EventSystem.html
- Input System package "Installation guide": the corresponding Player setting is "Active Input Handling"; when the new backends are enabled, `ENABLE_INPUT_SYSTEM=1` is added to builds; both systems can be on at once ("Both"). This is a **project-wide setting, not a scene or editor-session condition** — with `activeInputHandler: 2` already committed (`ProjectSettings.asset:689`), Input System APIs (incl. the scene's `InputSystemUIInputModule`) work identically in batchmode-generated scenes, EditMode/PlayMode tests, and standalone builds. Also: to run the API you need a package reference (present, `com.unity.inputsystem 1.20.0`).
  URL: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Installation.html

### 2.5 EditMode tests as an alternative generation path

- Manual "Edit mode and Play mode tests" (6000.4): "Edit mode tests only run in the Unity Editor and have access to **Editor code and runtime application code**" — so `EditorSceneManager.NewScene`/`SaveScene` are legally callable from an EditMode test; the repo already does exactly this in `MiniGameScenesAssetsTests.cs` (opens/scans every generated scene via `EditorSceneManager.OpenScene`).
  URL: https://docs.unity3d.com/6000.4/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html

---

## 3. Answer: can the agent do it alone, and how?

**Yes.** Every step is scriptable and has committed precedent in this repo: generating a scene (3df8f69 `MiniGameScenesAssets`), surgically editing the existing scene (T7-T11 pack scripts), asserting generated scenes in EditMode tests (`MiniGameScenesAssetsTests`, `BuildSettingsTests`), and — via the §2.2 docs — driving all of it from a CLI process with a deterministic exit code. No human editor session is required anywhere. The only thing an agent cannot author into a `.unity` file is state the runtime never serializes (`Vehicle.GridPosition`), which is why a minimal launch-seam bootstrap is part of the deliverable.

### 3.1 Ranked options

1. **(a) Committed editor generator + `-executeMethod` in batchmode, then commit the generated scene** — recommended, and the repo's own convention. Pros: idempotent `Ensure()`-style script (repairable on open via `[InitializeOnLoadMethod]`, like the existing five asset scripts), deterministic CI-style run, scene + `.meta` committed as normal assets, EditMode tests assert the asset. This is literally how the nine mini-game scenes shipped.
2. **(b) One-off local editor script** run once during implementation, committing only the scene. Works, but loses reproducibility; the repo standard is committed `Ensure()` scripts, and a one-off run can't repair drift later. Only choose if the generator is explicitly throwaway; still run it from the batched CLI identical to (a).
3. **(c) Hand-written `.unity` YAML** — rejected: Unity documents that UnityYAML files cannot be produced externally (§2.3); you would also have to fake correct serialized fileIDs, GUID references, and settings blocks, and meta GUID/drift bugs would be undetectable until runtime.
4. **(d) EditMode test as generator** — viable fallback (§2.5), but `-executeMethod` is the simpler, documented entry point; use tests for *asserting* the generated asset instead.

### 3.2 Recommended concrete path for #29 (exact API sequence)

Follow 3df8f69's generator + T11's surgery patterns; do **not** create a second scene:

1. **Add an editor class** (in `Assets/_Project/Scripts/Editor/`, assembly `ParkingJamEditor`), e.g. `LevelSceneAssets` with `[InitializeOnLoadMethod] EnsureOnLoad()` + `public static void Run()` (the `-executeMethod` entry), mirroring `MiniGameScenesAssets.cs:24-47` and `BuildSweep.cs:92-96`.
2. **Idempotency guard**: skip if `Assets/Scenes/Main.unity` already contains a marker GUID/type (pattern: `MiniGameScenesAssets.cs:53`), so repeat runs are no-ops.
3. **Scene editing block** (surgery on the existing Main.unity, exactly `BarrierAssets.cs:154-178`):
   - `var original = EditorSceneManager.GetActiveScene().path;`
   - `var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);`
   - Add `Grid` + `GridController` (set `_gridWidth/_gridHeight` to the level's values — this also fixes the `GameCameraRig.Start()` framing-order issue, `GameCameraRig.cs:12-16`), `GameManager`, `InputHandler` (wire `_gridController` via `SerializedObject`/`FindProperty("_gridController").objectReferenceValue` + `ApplyModifiedProperties()` before saving — it is `[SerializeField] private`, `InputHandler.cs:6`), a scene `Canvas`+`CanvasScaler`+`GraphicRaycaster` + `EventSystem`+`InputSystemUIInputModule` (copy the exact GameObjects from `MiniGame_Pipes_Easy.unity:122-301`), and `Vehicle`+`VehicleMovement` instances placed via `grid.CellToWorld(tile)` on transforms (`BarrierAssets.cs:168-171` for the prefab-instantiation form: `PrefabUtility.InstantiatePrefab(vehiclePrefab, scene)`).
   - Add the **launch-exam object**: one `LevelBootstrapper`-style MonoBehaviour (new runtime script, committed with the scene) whose serialized fields the generator wire: `GridController`, a `levelId`, and the vehicle prefab(s). Its `Start()` does what no existing code does: `LevelLoader.TryLoad(levelId, …)` → `grid.SetGridSize(...)` → for each serialized/prefabricated vehicle `Initialize(...)` + register (or instantiate from prefab) → done. (Runtime component `Start()` ordering hazard is avoided because grid size is authored on the component, §1.3.)
   - `EditorSceneManager.SaveScene(scene);` then reopen `original` (`BarrierAssets.cs:173-177`), `AssetDatabase.SaveAssets();`.
4. **Don't touch build settings** — `BuildSweep.EnsureBuildScenes()` + `BuildSettingsTests` already pin Main.unity as the single enabled scene; a new scene is not needed and would break the assertion at `BuildSettingsTests.cs:12`.
5. **Commit** `Main.unity` + `Main.unity.meta` + the new scripts (+ their `.meta` files) + the bootstrap runtime script.
6. **Add EditMode assertions** a la `MiniGameScenesAssetsTests.cs:29` — open the scene, assert GameManager/GridController/InputHandler/EventSystem roots exist and the scene parses; keep `BuildSweep` happy (its content allowlist already covers `Assets/Scenes/Main.unity`, `BuildSweep.cs:29`).

Run: `Unity.exe -batchmode -quit -projectPath <repo> -executeMethod LevelSceneAssets.Run -logFile <path> -accept-apiupdate` — exit code 0 = success, 1 = throw (check the log), exactly the contract in §2.2.

### 3.3 Failure modes to plan for

- **Missing folders**: `SaveScene` fails on non-existent directories (docs, §2.1) — create them with `AssetDatabase.CreateFolder` recursion (existing `EnsureFolder` helper, `MiniGameScenesAssets.cs:99-106`).
- **Missing/uncommitted `.meta`**: a regenerated meta = a *new GUID* → references (EditorBuildSettings GUIDs, `ProjectSettings/EditorBuildSettings.asset:8-37`) silently point at a stale GUID. Live example in this repo today: untracked `Assets/_Project/Tests/EditMode/Levels.meta`. Always commit the `.meta` next to the scene. (Path-based drift is self-healing here only because `BuildSweep.EnsureBuildScenes()` rewrites the list on open — don't rely on it, it's a side effect.)
- **Deleting/recreating the scene**: regenerates fileIDs/GUIDs; assert stability via EditMode tests that the repo already runs (139/139 was the 3df8f69 baseline; BuildSettingsTests is the gate).
- **Batchmode quirks**: single-instance lock (no batchmode while the editor has the project open, §2.2); missing `-accept-apiupdate` → compile errors in batch; `-nographics` kills console logs unless `-logFile` is set; `-quit` + async work can hang (use `-quitTimeout`); minimal stdout — parse the log file, not the console.
- **Input/UI traps**: `InputHandler._gridController` and `GameManager` needs are serialized-private — the generator *must* wire them via `SerializedObject` before `SaveScene`; an EventSystem without an Input Module on the same GameObject is inert (docs, §2.4); `activeInputHandler` is already `2` (Both) so no Player-settings change is needed for Input System to work in the built player.
- **Author-time can't-happens**: `Vehicle.GridPosition` is not serialized (`Vehicle.cs:18`) — scenes alone can never be playable; the bootstrap (§3.2 step 3) is a mandatory part of "playable", and it must run `Initialize()` *before* `GameManager.Start()`'s `PlaceVehiclesOnMap()` (`GameManager.cs:96-100`) strips it — i.e. bootstrap in `Awake`/`OnEnable`, or accept the two-phase registration.
- **Two scene sources of truth**: mini-game systems spawn rigs at runtime (`MiniGameManager.cs:48-73`) and the scenes are decorative build-list entries — don't add runtime scene-loading of level scenes; keep Main.unity as the single active scene (BuildSettingsTests gates this).

---

## 4. Sources

Repo (all local; hashes from `git log`):
- Mini-game scene generator: `Assets/_Project/Scripts/Editor/MiniGameScenesAssets.cs`, commit 3df8f69.
- Build scene list maintenance: `Assets/_Project/Scripts/Editor/BuildSweep.cs`; assertion: `Assets/_Project/Tests/EditMode/BuildSettingsTests.cs`.
- Scene surgery pattern: `Assets/_Project/Scripts/Editor/BarrierAssets.cs:154-190`; same pattern in `ConcretePackAssets.cs`, `PalmovPackAssets.cs`, `PeoplePackAssets.cs`, `CarPackAssets.cs`.
- Scene asset evidence: `Assets/Scenes/Main.unity` (HEAD, commit 3fe0e5c last touch); `Assets/_Project/Scenes/MiniGames/*.unity` (commit 3df8f69); `ProjectSettings/EditorBuildSettings.asset`; `ProjectSettings/ProjectVersion.txt` (6000.5.7f1); `ProjectSettings/ProjectSettings.asset:689` (activeInputHandler: 2); `Packages/manifest.json`; `Assets/_Project/Scripts/Editor/Editor.asmdef`, `Assets/_Project/Scripts/Game.asmdef`.
- Runtime seams: `Assets/_Project/Scripts/Core/GameManager.cs`, `Assets/_Project/Scripts/Core/InputHandler.cs`, `Assets/_Project/Scripts/Grid/GridController.cs`, `Assets/_Project/Scripts/Vehicles/Vehicle.cs`, `Assets/_Project/Scripts/Data/LevelLoader.cs`, `Assets/_Project/Scripts/Data/LevelData.cs`, `Assets/_Project/Scripts/Barriers/BarrierGate.cs`, `Assets/_Project/Scripts/MiniGames/MiniGameManager.cs`, `Assets/_Project/Tests/PlayMode/PlayModeTestBase.cs`, `InputMovementTests.cs:15-34`.
- Spec §9: `docs/specs/unity-implementation.md:465-481` ("Scenes in Build: Main.unity + all mini-game scenes (not marked active)").
- Research-note convention: `docs/research/level-data-storage.md`, `docs/research/unity-testing-seams.md` (kebab-case topic notes, `# Title` + numbered sections + per-claim URLs).

Unity docs:
- EditorSceneManager.NewScene: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.NewScene.html
- EditorSceneManager.SaveScene: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.EditorSceneManager.SaveScene.html
- Command line arguments: https://docs.unity3d.com/6000.0/Documentation/Manual/EditorCommandLineArguments.html
- UnityYAML (text scene files; "You cannot externally produce or edit UnityYAML files"): https://docs.unity3d.com/6000.0/Documentation/Manual/UnityYAML.html
- Event System (uGUI 2.0): https://docs.unity3d.com/6000.0/Documentation/Manual/EventSystem.html / https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/EventSystem.html
- Input System installation / Active Input Handling: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Installation.html
- Edit mode vs Play mode tests: https://docs.unity3d.com/6000.4/Documentation/Manual/test-framework/edit-mode-vs-play-mode-tests.html