# ADR-0008: Free SFX from Mixkit and CC0 sources

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Sourcing sound effects for a free PC puzzle game with no monetization budget.

**Decision:** Use only free SFX packs from Mixkit and CC0-licensed sources on freesound.org. No music tracks. No paid sound licenses.

**Rationale:**
- Mixkit offers a large library of royalty-free SFX (UI clicks, vehicle sounds, success jingles, buzzer tones) with a simple attribution-free license.
- freesound.org CC0 content covers environment ambience (parking lot, traffic) and pedestrian sounds without legal overhead.
- SFX-only avoids music licensing complexity and composition cost.
- A solo developer can curate 20–30 SFX files in ~2 hours; the alternative (custom recording or paid packs) would cost $50–200+.
- No music is needed for a puzzle game — SFX provide sufficient feedback for interactions, collisions, and level completion.

**Consequences:**
- Positive: Zero audio cost. All sounds can be used commercially with no attribution.
- Positive: Quick turnaround — download, trim, import, assign to events.
- Positive: Mixkit's license covers both PC and future mobile ports.
- Negative: No unique audio identity — sounds may be recognizable from other games using the same packs.
- Negative: CC0 freesound quality varies; requires curation and potentially some post-processing (Audacity normalization).
- Negative: No music — some players may find the silence during gameplay dull.

**Sources:**
- Mixkit license: mixkit.co/license/
- freesound.org CC0: freesound.org/help/about/
