# ADR-0007: Barrier-first exit lane spatial model

**Status:** Superseded by ADR-0013 (placement and gating scope sharpened)
**Date:** 2026-07-29
**Context:** Designing the spatial layout of the exit, barrier, and win condition for Parking Jam.

**Decision:** Use a three-zone spatial model (Inner Grid → Exit Lane → Barrier → Off-screen) with a barrier-first flow: the barrier is locked at level start, the player must unlock it via mini-game before any vehicle can leave the inner grid, and the level is cleared when all vehicles are off-screen.

**Rationale:**
- The barrier-first flow is novel for the sliding-block genre, but validated by escape room design (Problem-First pattern) and games like Slinger Block and Candy Crush ingredient levels.
- Spatial Denial theory (Ziyi Hua, SMU Guildhall 2024) supports it: show the goal, deny access, reward unlocking.
- The exit lane provides diegetic space for the auto-drive Clear animation without cluttering the inner grid.
- All-vehicles win condition avoids confusion about which vehicle to target and creates a clear "clear the lot" objective.

**Consequences:**
- Positive: Clear two-phase structure (unlock barrier → free vehicles) guides the player naturally.
- Positive: Each vehicle exiting provides a mini-reward animation.
- Positive: The barrier is a visible goal from level start, creating curiosity-driven play.
- Negative: Novel mechanic for the genre — first few levels must tutorialize the barrier interaction heavily.
- Negative: The mini-game becomes a hard gate — if too difficult, the player is stuck at the barrier with no alternative path.

**Source:** `docs/research/barrier-first-gameplay-flow.md`