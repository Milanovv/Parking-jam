# ADR-0003: JSON level data in StreamingAssets

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing how to store and load 25 puzzle levels at runtime.

**Decision:** Store levels as JSON files in `StreamingAssets/Levels/`, loaded at runtime via `File.ReadAllText` + `JsonUtility.FromJson<T>()`.

**Rationale:**
- Plain-text authoring: any text editor can create/edit levels, no Unity Editor required.
- Clean version-control diffs (one line changed per data field).
- No build step for data — drop a `.json` file in StreamingAssets and it's included.
- ~1-2 KB per level, parsed in <1 ms — performance is negligible.
- ScriptableObjects lock level editing to the Unity Editor; JSON is independent.
- JSON allows future community level editors or web-based designers.

**Consequences:**
- Positive: Designer without Unity can author levels.
- Positive: Easy to add, remove, or reorder levels at any time.
- Positive: Level data can be validated by CI scripts or external tools.
- Positive: `File.ReadAllText` works directly on PC; `UnityWebRequest` needed for future Android/WebGL port (well-documented, one-line change).
- Negative: No visual grid preview during authoring (hand-typed coordinates).
- Negative: Requires a stable JSON schema that must remain backward-compatible with existing levels.

**Source:** `docs/research/level-data-storage.md`, `docs/research/level-authoring-easiest.md`