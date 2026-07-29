# ADR-0001: PC-first release on itch.io

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing the initial release platform for Parking Jam, a free 2D sliding-block puzzle game.

**Decision:** Release on itch.io for Windows, free with no ads. PC-first rather than mobile-first.

**Rationale:**
- itch.io has zero upfront cost (no $100 Steam Direct fee), no annual fees, and the developer chooses the revenue share (0-100%).
- PC-first avoids mobile overhead: macOS build machine requirement (iOS), 24,000+ Android device fragmentation, $99/year Apple Developer fee, and 14-day Google Play testing gate.
- Free release builds a portfolio and audience before any paid launch.
- Unity's Input System abstracts mouse and touch uniformly, so mobile porting later is straightforward.
- Steam can follow after the game is validated on itch.io.

**Consequences:**
- Positive: Zero financial risk, instant uploads, no curation gate, can iterate based on player feedback.
- Positive: Unity Personal license (free) supports all PC platforms.
- Negative: itch.io has lower discoverability than Steam; will need self-promotion.
- Negative: No built-in review system or automated update pipeline (unlike Steam).
- Future: Mobile port will add build toolchain complexity and platform fees.

**Source:** `docs/research/pc-vs-mobile-release.md`, `docs/research/free-pc-game-stores.md`