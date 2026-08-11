# Research — Incorporating `berkeerdem1/Parking_Jam_3DCase` into Parking Jam

**Date:** 2026-08-08 (v2 sharpened 2026-08-09 — grilling session, decisions D1–D12 settled, ADR-0011/0012 filed)
**Method:** Primary-source review: local Unity repo `D:\testing-diplomna` (context, ADRs, specs, plan) + read-only clone of https://github.com/berkeerdem1/Parking_Jam_3DCase (commit `6e715b6`, branch `main`, depth-1) at `C:\Users\kris\AppData\Local\Temp\opencode\parking-jam-3dcase`, cross-checked with the GitHub REST API.

---

## 1. TL;DR

The repo is a **student case-project clone of the mobile game "Parking Jam 3D"** — an entirely different game from ours: free-driving 3D cars on fix-laid Bezier roads driven by simple click-to-drive scripts, physics collisions, 3 pre-authored levels. It has **no license** (all-rights-reserved by default). Its value to us is **only the vendored third-party free assets**: a complete low-poly car pack (6 cars / 2 trucks / bus / police car × 6 colours), a low-poly city/houses pack, concrete ground textures, 6 free SFX, a free confetti particle prefab, and the PathCreator plugin. The gameplay code (8 scripts, ~11 kbytes) is not reusable — our architecture (ADR-0004), input (ADR-0006), render pipeline (ADR-0002) and gameplay model all differ.

**Sharpened verdict (decision log, 2026-08-09):** the repo is **reference material only**. We adopt: the 3D low-poly aesthetic (2.5D presentation, **ADR-0011**), always re-import free packs from their **official Asset Store listings** (**ADR-0012**), build our own confetti and hand-rolled Bézier exit-curve follower, and drop all repo audio (ADR-0008). Confetti and PathCreator lines from v1 — take/drop classified below — were found to be contradictions and are resolved.

### Decision log (grilling, 2026-08-09)

| # | Decision | Choice | Where recorded |
|---|----------|--------|----------------|
| D1 | Renderer / look | 2.5D pivot: URP **Universal (3D) renderer**, fixed perspective camera (pitch ≈ 40°, no yaw, centred on lot). In-world = 3D low-poly; UI + mini-games stay 2D uGUI (ADR-0005) | ADR-0011 (supersedes ADR-0002's 2D Renderer, plan.md #10, re-scopes ADR-0009) |
| D2 | Pack source | Re-download the same free packs from the Unity Asset Store; repo clone = fallback only | ADR-0012, §5.1 |
| D3 | Exit-lane driver | Hand-rolled cubic Bézier follower in `_Project/Scripts/`; PathCreator **dropped** | §5.3, T8 |
| D4 | Confetti | Build our own particle confetti; repo's prefab is repo-authored (all-rights-reserved) | §5.3, T9 |
| D5 | Audio | Drop all repo audio — provenance unknown; Mixkit/CC0 only (ADR-0008) | §5.2, T11 |
| D6 | Pedestrian art | Free low-poly people pack **"City People FREE Samples"** (Denys Almaral) — verified low-difficulty source (0.5–1 day); fallbacks: PolyOne "Free Pack – Lowpoly People", PolyPeople City [Free] | `docs/research/pedestrian-people-3d-packs.md`, T5 |
| D7 | Camera | Fixed perspective ≈ 40° pitch (D1) | ADR-0011 |
| D8 | Vehicle scale | Import-scale normalisation: car ≈ 1 tile, truck ≈ 2, bus ≈ 2.5–3; visuals may overhang; collision stays grid-space (CONTEXT.md «Collision») | T2, T12 |
| D9 | Skins | Skin = paint material; the equipped skin re-colours every vehicle in the level → CONTEXT.md «Skin» clarified, Game Save schema unchanged | CONTEXT.md, T6 |
| D10 | Exit-curve data | Optional `exitCurve` (4 control points) per level in JSON; default straight-then-arc when absent | level-schema.md, T8 |
| D11 | City scope | Full Palmov pack (houses stay — street backdrop); prune demo scene + ferris wheel | T3 |
| D12 | Barrier model | Palmov fence + primitive crossbar, palette-matched | T10 |

---

## 2. Source: the repo itself (primary facts)

### 2.1 Metadata

- Repo: `berkeerdem1/Parking_Jam_3DCase`, description "Case Project", created 2023-11-28, last push 2023-12-16, default branch `main` (per GitHub API `repo` response, fields: `created_at`, `pushed_at`, `default_branch`, `license`).
- Root listing (GitHub API `contents`): `.gitattributes`, `.gitignore`, `.utmp/`, `.vscode/`, `.vsconfig`, `Assets/`, `Packages/`, `ProjectSettings/`, `README.md` — **no `LICENSE` file anywhere** (also confirmed via `git ls-tree -r HEAD` over all 758 tracked files: zero matches for LICENSE/license/NOTICE/COPYING).
- Unity version declared: `ProjectSettings/ProjectVersion.txt:1` → `m_EditorVersion: 2023.1.11f1`; README.md:8 recommends "open the project with Unity version 2023.1.11f1" and describes the game (README.md:2) as "My clone game containing the first 3 levels of Parking Jam 3D".
- Packages: `Packages/manifest.json:1-44` — **no URP, no Input System** (no `com.unity.render-pipelines.*`, no `com.unity.inputsystem`). Included: `com.unity.ai.navigation` 1.1.4, `com.unity.ugui` 1.0.0, TMP 3.0.6, Timeline, VisualScripting, Recorder 4.0.2, Collab, dev-feature set.
- Render pipeline: `ProjectSettings/GraphicsSettings.asset:41` → `m_CustomRenderPipeline: {fileID: 0}` ⇒ **Built-in Render Pipeline**. Materials use the built-in **Standard** shader (e.g. `Assets/Materials/Color1.mat`, `m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000}`).
- Input: legacy Input Manager in use — `Input.GetMouseButtonDown(0)` in `Assets/Scripts/clickDetector.cs:11` and `Assets/Scripts/Follower.cs:90`; `ProjectSettings/InputManager.asset` lists classic axes (Horizontal/Vertical/Mouse X etc.).
- Scene/tags: `ProjectSettings/TagManager.asset:7-18` defines tags `back, Road, finish, front, front2, back2, front3, back3, car, front4, back4, wall, TurnPoint`. Scenes in Build: `Level1.unity`, `Level2.unity`, `Level3.unity` (`ProjectSettings/EditorBuildSettings.asset:8-16`).
- Working tree: 758 tracked files; `Assets/` holds 315 non-meta files ≈ **31.4 MB** (of which TGA textures alone ≈ 21 MB, 6 files). GitHub API reports repo size ≈ 1.18 GB (includes history/other blobs).

### 2.2 Assets inventory (vendored third-party packs — the reusable part)

All under `Assets/`. Sizes computed from local clone.

| Pack (path in repo) | Files | MB | Contents |
|---|---|---|---|
| `BrokenVector/LowPolyCarPack` | 217 | 2.49 | 12 FBX vehicle models: `Car 1`–`Car 6`, `Truck 1`, `Truck 2`, `Bus`, `Policecar`, `Environment.fbx`; 55 colour prefabs (6 cars × 6 colours + 2 trucks × 6 + Bus × 6 + Policecar); 18 materials (9 PBR + 9 Unlit variants — paints ×6, lamps, rubber, windows); palettes PNGs; `Readme.pdf` |
| `BrokenVector/LowPolyShaders` | 7 | 0.72 | Built-in-RP custom shaders `LowPolyPBRShader.shader` and `LowPolyUnlitShader.shader`; `Readme.pdf` |
| `Palmov Island/Low Poly Houses Free Pack` | 242 | 3.19 | 9 house models (cottage, brewery, pizzeria, post office, city hall, temple…), ~12 environment props (bench, lamppost, fence, fountain, trash can…), plants/trees (~17), roads (5 pavement pieces), grounds (asphalt pieces, land, lake, tennis court), ferris wheel (lunapark), each with prefab + combined "house with environment" prefabs; demo scene |
| `Concrete textures pack` | 27 | 21.8 | 3 patterns (03/07/19): 4 MB `diffuse.tga` + 4 MB `normal.tga` each + prebuilt materials + previews |
| `PathCreator` (plugin) | 102 | 0.99 | Sebastian Lague's PathCreator: `Core/Runtime/*` (17 C# files, e.g. `BezierPath.cs`, `VertexPath.cs`, `PathCreator.cs`, `Utility/*`), `Core/Editor/*` (PathEditor, helpers), `Examples/` (5 demo scenes, `PathFollower.cs`, `RoadMeshCreator.cs`, prefabs, Road material + texture), `Documentation.pdf`, settings asset |
| `Prefabs/` (repo's own) | 15+ | — | `Car 1..4.prefab`, `Car1..4.prefab` (8 gameplay cars), `wall*.prefab`, `finish.prefab`, `Way.prefab` (cube roads), `Park.prefab`, `Confetti Effect.prefab` + `Confetti Effect 2.prefab` (particle, ~126 KB each) |
| `Resources/` audio | 6 | 1.5 | `car_drive.wav` (0.86 MB), `car_hit.wav`, `car_horn.wav`, `jingle_win.wav`, `win.mp3`, `cars80.mp3` |

### 2.3 Gameplay code analysis (8 home-made scripts, ~600 lines)

The repo is a **simulation, not a grid puzzle**: cars are Rigidbody **Drive-along-Bezier-path** objects. The whole loop:

1. `clickDetector.cs` (`isFront`/`isBack` booleans) + `Follower.cs` (`Update()` at lines 31–87): each car gets tag `front`/`back` children; clicking (legacy `Input.GetMouseButtonDown`) raycasts `Camera.main.ScreenPointToRay`, sets a bool, the car then advances `transform.position = pathCreator.path.GetPointAtDistance(distance)` (`Follower.cs:38-39`) with manual 90° rotation at `TurnPoints`.
2. `OnCollisionEnter` (`Follower.cs:112-141`): hitting a `car`/`wall` plays `car_horn`, freezes the Rigidbody of the other car, and **resets both to `startPosition`**; hitting `finish` (tag from `finish.prefab`) spawns confetti and destroys the car.
3. `GameManager.cs:24-44`: `Update()` counts `GameObject.FindGameObjectsByType tag "car"`; when 0 → instantiate confetti, wait 3 s, `SceneManager.LoadScene(sceneName)` — one singleton, no DontDestroyOnLoad.
4. `Sound_Manager.cs:9-14`: static `Resources.Load<AudioClip>` for car_hit/car_horn/car_drive, static playback.
5. `Folllower4.cs`, `Follower2.cs`, `Follower3.cs` are near-identical copies of `Follower.cs` (differ by 7–11 lines each, measured by diff) — per-car script copies, no prefab reuse.
6. `DestroyObject.cs:10` deletes itself after 2 s (confetti cleanup).

Levels: `Level1.unity` object list (from the scene YAML): `Game Manager`, `Sound Manager`, `Main Camera`, `Directional Light`, `Ground`, `City`, `Ways`, `Path(car1..3)/ BackPath(carN)` (PathCreator objects), per-car `TurnPoints`, `ConfettiNextLevelPos` — placed in a built environment (`City` = Palmov houses). **No Canvas / uGUI objects in the level scenes.** Car prefab (`Assets/Prefabs/Car 1.prefab`): minimal `Rigidbody` + tiny `BoxCollider` pairs, one `back` child tagged, and one script reference binding via guid → `Follower.cs.meta` (`guid: 3f9c8265903a4e74282bd329db641609`) — i.e. the prefab triggers path following.

**Gameplay takeaway:** the repo's behaviour is the *antithesis* of our ADR-0004 OccupancyMap/grid model (no grid, no discrete movement, physics collision instead of grid-space collision, cookie inputs). Nothing of it ports except concepts (confetti-on-clear, all-vehicles-cleared → next level) that we already spec'd ourselves (CONTEXT.md «Clear», ADR-0007).

### 2.4 The license checkpoint (exact vertices)

- **No license in the repo.** `git ls-tree -r HEAD` finds no LICENSE/COPYING/NOTICE file; GitHub API `license` field is `null`. `README.md` contains no license statement — only "All assets are free" (README.md:9), referring to the Asset Store/Package Manager assets.
- **Consequence for our project:** under copyright law, the repo's *own* content (scripts, scenes, level layout, prefab wiring of its own scripts) is **all-rights-reserved** by default. We must **not copy the game code** — settled as the legal stance behind ADR-0012.
- **The third-party packs inside are *not* authored by the repo's owner** and carry their own terms: each pack ships a `Readme.pdf` (e.g. `BrokenVector/LowPolyCarPack/Readme.pdf`, `Palmov Island/Low Poly Houses Free Pack/guideline.pdf`) which on the Unity Asset Store declares the standard Asset Store EULA (free Asset Store assets, usable in any number of commercial projects, no redistribution as sold). These PDFs are binary; their exact license text was not machine-extractable in this environment; under D2 we re-download from the Asset Store so the EULA is unambiguous at source.
- `LowPolyShaders/Readme.pdf` likewise contains the BrokenVector license (shaders dropped anyway, D1 — URP has equivalents).
- **PathCreator** is the well-known open-source `SebLague/Path-Creator` package vendored in the repo (MIT-licensed upstream), but that repo does not include its license either — moot under D3 (dropped).
- **Audio provenance unknown** (`car_drive`, `car_hit`, `car_horn`, `win`, `jingle_win`, `cars80`): no attribution files in the repo — settled: **not used** (D5).

---

## 3. Verbatim what we take — asset-by-asset decision table (sharpened)

| Source item (repo path) | Usage in Parking Jam | Target (per ADR-0010/0011) | Action | Notes |
|---|---|---|---|---|
| `BrokenVector/LowPolyCarPack/Models/*.fbx` (12) | Vehicle meshes — 6 cars (1-tile), Trucks (2-tile), Bus (~2.5–3-tile), Police | `Assets/ThirdParty/LowPolyCarPack/` + built with our `.prefab` | **Take, re-download from Asset Store** (D2) | The single biggest win: a complete consistent car park as 3D vehicles. Import-scale normalisation per D8 |
| `…/Materials/PBR\|Unlit/*.mat` | Repaint per skin; cars need URP shaders | Recreate under `_Project/Materials/Vehicles/` | **Port** | 18 materials, all built-in RP → URP Lit/unlit (R2), paint set = skin shelf (D9) |
| `…/Prefabs/Car_*.prefab` (55 colour prefabs) | None (they bundle `car`/`way`-dependent components) | — | **Drop** | Our `Vehicle.prefab` composed from models + our components (T6) |
| `BrokenVector/LowPolyShaders/` | Unlit custom shaders — obsolete in URP | — | **Drop** | Use URP shaders (R2); readme EULA irrelevant |
| `Palmov Island/…` models+prefabs | Parking-lot surroundings: exit-lane set dressing, decorative static obstacles, houses as street backdrop | `Assets/ThirdParty/PalmovHouses/` | **Adopt** (D11: full pack, prune demo scene + ferris wheel) | Grounds (`asphalt…`), fences/lamps/trees + houses for exit-lane & backdrop |
| `Concrete textures pack` | Ground / wall tiles for the parking lot, `normal`-mapped | `Assets/ThirdParty/ConcreteTextures/` | **Take** | TGA → PNG on import; PC compression BC7 not ASTC (R4 amend — PC-first, ADR-0001) |
| `Resources` audio: 6 files | None | — | **Drop** (D5) | Provenance unknown (R7); Mixkit/CC0 replaces per ADR-0008 (T11) |
| `Confetti Effect.prefab` (+ `2`) | None | — | **Drop** (D4) | **Contradiction fixed:** §2.2 classifies these as *repo's own* prefabs → all-rights-reserved. We build our own particle confetti (T9) |
| `finish.prefab`, `wall (1).prefab`, `Park.prefab`, `Ways.prefab` | None | — | **Drop** | Repo-authored prefab wiring (R6); Barrier built from Palmov props + primitives instead (D12) |
| `PathCreator` Core + Examples | None | — | **Drop** (D3) | **Contradiction fixed:** 102-file plugin for one scripted curve; replaced by hand-rolled cubic Bézier follower in `_Project/Scripts/` (T8). MIT-upstream licence no longer relevant |
| `Assets/Scripts/*.cs` (8 files) | Nothing | — | **Drop** | All-rights-reserved + incompatible with ADR-0004/0006 model |
| `Scenes/Level1-3.unity` | Nothing | — | **Drop** | Built-in RP, no UI, physics junk |
| Boss Texture packs, example scenes, `.idea`, Recorder/analysis | — | — | Drop | |

**Added (not in repo):** pedestrians come from the free Asset Store pack **"City People FREE Samples"** (per `docs/research/pedestrian-people-3d-packs.md`, D6).

---

## 4. Compatibility risks and resolutions (sharpened)

| # | Risk | Detail (source) | Resolution |
|---|---|---|---|
| R1 | **Unity 2023.1 vs Unity 6** | Repo opened with `2023.1.11f1` (`ProjectVersion.txt`). Scene/prefab/material YAML forward-upgradeable; Unity 6 auto-upgrades serialized assets on first open (warnings only). | Import into our Unity 6 project via Asset Store; let the upgrade pass rewrite YAML; re-save every touched asset (T12). |
| R2 | **Built-in RP → URP** | 61 FBX models are mesh-only (fine). But Standard-shader materials + `LowPolyShaders` (built-in) don't render correctly in URP. | **Settled (D1/ADR-0011):** URP **Universal (3D) renderer**; materials → `Universal Render Pipeline/Lit` (or Unlit for the flat look). The 2D Renderer was confirmed unsuitable: 3D meshes get no lighting under it without `Mesh2D-Lit-Default` conversion (Unity docs — 2D renderer shader compatibility). |
| R3 | **Input mismatch** | Repo uses legacy `Input.GetMouseButtonDown` + raycasts; we use the Input System `Pointer` action (ADR-0006). | Not a blocker — we rewrite the view layer anyway; keep `InputHandler` as designed (`unity-implementation.md` §8), raycast 3D colliders (drag target = car mesh). No borrowed input code. |
| R4 | **Huge textures** | 6 TGA (3 diffuse + 3 normal) at ~4 MB each (≈ 21 MB of the 31 MB repo) | Import settings: max 2048, **BC7** (PC-first, ADR-0001 — ASTC was repo's mobile bias; wrong for us), keep 2 patterns (03 + 07). |
| R5 | **Model scale/orientation** | FBX imports at 0.01 scale; vehicles must align to the tile grid | **Settled (D8):** normalise at import — car ≈ 1 tile, truck ≈ 2, bus ≈ 2.5–3, minor visual overhang allowed; grid-aligned root via `Grid.CellToWorld()`; collision stays grid-space. Tuning ticket T12. |
| R6 | **Prefab script binding** | Car prefabs bind repo scripts via GUID (e.g. `Follower.cs.meta` → guid `3f9c8265903a4e74282bd329db641609`) | Don't import those prefabs; build `Vehicle.prefab` ourselves from the models + our components (T6). |
| R7 | **License / provenance gap** | Repo audio/particles carry no attribution; repo ships no license | **Settled (D2/D4/D5/ADR-0012):** packs re-downloaded from Asset Store; repo sounds dropped; confetti built in-house. |
| R8 | **Pedestrian art gap** | Neither repo pack contains people; CONTEXT.md Pedestrian needs obstacles that read as human | **Settled (D6):** City People FREE Samples (0.5–1 day verified); style single-material palette matches paint-skin workflow. |

---

## 5. The incorporation plan — concrete (sharpened)

### 5.1 Legal plan for the repo (ADR-0012)

1. **The repo is reference material only.** Repo-authored content — `Assets/Scripts/`, `Assets/Scenes/`, repo-authored prefabs (incl. confetti), level layout — is all-rights-reserved (no LICENSE). Never copy it.
2. **Third-party free packs** (car pack, houses, concrete, pedestrians): re-download the same free packs directly from the **Unity Asset Store** (README.md:9 states the assets come from there and are free) — the standard Asset Store EULA permits commercial use and modification, no attribution, no redistribution of raw assets (cite: unity.com/legal/as-terms, §2.2.1; confirmed for the people pack in `pedestrian-people-3d-packs.md`). The repo clone is a fallback only for delisted packs.
3. All imported third-party materials go under `Assets/ThirdParty/`; all first-party rebuilt assets go under `_Project/` (ADR-0010)
4. Keep `ThirdPartyNotices.md` in the repo mapping asset → source → license → date (ADR-0009); audit each pack's EULA at import (T1).

### 5.2 Where each file lands (ADR-0010/0011 mapping)

```
Assets/
├── ThirdParty/                          ← extended "Plugins go in Plugins/" to packs (ADR-0010)
│   ├── LowPolyCarPack/                  ← T2 (source: Asset Store, D2)
│   ├── PalmovHouses/                    ← T3
│   ├── ConcreteTextures/                ← T4
│   ├── CityPeople/                      ← T5 (pedestrians)
│   └── NOTICE.md (per-pack license notes)
└── _Project/
    ├── Scripts/Core|Grid|Vehicles|…     ← all rewritten per ADR-0004 (+ AudioManager per ADR-0008 + Bézier follower, T8)
    ├── Prefabs/Vehicles/Vehicle.prefab  ← T6 (+ Prefabs/Pedestrians/, T5; Prefabs/FX/Confetti, T9; Barrier, T10)
    ├── Scenes/                          ← unchanged
    ├── Audio/Sfx/…                      ← T11 (Mixkit/CC0 only, D5)
    ├── Materials/Vehicles/              ← T2/T6 (URP paint set, D9)
    ├── Models/Vehicles/ + Models/Pedestrians/ ← T2/T5 (referenced from ThirdParty, re-materialed)
    ├── Settings/                        ← URP asset swap to Universal renderer (T7)
    └── StreamingAssets/Levels/…
```

No `Assets/Plugins/` content — PathCreator dropped (D3).

### 5.3 The two behavioural features worth stealing (as designs, not code)

1. **Clear fanfare** — confetti at the exit point plus a short hold before the next level. The repo does exactly this (`GameManager.cs:24-44`: all `car`-tagged objects gone → confetti → 3 s wait → `LoadScene`); our CONTEXT.md `Clear` + ADR-0007 spec the same flow. **Implemented with our own Clear event + own-built confetti prefab** (D4, T9) — the repo's prefab is repo-authored and cannot be copied.
2. **Auto-drive exit animation** — the repo's `Follower.cs:38-39` (`pathCreator.path.GetPointAtDistance(distance)`) is exactly the mechanism ADR-0007 needs for the "auto-drive animation through the exit lane". **Implemented with a hand-rolled cubic Bézier follower** (D3, T8) — no third-party plugin; curve control points ride in the level JSON as optional `exitCurve` (D10), with a default straight-then-arc shape.

**All together:** the only *code* we write is ours; the only *content* re-used is free packs re-downloaded from the Asset Store (car pack, city pack, concrete textures, people pack). Everything "borrowed" from the repo is a design reference, not a file.

---

## 6. Order of tickets (small, dependency-ordered — sharpened)

- T1 — **License gate (unblocking)**. Re-download all four packs from the Asset Store (D2); read each EULA (Asset Store standard — commercial + modification OK); write `docs/legal/third-party-notices.md`; confirm repo = reference-only (ADR-0012). Unblocks T2–T5.
- T2 — Import LowPolyCarPack under `Assets/ThirdParty/`; convert materials to URP (paint set under `_Project/Materials/Vehicles/`); FBX import-scale normalisation per D8 (car≈1, truck≈2, bus≈2.5–3 tiles).
- T3 — Import Palmov houses pack; prune demo scene + ferris wheel (D11); place fences/trees/lamps/houses for the parking-lot backdrop and exit-lane street.
- T4 — Import concrete textures (03/07); build `_Project/Materials/ParkingLotGround` (URP Lit/Unlit, tile 1 m, normal map); compression BC7/max 2048 (R4).
- T5 — Import City People FREE Samples; build `Pedestrian.prefab` (patrol slide per tick, no rig, single-material repaint) (D6).
- T6 — Rebuild `Vehicle.prefab` under `_Project/Prefabs/` from models: footprint (1/2/3 tiles via model scale), colliders, wiring to `GameManager`/`SkinController`; 6-paint material set; skin = paint, applied level-wide (D9).
- T7 — 2.5D setup: swap URP asset renderer to **Universal (3D)** via `UrpSetup.cs`; fixed perspective camera, pitch ≈ 40°, no yaw, centred on the lot (D1/D7, ADR-0011). *Must precede visual QA of T2–T6.*
- T8 — Hand-rolled cubic Bézier follower in `_Project/Scripts/` (D3); add optional `exitCurve` to level schema (D10, level-schema.md); wire into the Clear animation per ADR-0007 with default curve when omitted.
- T9 — Own confetti particle prefab (`_Project/Prefabs/FX/`) wired to the Clear event (D4).
- T10 — Barrier model: Palmov fence + primitives crossbar, palette-matched (D12), positioned at the far end of the exit lane (ADR-0007).
- T11 — Audio: Mixkit/CC0 SFX set — horn, hit, win jingle, confetti pop (ADR-0008). Repo sounds dropped (D5).
- T12 — QA sweep: scale/rotation on grid, colliders, BC7 build sizes, build-settings cleanup (no repo scenes).

---

## 7. Sources checked (primary)

- Local: `D:\testing-diplomna\CONTEXT.md`; `docs/adr/0001…0012/*.md` (0011, 0012 filed from this research); `docs/plan.md`; `docs/specs/unity-implementation.md`, `level-schema.md`, `level-progression.md`, `mini-games.md`; `docs/research/unity-requirements.md`; `docs/research/pedestrian-people-3d-packs.md` (D6 source).
- Remote clone: all file paths above under `C:\Users\kris\AppData\Local\Temp\opencode\parking-jam-3dcase\` (README.md, all `Assets/Scripts/*.cs` incl. line cites in §2.3, `ProjectVersion.txt`, `Packages/manifest.json`, `ProjectSettings/{GraphicsSettings,InputManager,TagManager,EditorBuildSettings}.asset`, `Assets/Prefabs/Car 1.prefab`, `Assets/Prefabs/finish.prefab`, `Assets/Prefabs/Ways.prefab`), `git ls-tree -r HEAD` (758 files), GitHub API `repos/…` and `repos/…/contents` (license `null`).
- Unity docs (R2/D1 fact check): URP 2D renderer shader compatibility & sorting workflows (Manual/urp/2d-renderer-urp-shader-compatibility.html; 2d-renderer-urp-sorting-workflows.html; render-pipelines-feature-comparison.html) — cited in full in `pedestrian-people-3d-packs.md` and ADR-0011.
- PDF license texts (`LowPolyCarPack/Readme.pdf`, `LowPolyShaders/Readme.pdf`, `Palmov…/guideline.pdf`) were **not machine-readable** in this environment — moot under D2 (Asset Store EULA at source).