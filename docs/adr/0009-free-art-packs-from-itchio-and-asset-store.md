# ADR-0009: Free art packs from itch.io and Unity Asset Store

**Status:** Accepted — re-scoped by ADR-0011 (in-world content is now 3D low-poly packs; 2D sourcing limited to UI icons and mini-game art)
**Date:** 2026-07-29
**Context:** Sourcing visual assets (vehicles, pedestrians, barriers, parking lot tiles, UI icons) for a free puzzle game with no art budget.

**Decision:** Source all visual assets from free asset packs on itch.io and the Unity Asset Store. No custom art commissions, no paid asset packs. In-world content comes from free 3D low-poly packs (see ADR-0011); 2D packs cover UI icons and mini-game art.

**Rationale:**
- Thousands of free 2D sprite packs exist for the "top-down parking lot / city" theme across both platforms.
- itch.io's asset section is filterable by "Free" and "Commercial use" license, making compliance checks trivial.
- Unity Asset Store's free section includes complete UI icon sets, vehicle sprites, and tileable ground textures suitable for a parking lot theme.
- Common art style across packs can be unified via a consistent colour palette applied in-editor (Sprite color tint).
- The spec calls for ~25 unique sprites (3 vehicles × 4 colours, 1 pedestrian sheet, barrier, 4 ground tiles, 10 UI icons + mini-game assets). Free packs cover this easily.
- Custom art for a solo dev would take 4–6 weeks per vehicle set; free packs reduce this to ~2 days of curation.

**Consequences:**
- Positive: Zero art cost. Solo developer can curate a consistent set in ~2 days.
- Positive: Wide selection — dozens of relevant packs available immediately.
- Positive: itch.io and Asset Store licenses support commercial use in published games.
- Negative: Art style may be inconsistent across packs — requires colour palette unification and careful selection.
- Negative: If the game gains traction, the art style cannot be changed without replacing every sprite (re-skin).
- Negative: Some free packs have restrictive licenses (no commercial use, no modification) — each pack's license must be checked individually.

**Sources:**
- itch.io asset licensing: itch.io/docs/creators/asset-licensing
- Unity Asset Store terms: unity.com/legal/as-terms
