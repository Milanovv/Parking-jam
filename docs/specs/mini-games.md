# Parking Jam — Mini-Game Design Spec

## Overview

Mini-games are short interruptive challenges the player must complete to unlock the Barrier. One mini-game per level, assigned in the level JSON. The pool contains 3 types, each with scalable difficulty.

## Constraints (from domain model and spec)

- Pauses main game (timer/move counter frozen)
- Free retry — no cost to restart, no penalty animation
- Must complete in ~15–30 seconds on first attempt
- On completion → Barrier removed → vehicles can exit
- Implemented as additive scenes (see `MiniGameManager` in spec)
- Each has a `MiniGameController` that calls `MiniGameManager.CompleteMiniGame()` on win
- Success reward: barrier lift animation (boom gate rising), no score bonus
- Scene loaded via `SceneManager.LoadSceneAsync("MiniGame_...", LoadSceneMode.Additive)`, unloaded on complete
- First mini-game encounter (level 1 or 2) must be Easy Pipe Puzzle with tutorial overlay

---

## Type 1: Pipe Puzzle

### Concept

A grid of pipe segments. Some are pre-placed and fixed. The player taps rotatable segments to complete a continuous path from a source to a sink.

### Grid sizes

| Difficulty | Grid | Rotatable tiles | Time limit |
|------------|------|----------------|------------|
| Easy | 3x3 | 2–3 | None |
| Medium | 4x3 | 4–5 | 30 s |
| Hard | 4x4 | 6–8 | 20 s |

### Piece types

- **Straight** (horizontal or vertical) — has 2 orientations (0°, 90°)
- **Corner** (L-shaped) — has 4 orientations (0°, 90°, 180°, 270°)
- **T-junction** — has 4 orientations
- **Fixed piece** — non-interactive, placed at level-data time
- **Source** — one tile, coloured green, path starts here
- **Sink** — one tile, coloured red, path must reach here

### Win condition

A continuous path of connected open ends exists from Source to Sink, verified by BFS from source after every rotation.

### Lose condition

Time expires (if timer is set).

### Generation algorithm

1. Start with a valid path from source to sink on the target grid
2. Scramble by rotating a random subset of rotatable tiles 0–3 times
3. Run BFS from source to verify a solution still exists
4. If unsolvable, re-scramble (rare for ≤4×4 grids)

This guarantees at least one valid solution exists for every puzzle (validated by Tembrica, Simon Tatham's Net, Puzzle-Pipes.com consensus).

### Hint system

| Difficulty | Hints |
|------------|-------|
| Easy | 0 (no hint needed) |
| Medium | 1 free hint — highlights one tile that needs rotation |
| Hard | 2 free hints — highlights one tile each |

Hint temporarily glows a tile that is currently in the wrong rotation. Does not reveal the correct rotation — only flags it as needing attention.

### Visual

- Top-down 2D sprites on a dark background
- Tapping a rotatable piece rotates it 90° clockwise with a snap animation (~0.1 s)
- **Flow visualization**: after connectivity check succeeds, play a water-flow animation along the BFS path from source to sink (~0.3–0.5s total). Connected tiles get a green glow or "Fill" sprite overlay
- **Live connectivity**: tiles on the current solution path are highlighted with a dim glow. When a rotation breaks the path, the glow on disconnected tiles fades immediately (re-run BFS after every rotation)

### Connectivity check

```csharp
bool IsConnected(PipeTile[,] grid, Vector2Int source, Vector2Int sink) {
    Queue<Vector2Int> queue = new();
    HashSet<Vector2Int> visited = new();
    queue.Enqueue(source);
    visited.Add(source);

    while (queue.Count > 0) {
        Vector2Int current = queue.Dequeue();
        if (current == sink) return true;

        foreach (Vector2Int dir in FourDirections) {
            Vector2Int neighbor = current + dir;
            if (!InBounds(neighbor) || visited.Contains(neighbor)) continue;
            if (PipeHasOpenEnd(grid[current.x, current.y], dir) &&
                PipeHasOpenEnd(grid[neighbor.x, neighbor.y], -dir)) {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
            }
        }
    }
    return false;
}
```

Must be called after every rotation. For ≤16 tiles, this is sub-millisecond.

### Unity implementation

- **Grid data**: 2D array of `PipeTile` structs, each storing `TileType` (enum: Straight, Corner, T, Source, Sink) and `CurrentRotation` (0–3). Sprite determined by type + rotation
- **Sprite pivot**: pivot at center of tile. Rotate via `transform.eulerAngles` with `Vector3.forward` axis
- **Tap detection**: `IPointerClickHandler` or `Physics2D.Raycast` with `BoxCollider2D` on each tile. Enlarge collider 10–15% beyond sprite bounds for fat-finger tolerance
- **Rotation animation**: `DoTween` or coroutine: `transform.DORotate(new Vector3(0, 0, currentZ + 90), 0.1f)`
- **Flow animation**: coroutine sequentially enables "Fill" child sprites along BFS path from source to sink

### Edge cases

- **Unsolveable generation**: prevented by solve→scramble→verify algorithm
- **Connectivity glow stale**: re-run BFS after every rotation to prevent "ghost connection" from previous path
- **Puzzle starts solved**: if scramble produces a solved state, re-scramble until at least one tile differs from solution
- **Multiple solutions**: connectivity check accepts ANY valid path — not a specific pre-computed one

---

## Type 2: Pattern Lock

### Concept

A sequence of coloured buttons lights up. Player must repeat the sequence by tapping the buttons in the same order (Simon Says).

### Button layouts

| Difficulty | Button count | Layout | Sequence length | Flash speed | Audio tones |
|------------|-------------|--------|----------------|-------------|-------------|
| Easy | 4 | 2x2 grid | 4 | 1.0 s per flash | 4 distinct tones |
| Medium | 5 | Cross | 6 | 0.8 s per flash | 5 distinct tones |
| Hard | 6 | 2x3 grid | 8 | 0.6 s per flash | 6 distinct tones |

Audio tones are non-negotiable — the mechanic is defined by colour–tone pairing (original Simon patent 1978). Each button must have a unique frequency.

### Win condition

Player correctly repeats the full sequence.

### Lose condition

Player taps a wrong button → failure notification → retry.

### Accessibility

Each button has a **shape icon** overlaid in addition to colour, so colour-blind players can distinguish them:

| Button | Colour | Shape | Tone (Hz) |
|--------|--------|-------|-----------|
| 1 | Red | Circle | 660 (C5) |
| 2 | Blue | Triangle | 440 (A4) |
| 3 | Green | Square | 990 (G5) |
| 4 | Yellow | Diamond | 770 (F5) |
| 5 | Purple | Star | 550 (C#5) |
| 6 | Orange | Hexagon | 880 (A5) |

Tone frequencies derived from original Simon (red/green/blue/yellow) and extended for 5–6 button variants.

### Visual

- Circles on a dark background, each with a distinct shape icon at center
- Active button: bright colour + scale pop (1.0 → 1.15 → 1.0 over 0.1s) + tone plays
- Wrong tap: brief shake animation on the tapped button + low buzzer tone
- Sequence playback: buttons light up in order, one at a time, with tone per button

### Sequence playback coroutine

```csharp
IEnumerator PlaySequence() {
    inputLocked = true;
    yield return new WaitForSeconds(0.3f); // brief pause before playback

    foreach (int buttonIndex in sequence) {
        buttons[buttonIndex].Highlight();       // colour + scale pop
        buttons[buttonIndex].PlayTone();        // AudioSource.PlayOneShot
        yield return new WaitForSeconds(flashDuration);
        buttons[buttonIndex].Unhighlight();
        yield return new WaitForSeconds(0.15f); // gap between flashes
    }

    inputLocked = false;
    // "Go" signal — brief flash on all buttons or screen pulse
}
```

### Input rules

- Input is **locked** during sequence playback
- After playback ends, wait 0.3s before accepting input (prevents accidental late taps)
- Each player tap is compared against `sequence[currentStep]`
- On match: increment step, brief flash on tapped button, play tone
- On mismatch: trigger failure (shake + buzzer), offer retry
- On full match: trigger win

### Unity implementation

- **Button prefab**: each has `Image` (colour), child `Image` (shape icon), `AudioSource`
- **Audio**: pre-recorded sine wave tones or `AudioSource.PlayOneShot()` with generated clips per frequency
- **Button animation**: `DoTween` scale punch: `transform.DOPunchScale(Vector3.one * 0.15f, 0.1f)`
- **Shake on failure**: `transform.DOShakePosition(0.3f, strength: 5)`
- **Input lock**: `bool inputLocked` flag checked in button click handlers. Also set during player's own tap animation to prevent double-counting

### Edge cases

- **Same sequence on retry**: always regenerate a new random sequence (prevents brute-force by writing down pattern)
- **Speed below 0.5s**: do not go below 0.5s per flash — becomes a reaction test, not memory
- **Maximum sequence length**: never exceed 10. Average player recall caps at 5–7 items (Miller's Law, yourfacewhen.org)
- **Mid-playback scene close**: stop all coroutines in `OnDisable` / `OnDestroy` to prevent orphaned playback

---

## Type 3: Memory Flip

### Concept

A grid of face-down cards. Player flips two cards at a time. If they match, they stay face-up. Goal: match all pairs.

### Grid sizes

| Difficulty | Grid | Pairs | Move limit | Notes |
|------------|------|-------|-----------|-------|
| Easy | 3x2 | 3 | None | Tutorial-level — brute-forceable |
| Medium | 4x3 | 6 | 12 | 2 moves per pair — requires memory |
| Hard | 4x4 | 8 | 16 | Optimal minimum — near-perfect memory |

Move-limit approach validated over time-limit approach: doesn't penalize slow-but-careful players (Mario Party uses time, but move-limit is standard for puzzle games).

### Win condition

All pairs matched.

### Lose condition

Move limit reached (if set).

### Card state machine

```
FaceDown → (tap) → Animating (flip up) → FaceUp
                                                    \
                                                     → (first card) wait for second tap
                                                     → (second card, match) → Animating (glow) → Matched
                                                     → (second card, no match) → Animating (flip down, 0.8s delay) → FaceDown
```

Input locked during `Animating` state to prevent triple-flip bugs.

### Flip animation (Scale-X method)

```
Phase 1: scale X 1.0 → 0.0 over 0.2s
At X=0: swap sprite (back ↔ front)
Phase 2: scale X 0.0 → 1.0 over 0.2s
```

Simplest approach; used by most Unity memory game tutorials. Alternative: 3D rotation flip (`transform.Rotate(0, 180, 0)`) for more visual depth — swap sorting order at 90° midpoint.

### Visual

- Cards on a dark background with consistent grid spacing (minimum ~10px padding between cards)
- Card back: identical for all cards (any marking on the back is a bug)
- **Matched cards**: stay face-up with a subtle green glow outline + opacity reduction — distinguishes from unmatched face-up cards
- **Unmatched flip-back**: both cards flip back after 0.8s delay. Player cannot tap during this delay
- **Flip-back animation**: same scale-X method in reverse (face-up → scale X to 0 → swap to back → scale X to 1)

### Shuffle algorithm

Fisher-Yates shuffle on the card pair array at load time. No card needs to leave its original position (derangement not required).

### Unity implementation

- **Grid layout**: `GridLayoutGroup` for UI cards, or manual positioning for world-space. Center on screen with consistent margins
- **Card prefab**: two child `SpriteRenderer` objects (front/back). Front has the pair image. Back is the card back. `sortingOrder` toggled at flip midpoint
- **Match persistence**: matched cards: disable collider, set `sortingOrder` above unmatched, play glow animation, reduce alpha to 0.7
- **Input lock**: bool `inputLocked` flag during `Animating` state on any card. All card click handlers check this flag
- **Flip-back coroutine**: after two non-matching cards are face-up, wait 0.8s with input locked, then flip both back simultaneously. Tween both in parallel via `DoTween.Join()`

### Edge cases

- **Brute force without move limit**: acceptable on Easy (tutorial only). Medium+ has move limits
- **Same card tapped twice**: count as one flip (not a match attempt). Second tap on the same card does nothing
- **Accidental tap during flip-back**: input is locked — tap is ignored
- **Fat-finger on small grids**: ensure minimum card size and padding. 3x2 grid at phone resolution: cards ~80px each with 10px gap

---

## Level assignment

Each level's `barriers[].miniGameScene` references one of:

| Scene name | Type | Difficulty | Key params |
|-----------|------|-----------|------------|
| `MiniGame_Pipes_Easy` | Pipe Puzzle | Easy | No timer, 3x3, 2–3 rotatable, 0 hints |
| `MiniGame_Pipes_Medium` | Pipe Puzzle | Medium | 30s timer, 4x3, 4–5 rotatable, 1 hint |
| `MiniGame_Pipes_Hard` | Pipe Puzzle | Hard | 20s timer, 4x4, 6–8 rotatable, 2 hints |
| `MiniGame_Pattern_Easy` | Pattern Lock | Easy | 4 buttons, seq len 4, 1.0s flash |
| `MiniGame_Pattern_Medium` | Pattern Lock | Medium | 5 buttons, seq len 6, 0.8s flash |
| `MiniGame_Pattern_Hard` | Pattern Lock | Hard | 6 buttons, seq len 8, 0.6s flash |
| `MiniGame_Memory_Easy` | Memory Flip | Easy | 3 pairs, no move limit |
| `MiniGame_Memory_Medium` | Memory Flip | Medium | 6 pairs, 12 moves |
| `MiniGame_Memory_Hard` | Memory Flip | Hard | 8 pairs, 16 moves |

### Distribution guidance (25 levels)

Mini-games start at level 7 (first barrier encounter). Levels 1–6 have no mini-games.

| Levels | Type | Notes |
|--------|------|-------|
| 1–6 | None | Tutorial and mechanics introduction |
| 7–8 | Pipe Puzzle (Easy → Medium) | First barrier encounter. L7 has tutorial overlay |
| 9–15 | Pattern Lock (Easy → Medium) | Rotates to new mini-game type |
| 16–20 | Memory Flip (Medium) | Rotates to new mini-game type |
| 21–25 | Any Hard variant | At least one of each type |

Mix types within a range for variety. Avoid same mini-game type twice in a row. Levels 21–25 should have at least one of each Hard variant.

### Failure feedback per type

| Type | On failure | On retry |
|------|-----------|----------|
| Pipe Puzzle | Timer expires → brief red flash on grid → "Time's up" text, 1.5s delay | Board resets to scrambled state |
| Pattern Lock | Wrong button → buzzer tone + shake on tapped button (0.3s) → "Wrong" text | New random sequence generated |
| Memory Flip | Move limit reached → all cards briefly reveal → "Out of moves" text | Full reshuffle + re-flip |

No health penalty, no progress loss. Retry is instantaneous.

---

## Integration with existing spec

Update the `barriers[]` entry in level JSON:

```json
"barriers": [
  { "miniGameScene": "MiniGame_Pipes_Easy", "tile": [7, 3] }
]
```

No changes to `MiniGameManager`, `Barrier`, or `GameManager` needed — the scene name is already data-driven. Each mini-game scene self-contains its logic and UI.

### Implementation notes

- All three mini-game types are small enough that brute-force BFS/checking is sub-millisecond at ≤4×4 grid sizes (NP-completeness proof exists for pipe puzzles but only matters at 10×10+)
- Additive scene loading preserves main game state naturally — no manual save/restore needed
- Each mini-game scene uses its own `EventSystem` (uGUI). Disable the main scene's `EventSystem` while mini-game is active to prevent input conflicts
- On mini-game complete: unload additive scene, re-enable main scene's `EventSystem`, trigger barrier removal
- All mini-games support **free retry** — reset internal state on retry without unloading/reloading the scene
