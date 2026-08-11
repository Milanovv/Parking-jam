# ADR-0011: 2.5D visual style — URP Universal renderer, low-poly 3D in-world content

**Status:** Accepted
**Date:** 2026-08-09

**Context:** The plan called for a 2D sprite look (plan.md #10; ADR-0002's URP 2D Renderer; ADR-0009's 2D pack sourcing). Research on the reference repo `berkeerdem1/Parking_Jam_3DCase` found a complete, free low-poly 3D car pack (6 cars / 2 trucks / bus / police) plus a city pack and concrete textures that fit the parking theme perfectly — but Unity's URP 2D Renderer cannot light 3D meshes without converting them to 2D-compatible shaders (`Mesh2D-Lit-Default`) and using 2D lights (Unity docs: 2D renderer shader compatibility / sorting workflows).

**Decision:** Switch to the **2.5D presentation**: the URP asset's renderer becomes the **Universal (3D) renderer**; all in-world content (vehicles, pedestrians, city backdrop, exit lane, barrier) is built from free low-poly 3D models rendered under a fixed perspective camera (pitch ≈ 40°, no yaw, centred on the lot). Gameplay logic is untouched — grid-space collision, no physics (CONTEXT.md «Collision»). UI and mini-games remain 2D uGUI sprites (ADR-0005). A Skin is a paint material applied level-wide to all vehicles (CONTEXT.md «Skin»).

**Rationale:**
- The 3D car pack is the single biggest asset win available; trailers/portfolio value outweigh the 2D-look simplicity.
- Sprites, sorting layers and uGUI continue to work under the Universal renderer; only 2D lights/shadows are lost (Unity render-pipelines feature comparison).
- The low-poly flat-shaded aesthetic reads consistently with our paint-based skin system.
- The pivot is cheap now: `Assets/_Project/` contains code scaffolding and tests only — no sprites or populated scenes exist yet (checked 2026-08-09).

**Considered Options:**
- Stay 2D sprites — drops the car pack entirely; the incorporation effort becomes ~3 textures + audio re-sourcing.
- Hybrid (2D Renderer + `Mesh2D-Lit-Default` conversion) — flat unlit look, every material converted, 2D light rigging; the weakest visual ceiling.

**Consequences:**
- Supersedes plan.md #10 and the renderer choice of ADR-0002 (URP itself retained).
- Re-scopes ADR-0009: in-world content = free 3D low-poly packs from the Asset Store; 2D packs limited to UI icons and mini-game art.
- All imported materials need URP conversion; FBX import-scale normalisation required (car ≈ 1 tile, truck ≈ 2, bus ≈ 2.5–3).
- Raycast input targets 3D colliders instead of Physics2D (ADR-0006 pointer abstraction unchanged).

**Source:** `docs/research/parking-jam-3dcase-incorporation.md` (§1 decision log D1/D7/D8, §4 R2); `docs/research/pedestrian-people-3d-packs.md`.