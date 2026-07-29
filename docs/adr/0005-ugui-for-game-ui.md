# ADR-0005: uGUI for game UI

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing a UI system for menus, HUD, level select, shop, and overlays.

**Decision:** Use Unity Canvas (uGUI) for all game UI.

**Rationale:**
- uGUI is the "Recommended" runtime UI system in Unity 6 (per Unity Manual comparison), not deprecated.
- Grid Layout Group handles the 5×5 level select grid in 10 minutes (no flexbox fiddling).
- Anchor presets make responsive HUD positioning trivial (2 clicks per element).
- Built-in Scroll View, Button hover states (Color Tint), and Animator integration cover every screen needed.
- UI Toolkit (Unity's alternative) lacks Animator/Timeline integration, has scroll view quirks, and adds ~4 hours of setup vs ~1.5 hours for uGUI.
- Community consensus (Unity Forums, Reddit, 2026): solo devs overwhelmingly use uGUI for game UI.

**Consequences:**
- Positive: Fast prototyping — ~1.5 hours to first working UI pass.
- Positive: Vast tutorial and community support for any UI problem.
- Positive: Full Animator/Timeline support for animated transitions and popups.
- Negative: uGUI is in maintenance mode (bug fixes only); UI Toolkit is Unity's strategic direction.
- Hybrid approach (UI Toolkit for menus only) is possible but unnecessary for this scope.

**Source:** `docs/research/ui-framework-easiest.md`