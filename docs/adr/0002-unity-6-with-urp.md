# ADR-0002: Unity 6 with Universal Render Pipeline

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing the Unity version and render pipeline for a 2D sliding-block puzzle game.

**Decision:** Use Unity 6 (6000.x) with the Universal Render Pipeline (URP) and its 2D Renderer.

**Rationale:**
- Unity 6 is the current LTS-ready release with long-term support.
- URP's 2D Renderer supports sprite rendering, sorting layers, sprite masks, and 2D lights natively.
- Built-In Render Pipeline is deprecated in Unity 6.5.
- URP is lightweight enough for the eventual mobile port without additional migration work.
- Unity's official 2D sample projects (Happy Harvest, Gem Hunter Match) ship with URP.

**Consequences:**
- Positive: 2D Renderer is purpose-built for sprite-based games.
- Positive: URP is well-documented with extensive community support.
- Positive: No migration needed when adding mobile platforms later.
- Negative: Requires understanding URP asset configuration (2D Renderer Data asset).
- Negative: Slightly more setup than Built-In RP for a pure 2D game.

**Source:** `docs/research/plan-validation.md`