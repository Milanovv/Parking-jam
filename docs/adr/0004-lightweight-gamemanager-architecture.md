# ADR-0004: Lightweight GameManager architecture

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing a code architecture pattern for a solo-dev 2D puzzle game with ~25 levels.

**Decision:** Use a single `GameManager` MonoBehaviour that coordinates a plain C# `OccupancyMap` (Dictionary-based grid model), thin MonoBehaviour views, an `enum GameState` with a switch statement, and the Memento pattern for undo (snapshot stack).

**Rationale:**
- Grid logic lives in pure C# (OccupancyMap), decoupled from Unity lifecycle — unit-testable, ~40 lines.
- Thin views (VehicleView) only read model state and update transforms — one concern per file.
- enum + switch for game states avoids abstract FSM framework overhead (community consensus: overkill for small games).
- Memento (full-state snapshots) is the simplest undo for an 8×8 grid with <15 vehicles — no ICommand interface, no per-action undo logic.
- Direct method calls through GameManager (no event buses, no injection) keeps the code navigable top-to-bottom.

**Consequences:**
- Positive: Minimal boilerplate — ~5 script files for the core game loop.
- Positive: OccupancyMap is testable in isolation without Unity.
- Positive: Any developer can read GameManager.cs and understand the entire game loop.
- Negative: GameManager becomes a moderately-sized file as features grow (~300-500 lines).
- Negative: If the project grows significantly, may need to split into separate systems.

**Source:** `docs/research/easiest-architecture-pattern.md`