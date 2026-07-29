# ADR-0006: Input System package for unified input

**Status:** Accepted
**Date:** 2026-07-29
**Context:** Choosing an input handling strategy that works for mouse (PC release) and supports future touch (mobile port).

**Decision:** Use the Unity Input System package with a single "Drag" action bound to both `Mouse` and `Touch` devices via the `Pointer` abstraction.

**Rationale:**
- Input System's `Pointer` device class abstracts mouse and touch uniformly — the same action works on both without branching code.
- Built-in `TouchSimulation.Enable()` allows testing touch behavior in the Unity Editor using mouse input.
- `InputSystemUIInputModule` handles unified pointer input for uGUI interactions.
- Tap and drag interactions are first-class citizens in the Input System.
- The legacy Input Manager (`Input.GetMouseButton`) would require a rewrite for mobile touch support.

**Consequences:**
- Positive: Mobile input works out of the box when porting — no input code changes.
- Positive: Touch simulation in Editor speeds up mobile UI testing.
- Positive: Actions can be remapped and extended without touching C# code (Input Action Assets).
- Negative: Requires installing the Input System package (built-in, no extra cost).
- Negative: Small learning curve for Input Action Assets vs. legacy Input calls.
- Negative: Must disable the legacy Input Manager in Project Settings to avoid conflicts.

**Source:** `docs/research/plan-validation.md`