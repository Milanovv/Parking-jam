# ADR-0013: Barrier placement and global gating

**Status:** Accepted
**Date:** 2026-08-09
**Context:** ADR-0007 established the three-zone spatial model (Inner Grid → Exit Lane → Barrier → Off-screen) but left the barrier's placement and gating scope ambiguous — the glossary ("gate at the far end of the Exit Lane"), the level schema ("outermost exit tile"), and the progression spec (Level 8: "the other exit tile is open") contradicted each other.

**Decision:** The barrier occupies the outermost exit tile — the tile at the inner-grid boundary, one cell before a vehicle would leave the grid. It gates **globally**: while locked, the exit edge is closed across *all* exit tiles, so no vehicle can leave the inner grid through any exit; a vehicle dragged toward any exit stops on the last inner-grid tile, bumper-to-gate in front of the barrier. A level contains **at most one barrier**. Unlocking (mini-game completed, or coin skip) removes the barrier and opens the edge for the rest of the level.

**Rationale:**
- Global gating preserves ADR-0007's Problem-First hard-gate design: the mini-game is never optional, so the player is genuinely stuck at the gate until it is solved or skipped.
- Boundary placement keeps the exit lane empty until a vehicle actually exits (no lane-queue contradiction), makes the tap target reachable on the grid (Level 7 tutorial: "tap to unlock"), and matches the "vehicles line up behind the barrier" behavior.
- One barrier per level matches every level in the progression spec; multi-gate levels remain possible later as a validation-only change.

**Consequences:**
- Level 8's design changes: its second exit tile is closed until unlock, so the decision is post-unlock routing, not "long path vs unlock".
- Implementation: the barrier occupies its tile while locked, and the vehicle sweep must check a level-wide locked flag at the exit edge in addition to tile occupancy.
- Supersedes ADR-0007's spatial ambiguity while keeping its three-zone model and barrier-first flow.