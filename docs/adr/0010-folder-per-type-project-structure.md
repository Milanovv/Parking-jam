# ADR-0010: Folder-per-type project structure under `_Project/`

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Organizing Unity project assets for a solo developer building a single-scene puzzle game with ~25 levels.

**Decision:** Place all game-specific assets under a top-level `Assets/_Project/` folder, organized by asset type (Scripts/, Prefabs/, Scenes/, Sprites/, Audio/, Fonts/, Settings/). Plugins go in `Assets/Plugins/`, level data in `Assets/StreamingAssets/`.

**Rationale:**
- Folder-per-type is Unity's default convention (scripts in Scripts/, textures in Textures/) — it matches what the Unity Editor and Asset Store packages expect.
- The `_Project/` prefix groups first-party assets together, visually separating them from third-party packages in the Project window.
- A solo developer benefits from predictable locations: "where do I put a new script?" → `_Project/Scripts/`. No ambiguity.
- Feature-first folders (e.g., `Features/Grid/`, `Features/Vehicles/`) are unnecessary for this scope — there are ~15 scripts total across 5 subdirectories. Folder-per-type is navigable at a glance.
- Plugin separation (Plugins/) follows Unity convention and avoids accidental modification of third-party code.

**Consequences:**
- Positive: Predictable, zero-discussion organization — any Unity developer knows where to look.
- Positive: `_Project/` prefix isolates first-party content from imported packages.
- Positive: Easy to exclude from version control if needed (single `.gitignore` entry).
- Negative: As the project grows, Scripts/ and Prefabs/ subdirectories may become crowded (mitigation: subfolders Core/, UI/, Grid/ within `_Project/Scripts/` as spec'd).
- Negative: Feature-first advocates would argue co-location is better — but not justified at this scale.

**Sources:**
- `docs/research/project-structure.md` — compares folder-per-type vs feature-first
