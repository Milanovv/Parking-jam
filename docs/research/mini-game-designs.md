# Mini-Game Designs: Research Findings for Parking Jam

Research conducted July 2026. Sources: official game pages, developer documentation,
academic papers, gameplay footage analysis, Unity asset store docs, and community wikis.

---

## Table of Contents

1. [Pipe Puzzle (Rotate-to-Connect)](#1-pipe-puzzle-rotate-to-connect)
2. [Pattern Lock (Simon Says)](#2-pattern-lock-simon-says)
3. [Memory Flip (Match Pairs)](#3-memory-flip-match-pairs)
4. [Cross-Cutting: Failure & Retry Patterns](#4-cross-cutting-failure--retry-patterns)
5. [Cross-Cutting: Mini-Games as Barriers](#5-cross-cutting-mini-games-as-barriers)
6. [Recommendations](#6-recommendations)

---

## 1. Pipe Puzzle (Rotate-to-Connect)

### 1.1 Existing Implementations

| Game | Context | Mechanic detail | Source |
|------|---------|----------------|--------|
| **BioShock (2007)** | Hacking mini-game for turrets, cameras, safes, vending machines | Swap tiles on a grid to route fluid from inlet to outlet. Alarm tiles cause damage. Timed (fluid flows continuously). Failure: electric shock, enemies alerted | bioshock.fandom.com/wiki/Hacking |
| **Pipe Mania / Pipe Dream (1989)** | Standalone puzzle, Amiga → multiplatform | Place randomly-appearing pipe pieces on a grid before flowing "flooz" catches up. 7 pipe types. Real-time timer. | gamesnostalgia.com/game/pipe-dream; en.wikipedia.org/wiki/Pipe_Mania |
| **Pipe World (corovcam)** | Standalone puzzle, Unity/WebGL | Grid with source→destination. Rotate/drag pipes. Water/lava flow animation. Arcade mode + level select. 3 difficulty levels, score based on time. | corovcam.github.io/pipe-world/ |
| **Tembrica Pipe Puzzle** | Web game | 5×5 to 13×13 grids. 6 pipe types. Live "connected" counter. Hints + Show Solution. Generated from valid solution + scrambled. | tembrica.com/en/pipe-puzzle |
| **Puzzle-Pipes.com** | Web game (Net/FreeNet variant) | 4×4 to 25×25. Must connect ALL tiles into single spanning tree. No loops allowed. Flood-fill visualization. Timer optional. | puzzle-pipes.com |
| **Plumber Duck (SEELE AI)** | Web puzzle | Source→Duck exit. Tap to rotate 90°. Water flow animation on win. Orthographic camera, fat-finger hit areas. | seeles.ai/games/puzzle/plumber-duck-cute-pipe-puzzle-game |
| **Water Pipes (Mobiloids)** | Android | Classic pipe rotation with timer. Google Play listing 10M+ downloads. | play.google.com (com.mobiloids.waterpipespuzzle) |

### 1.2 Primary Source Citations

- **BioShock Hacking**: bioshock.fandom.com/wiki/Hacking — documents grid layout, alarm tiles, buyout option, frozen hack mechanic
- **Pipe Mania**: en.wikipedia.org/wiki/Pipe_Mania — original 1989 Assembly Line release, Lucasfilm Games porting
- **Pipes (puzzle)**: en.wikipedia.org/wiki/Pipes_(puzzle) — defines NP-completeness proof (Král et al. 2004), SAT-solver generation
- **Simon Tatham's Portable Puzzle Collection (Net)**: chiark.greenend.org.uk/~sgtatham/puzzles/js/net.html — reference implementation, guarantees single-solution generation
- **Pipe Puzzle generation thesis**: theses.liacs.nl/2724 — "Generating Pipes puzzles using maze-generating algorithms" (Hegeman 2023)
- **Puzzle procedural generation blog**: snellman.net/blog/archive/2019-05-14-procedural-puzzle-generator — detailed generation algorithm write-up
- **Unity Pipe World docs**: corovcam.github.io/pipe-world/ — full Unity project architecture, class diagrams, asset references

### 1.3 Edge Cases & Pitfalls

**Unsolveable generation.**
BioShock's hacking mini-game is documented to sometimes spawn impossible layouts (hazard tiles in a 'V' formation that cannot be routed around), particularly when hacking safes (bioshock.fandom.com). The player is forced to abort, take damage, and retry. This is a known community complaint.

**Solution: always generate from a solved state.**
Every reliable pipe puzzle generator (Tembrica, Puzzle-Pipes, Simon Tatham's Net) starts from a valid spanning tree or path, then scrambles by rotating tiles. This guarantees at least one solution exists (tembrica.com/en/pipe-puzzle FAQ, chiark.greenend.org.uk).

**Multiple solutions / ambiguity.**
If the puzzle permits multiple solutions, the player may solve it in a way the generator didn't expect. The connection-checking logic must accept ANY valid path from source to sink, not a specific pre-computed one. Tatham's Net enforces a unique solution by using SAT-solver verification.

**Connectivity check fails on rotation.**
A Stack Overflow post (stackoverflow.com/questions/55222754) describes a Unity pipe game where rotating a tile that is part of a connected group doesn't break the water-flow visualization for downstream tiles, because they remain connected to other water-filled tiles. Fix: re-run a full BFS/DFS from the source after every rotation, rather than relying on trigger colliders.

**NP-completeness.**
General pipe puzzle solving is NP-complete (Král et al. 2004, via Planar 1-in-3-SAT reduction). For the small grids in Parking Jam (3×3 to 4×4), brute-force BFS over all 4^N rotations (where N = rotatable tiles) is fast enough. 4^8 = 65,536 states — trivial for a modern device.

**Unsolvable scrambled state.**
If the scramble rotation count is too high, the resulting configuration may have a solution path length that exceeds the time limit, or may accidentally create a disconnected component that cannot be reconnected. Scramble by rotating each tile 0–3 times randomly, then verify with BFS that a solution exists. Re-scramble if not.

### 1.4 Difficulty Calibration

| Source | Easy | Medium | Hard | Expert |
|--------|------|--------|------|--------|
| **Tembrica** | 5×5, ~50% pre-placed | 7×7 | 9×9 | 11×11, 13×13 |
| **Puzzle-Pipes.com** | 4×4 | 5×5, 7×7 | 10×10 | 15×15, 20×20, 25×25 |
| **Pipe World** | 3×3 (tutorial) | 4×4 | 5×5+ | Time-based scaling |
| **BioShock** | 5×5 grid, 2 alarm tiles | 5×5, 3 alarm tiles | 6×6, 4 alarm tiles | 6×6, 5+ alarm tiles |
| **Parking Jam (spec)** | 3×3, 2–3 rotatable, no timer | 4×3, 4–5 rotatable, 30s | 4×4, 6–8 rotatable, 20s | — |

**Key calibration patterns from research:**
- Grid size is the primary difficulty lever (tembrica.com, puzzle-pipes.com)
- Pre-placed / fixed tiles act as scaffolding for beginners (Tembrica Easy: ~50% correct)
- Time limit adds pressure but must leave enough margin: BioShock's fluid flow is slow enough that careful players finish with seconds to spare
- Alarm/hazard tiles add "don't touch" constraints (BioShock) — optional for Parking Jam
- All surveyed games agree: **scramble from solution, verify with solver**

### 1.5 Unity Implementation Notes

**Sprite rotation pivot.**
2D pipe sprites must have their pivot point at the center of the tile. When rotating via `transform.Rotate(0, 0, 90)` or `transform.eulerAngles`, the rotation axis must be Vector3.forward for 2D (discussions.unity.com/t/rotating-a-2d-object). If using UI Image components, rotate the RectTransform.

**Connectivity check.**
```
BFS from source tile:
  for each neighbor in 4 directions:
    if pipe has open end facing that direction
    AND neighbor has open end facing back:
      enqueue neighbor
  if sink reached → connected
```
Implement as a static method called after every rotation. For small grids (≤16 tiles), this is sub-millisecond (stackoverflow.com/questions/55222754).

**Grid data structure.**
A 2D array of `PipeTile` classes/enums, each storing `tileType` (straight, corner, T, cross, source, sink) and `currentRotation` (0–3). The sprite is determined by `tileType` combined with `currentRotation`. The Pipe World project (corovcam.github.io/pipe-world/) uses this exact pattern with a `PipeHandler.cs` per tile.

**Flow visualization.**
After connectivity check succeeds, play a water-flow animation along the path. Pipe World uses a "Fill" sprite overlay that activates on connected tiles. Tembrica uses a glow effect on connected segments.

**Tap detection.**
Use `IPointerClickHandler` or `Physics2D.Raycast` with a `BoxCollider2D` on each tile. Enlarge the collider slightly beyond the sprite bounds for fat-finger tolerance (recommended by Plumber Duck docs: seeles.ai).

**Water flow path animation.**
Use `DoTween` or a coroutine to sequentially show fill sprites along the BFS path from source to sink. Duration: ~0.3–0.5s total for a 3×3 grid.

---

## 2. Pattern Lock (Simon Says)

### 2.1 Existing Implementations

| Game | Context | Mechanic detail | Source |
|------|---------|----------------|--------|
| **Simon (1978, Milton Bradley)** | Standalone electronic game | 4 colored buttons (red, blue, green, yellow). Sequence length 1→infinite, speed increases. Audio tone per color. | en.wikipedia.org/wiki/Simon_(game) |
| **Donkey Kong Country 3 (GBA)** | Barrier: free Banana Bird from crystal prison | 4 colored crystals. Must repeat sequence of increasing length. Failure: restart. | tvtropes.org (SimonSaysMinigame) |
| **Stardew Valley** | Unlock: Ginger Island crystal cave | Colored crystals light up and play notes. Repeat sequence. Rewards golden walnut. | tvtropes.org (SimonSaysMinigame) |
| **RuneScape** | Random events / puzzle doors | Follow-the-leader with living statue. Wrong input = damage + restart. | tvtropes.org (SimonSaysMinigame) |
| **Shadowrun Returns: Hong Kong** | Hacking: Blocker IC in the Matrix | Number-pad sequence repetition. Required for story progression. | tvtropes.org (SimonSaysMinigame) |
| **Harry Potter (GBA)** | Spell learning: copy wand movements | 3 rounds, increasing sequence. DDR-style input. | tvtropes.org (SimonSaysMinigame) |
| **Aquaria** | Mini-boss encounter | Simon Says pattern matching as combat mechanic. | allthetropes.org (SimonSaysMiniGame) |
| **Your Turn to Die** | "Memory Dance" attraction | Repeat dance sequence from opponent. Wrong order = damage, too many failures = game over. | tvtropes.org (SimonSaysMinigame) |
| **Bioshock** | Safe code input | 4-directional code sequence. Different from pipe puzzle — this is the other hacking variant. | bioshock.fandom.com/wiki/Hacking |

### 2.2 Primary Source Citations

- **Simon (original game)**: en.wikipedia.org/wiki/Simon_(game) — designed by Ralph Baer and Howard Morrison, launched 1978 at Studio 54
- **TV Tropes: "Simon Says" Mini-Game**: tvtropes.org/pmwiki/pmwiki.php/Main/SimonSaysMinigame — exhaustive catalog of implementations across genres
- **All The Tropes: "Simon Says" Mini-Game**: allthetropes.org/wiki/"Simon_Says"_Mini-Game — similar catalog, additional entries
- **RPG Maker MZ Simon Says plugin**: undermax.itch.io/simonsays — commercial plugin explicitly documents use case: "solve to unlock doors, chests, or progress deeper into the area"
- **Simon Says educational reverse engineering**: teachengineering.org/activities/view/rice-2615-simon-decoded — documents original game specs: 4 colors, tone per color, speed increase per round
- **Working memory research**: yourfacewhen.org/games/memory/ cites average human sequence recall at 5–7 items, matching Miller's Law

### 2.3 Edge Cases & Pitfalls

**Working memory limit (7 ± 2 items).**
The original Simon game and most implementations hit a difficulty wall around sequence length 8–10. Average players plateau at 5–7 (yourfacewhen.org). For Parking Jam's spec (max 8), this is the upper edge of comfortable recall. Players with low working memory may find even length 6 frustrating.

**Impossible retry with same sequence.**
If the player fails and retries the same sequence, they can brute-force it by writing down the pattern. The spec already addresses this: regenerate a new random sequence on each attempt.

**Speed increases cause reflex failures.**
In the original Simon, speed increases every 4–5 successful rounds. Players fail not because they can't remember, but because they can't tap fast enough. For Parking Jam, keep playback speed at a fixed rate per difficulty level (as spec'd), don't increase within a single mini-game.

**Audio-dependent play.**
Color-blind or hearing-impaired players need both audio + visual cues. The original Simon used distinct tones per color AND visual light. All surveyed implementations provide both. Ensure your button colors are also distinguishable by shape, icon, or position.

**Input buffering / accidental double-tap.**
If the player taps during the sequence playback, it should be ignored (input locked during demonstration phase). After playback ends, wait for the first valid tap. Many implementations flash a "Go" signal or change button states.

**Sequence playback interruption.**
If the player closes the mini-game mid-playback, the sequence state must reset cleanly. Use `OnDisable` / `OnDestroy` to stop coroutines.

**Fatigue on repeated retries.**
In Harry Potter (GBA) and Your Turn to Die, repeated failure on a Simon Says mini-game results in damage accumulation but never a hard lock. This matches Parking Jam's "free retry" model.

### 2.4 Difficulty Calibration

| Source | Easy | Medium | Hard | Notes |
|--------|------|--------|------|-------|
| **Original Simon (1978)** | Round 1: 1 flash | Round 5: 5 flashes | Round 10: 10 flashes | Speed increases every 4 rounds. 1.0s → 0.5s per flash |
| **RPG Maker MZ Plugin** | 3 rounds, slow speed | 5 rounds, medium speed | 8 rounds, fast speed | Configurable via plugin params |
| **Death Order (Roblox)** | 2–3 commands | 4–5 commands | 6+ commands | Punishment: instant elimination |
| **Parking Jam (spec)** | 4 buttons, seq len 4, 1.0s | 5 buttons, seq len 6, 0.8s | 6 buttons, seq len 8, 0.6s | Fixed speed per difficulty |

**Calibration notes:**
- Button count affects difficulty combinatorially: 4 buttons with seq len 4 = 4^4 = 256 possible sequences; 6 buttons = 6^8 = 1.6M. Higher button count reduces the chance of guessing.
- Sequence length should never exceed 10 for a casual mobile audience (yourfacewhen.org cites 5–7 average recall).
- Flash speed below 0.5s per step becomes a reaction test, not a memory test. The spec's 0.6s minimum is acceptable.
- The original Simon used a constant speed per round within a difficulty band. Don't accelerate mid-game.

### 2.5 Unity Implementation Notes

**Sequence playback coroutine.**
```csharp
IEnumerator PlaySequence() {
    inputLocked = true;
    foreach (int buttonIndex in sequence) {
        buttons[buttonIndex].Highlight();
        yield return new WaitForSeconds(flashDuration);
        buttons[buttonIndex].Unhighlight();
        yield return new WaitForSeconds(0.15f); // gap between flashes
    }
    inputLocked = false;
}
```

**Input timing window.**
After playback, accept taps with no time limit (per spec). The player should not be rushed. Each tap compares against `sequence[currentStep]`. On match: increment step. On mismatch: trigger failure. On full match: trigger win.

**Color/audio pairing.**
Each button needs a unique color + tone frequency. The original Simon used:
- Red: 660 Hz (C5)
- Green: 990 Hz (G5)
- Blue: 440 Hz (A4)
- Yellow: 770 Hz (F5)
Use `AudioSource.PlayOneShot()` with generated sine waves or pre-recorded tones (discussions.unity.com).

**Button press animation.**
Scale pop + brightness change: scale 1.0 → 1.15 → 1.0 over 0.1s. Use `DoTween` or `LeanTween` for elastic easing.

**Accessibility.**
Add shapes/icons to each button (circle, triangle, square, diamond, star, hexagon) so color-blind players can distinguish them. This is a common critique of the original Simon (teachengineering.org).

**No input during playback.**
Set a `bool inputLocked` flag during the demonstration phase. Also block input during the brief flash animation of the player's own taps to prevent double-counting.

---

## 3. Memory Flip (Match Pairs)

### 3.1 Existing Implementations

| Game | Context | Mechanic detail | Source |
|------|---------|----------------|--------|
| **Mario Party (N64)** | "Memory Match" mini-game | Ground-pound panels to reveal. Find all pairs in 45s. Bowser panel stuns. | tvtropes.org (MemoryMatchMiniGame) |
| **Mario Party DS** | "Memory Mash" | 2-player race. 8 pairs. Ground-pound to flip. | tvtropes.org (MemoryMatchMiniGame) |
| **Banjo-Kazooie** | Gobi's Valley pyramid | 4×4 grid (8 pairs). 100 seconds. Ground-pound tiles. | tvtropes.org (MemoryMatchMiniGame) |
| **Super Mario 64 DS** | Luigi's mini-game | Card flip. Match pairs. Reward on match, penalty on miss. | tvtropes.org (MemoryMatchMiniGame) |
| **New Super Mario Bros. Wii** | Power-Up Panels (Toad House) | Flip panels. Match 2 same power-ups to collect. Matching Bowser = lose. | tvtropes.org (MemoryMatchMiniGame) |
| **Clubhouse Games: 51 Worldwide Classics** | Standard Concentration | Classic card matching. Multiple difficulty options. | tvtropes.org (MemoryMatchMiniGame) |
| **SpongeBob: Battle for Bikini Bottom (PC)** | "Manhole Memory" | Match character/robot pairs. 3 rounds. Rewards story items. | tvtropes.org (MemoryMatchMiniGame) |
| **Bee Swarm Simulator (Roblox)** | Night Memory Match | 4×4 grid. 3 base chances (increases with badges). 8h cooldown. | bee-swarm-simulator.fandom.com |
| **Sol's RNG (Roblox)** | Memory Match mini-game | Card matching for rewards. | sol-rng.fandom.com |
| **Bitsboard** | Educational | up to 32 pairs. Face-up mode for beginners. Multiple matching modes (image, text, audio). | bitsboard.com/games/memory-cards |
| **Starlight Tools** | Web memory game | 4×4 (8 pairs), 6×4 (12 pairs), 6×6 (18 pairs). Star rating based on moves. | starlighttools.org/games/memory-match |

### 3.2 Primary Source Citations

- **TV Tropes: Memory Match Mini-Game**: tvtropes.org/pmwiki/pmwiki.php/Main/MemoryMatchMiniGame — comprehensive catalog of ~50+ implementations
- **Unity Asset Store: Memory Card Flip**: assetstore.unity.com/packages/templates/packs/memory-card-flip-119222 — complete Unity project with flip animation, multiplayer support
- **Unity Asset Store: Memory Match Game Template**: assetstore.unity.com/packages/templates/tutorials/memory-match-game-template-214038 — Unity Learn template
- **SBGames 2018 academic paper**: sbgames.org/sbgames2018/files/papers/ArtesDesignFull/188275.pdf — "A Memory Game for All: Differences and Perception as a Design Strategy" — accessible memory game design for visually impaired
- **RPG Maker MV Memory plugin**: uwls-software.itch.io/memory-match-2-minigame-for-rpg-maker-mv — commercial plugin, supports up to 32 pairs, custom graphics, common event triggers on win/lose
- **RPG Maker MZ Flip Cards plugin**: undermax.itch.io/flipcards — inspired by DQVII Lucky Panel, flip animation, customizable

### 3.3 Edge Cases & Pitfalls

**Brute force trivialization.**
Without a move limit or time limit, players can brute-force by flipping every card systematically (flip card 1, flip card 2, if no match, flip card 1 again with card 3, etc.). The spec already notes this is "acceptable for v1." For harder difficulties, a move limit forces genuine memory play.

**Input lock during flip-back.**
The spec already addresses this: 0.8s delay after flipping two non-matching cards, during which no input is accepted. This is standard practice across all surveyed implementations (Hamza-Abouelwahab/Memory-Game, starlighttools.org). The delay must be long enough to register the card positions but short enough not to feel sluggish.

**Card position memorization by location, not image.**
Players can win by memorizing positions rather than images ("the star is top-left, the circle is middle-right"). This is inherent to the mechanic and not a bug. Move limits still make this challenging.

**Peeking / accidental reveal.**
If cards are too close together, a fat-finger tap on mobile may flip the wrong card. Ensure minimum card spacing (Padding: ~10px between cards on a 4×4 grid at phone resolution). Starlight Tools uses 6×4 as "medium" which gives smaller cards — consider this when choosing grid sizes.

**Early game luck.**
The first flip is always random. A lucky first-match feels good; an unlucky spread of pairs makes the first few moves feel wasteful. This is accepted in the genre — the SBGames paper notes that luck in first moves is balanced by memory in later moves.

**Too few pairs feel trivial.**
The spec's Easy (3 pairs, 3×2 grid) is very easy — most players solve it without any memory at all. This is appropriate for tutorial levels but may feel pointless to experienced players. The spec assigns Memory Flip only to levels 16–20 (late game), where Medium (6 pairs) is used.

**Card flip animation timing.**
The flip animation must be fast enough not to slow gameplay but slow enough to read the card. The spec's 0.2s per direction (0.4s total) is in line with Starlight Tools (0.3s total) and the Unity Memory Card Flip asset (0.25s). The 0.8s delay after a mismatch is standard.

### 3.4 Difficulty Calibration

| Source | Easy | Medium | Hard | Notes |
|--------|------|--------|------|-------|
| **Mario Party (N64)** | — | 4×4 (8 pairs), 45s | — | Timer-based, not move-based |
| **Banjo-Kazooie** | — | 4×4 (8 pairs), 100s | — | Time limit reduces on retry (75s) |
| **Starlight Tools** | 4×4 (8 pairs) | 6×4 (12 pairs) | 6×6 (18 pairs) | Move counter + star rating |
| **Bitsboard** | 2 pairs, face-up | 6 pairs | 32 pairs | Educational; face-up mode for accessibility |
| **RPG Maker Plugin** | 8 pairs, 20 moves | 12 pairs, 30 moves | 32 pairs, 50 moves | Configurable pairs and move limit |
| **Parking Jam (spec)** | 3×2 (3 pairs), no limit | 4×3 (6 pairs), 12 moves | 4×4 (8 pairs), 16 moves | Move limit on Medium+ |

**Calibration notes:**
- 3 pairs (Easy) is solvable by brute force in 6 moves max. With no move limit, it's a gimme — good for tutorial.
- 6 pairs (Medium) with 12 moves gives 2 moves per pair — tight but fair. Average optimal solution is 7 moves (3 initial + 4 matched from memory).
- 8 pairs (Hard) with 16 moves gives 2 moves per pair — requires near-perfect memory. 16 moves is the exact optimal minimum (8 pairs × 2 flips each).
- Time limits are used in Mario Party and Banjo-Kazooie but not in the spec. The spec's move-limit approach is better for a puzzle game because it doesn't penalize slow-but-careful players.

### 3.5 Unity Implementation Notes

**Card flip animation methods.**

**Method A: Scale-X flip (simplest).**
```
Phase 1: scale X 1.0 → 0.0 over 0.2s
At X=0: swap sprite (back ↔ front)
Phase 2: scale X 0.0 → 1.0 over 0.2s
```
This is the most common approach (spec's recommendation). Use `DoTween` or a coroutine with `transform.localScale`. Works for both world-space and UI canvases.

**Method B: 3D rotation flip (more visually pleasing).**
```
transform.Rotate(0, 180, 0) over 0.4s using transform.eulerAngles
Swap sprite sorting order at 90° (when card is edge-on)
```
See GunnarKarlsson's Unity gist (gist.github.com/GunnarKarlsson/63ce3512e01276f0eba5f9a4bb0e64cb). Swap `sortingOrder` of front and back child sprites at the midpoint. Requires 2 child `SpriteRenderer` objects.

**Method C: Shader-based flip (most performant).**
Use a custom Shader Graph with a Flip Node that manipulates UV coordinates. See "EASILY Flip Cards in Unity" (youtube.com/watch?v=15Bh5QiScbY) for a DOTween + shader approach. This avoids gameobject hierarchy overhead.

**Card state machine.**
Each card needs a state: `FaceDown`, `FaceUp`, `Matched`, `Animating`. Lock input during `Animating`. The memory-game locking mechanism is essential to prevent triple-flip bugs (documented in Hamza-Abouelwahab/Memory-Game and the SBGames paper).

**Grid layout.**
Use `GridLayoutGroup` for UI-based cards or manual positioning for world-space cards. Ensure cards are centered on screen with consistent margins. The Starlight Tools layout uses even spacing that scales to screen size.

**Match persistence.**
Matched cards should stay face-up. Add a subtle glow/outline and reduce opacity slightly to distinguish from unmatched face-up cards (spec: "brief glow + fade persistence"). Disable their colliders so they can't be re-interacted with.

**Shuffle algorithm.**
Fisher-Yates shuffle on the card pair array at game start. Ensure no card stays in its original position (derangement isn't necessary but avoids "same spot" reveals).

---

## 4. Cross-Cutting: Failure & Retry Patterns

### 4.1 How Games Handle Mini-Game Failure

| Game | Mini-Game | Failure consequence | Retry model |
|------|-----------|-------------------|-------------|
| **BioShock** | Pipe Puzzle (hack) | Electric shock (health damage). Alarm triggers enemy alert. | Infinite retry. Player can abort hack and try again. Buy-out option. |
| **Donkey Kong Country 3** | Simon Says (bird cage) | Failure resets puzzle. No progress loss. | Infinite retry, no penalty. |
| **Stardew Valley** | Simon Says (crystals) | Nothing — try again immediately. | Infinite retry, no penalty. |
| **Your Turn to Die** | Memory Dance (Simon) | Damage accumulated. Too many = game over. | Limited retries (lives system). |
| **Banjo-Kazooie** | Memory Flip (pyramid) | Time wasted; try again. | Infinite retry within level. |
| **Mario Party** | Memory Match | Lose coins / lose turn. | Per-round retry in party mode. |
| **Death Order (Roblox)** | Simon Says | Instant elimination. | Permadeath (battle royale context). |
| **Moonlighter** | Various shop mini-games | Time wasted, no item gained. | Infinite retry, opportunity cost. |

**Key pattern: Casual/mobile games use infinite free retry.**
The research strongly supports Parking Jam's "free retry — no cost to restart" approach. BioShock is the notable exception: failure has a health cost, but the player can pay to skip (buy-out). For a sliding-block puzzle game, punishing failure would violate the casual genre expectation.

**Why infinite retry works here:**
- The mini-game is a gate, not a core challenge. The challenge is the sliding-block puzzle itself.
- The mini-game is short (15–30s). Retrying isn't punishing.
- Free retry keeps the player in flow state — they don't need to redo the board.
- This matches the escape room "soft gate" pattern (see barrier-first-gameplay-flow.md): the barrier should be surmountable, not a hard wall.

### 4.2 Death/Retry Loop Design Principles

From game design literature (joyplayx.com/article/how-to-handle-failure-in-games, gamedesigning.org/learn/game-difficulty/):

1. **Fast restarts**: Minimize load time between failure and retry. The spec's additive scene approach should load instantly.
2. **Clear feedback**: Why did the player fail? Pipe puzzle: timer expired or no path. Pattern Lock: wrong button highlighted. Memory Flip: no visual — just flip-back.
3. **Fairness**: Failure must feel like the player's fault, not the game's. Pipe puzzles generated from a valid solution guarantee fairness. Pattern Lock sequences should be new each attempt.
4. **Progress preservation**: Don't reset the main board on mini-game failure. The barrier stays locked; the player just needs to solve the mini-game again.

---

## 5. Cross-Cutting: Mini-Games as Barriers

### 5.1 Games Where Mini-Games Gate Progress

| Game | Barrier | Mini-Game | Context |
|------|---------|-----------|---------|
| **BioShock** | Locked door, safe, turret | Pipe Puzzle | Hacking to unlock doors, disable cameras, turn turrets friendly |
| **Donkey Kong Country 3 (GBA)** | Crystal prison (Banana Bird) | Simon Says | Memory sequence to free bird |
| **Stardew Valley** | Ginger Island crystal cave door | Simon Says | Sequence repeat for golden walnut |
| **RuneScape** | Puzzle room doors | Simon Says (statue) | Follow-the-leader to unlock |
| **SpongeBob: Battle for Bikini Bottom (PC)** | Manhole covers | Memory Flip | Match pairs to get Magic Shop items for story progression |
| **Super Mario 64 DS** | Luigi's mini-game room | Memory Flip | Mini-game world accessed via doors |
| **Harry Potter (GBA)** | Spell progression gate | Simon Says (wand) | Must pass sequence to learn spell to progress |
| **Bee Swarm Simulator (Roblox)** | Night Memory Match (behind Bear Gate) | Memory Flip | 8h cooldown after each successful match — prevents infinite grind |
| **Various RPG Maker plugins** | Doors, chests, progression gates | Simon Says, Memory Flip | Explicitly sold as "gate" mechanics (undermax.itch.io, uwls-software.itch.io) |

### 5.2 Key Design Insights

**The mini-game as "key" metaphor.**
In every surveyed game, the mini-game acts as a **key** — solve it, and the gate stays open permanently. No game requires re-solving the same mini-game to pass through the same gate. This validates Parking Jam's spec: barrier stays open once unlocked.

**Pacing: mini-game difficulty relative to context.**
- In BioShock, the pipe puzzle difficulty increases with the value of the hacked object (safes are hardest). This maps well to Parking Jam: later levels have harder mini-games.
- In Donkey Kong Country 3, the Simon Says difficulty increases per bird collected. Same as Parking Jam's progression.
- In Bee Swarm Simulator, the Memory Flip has an 8-hour cooldown — far too punishing for Parking Jam, but appropriate for a grind-heavy Roblox game.

**RPG Maker plugin documentation confirms the pattern.**
Undermax's Simon Says plugin (undermax.itch.io/simonsays) explicitly pitches: "Place a Simon Says puzzle in an ancient ruin or magical temple. Then players must successfully repeat increasingly difficult sequences to unlock doors, chests, or progress deeper into the area." This is exactly Parking Jam's mechanic — the barrier is the "door," the mini-game is the "lock."

### 5.3 What the Spec Gets Right (Validated by Research)

| Spec decision | Research validation |
|--------------|-------------------|
| 3 mini-game types | Each type is a well-established stock puzzle with decades of implementation history |
| Free retry | Matches casual game conventions; avoids BioShock-style frustration |
| Fixed difficulty per level | Matches all surveyed games — no dynamic difficulty adjustment needed |
| Barrier stays open permanently | Universal pattern across all surveyed barrier-mini-game implementations |
| No mini-game type twice in a row | All surveyed games vary mini-games to avoid fatigue |
| Additive scenes for mini-games | Matches Unity best practices for temporary UI/game state |
| 15–30 second completion target | Matches Simon (15s per round), Memory Flip (30-100s with timer), Pipe Puzzle (20-30s with timer) |

### 5.4 What the Spec Could Improve (Research Gaps)

| Gap | Suggestion | Source |
|-----|-----------|--------|
| No accessibility considerations | Add shape/icon to Pattern Lock buttons for color-blind accessibility | teachengineering.org, sbgames.org |
| No hint system for Pipe Puzzle | Add a limited-use hint that highlights one tile to rotate | tembrica.com/en/pipe-puzzle (scaling hints per difficulty) |
| Memory Flip Easy (3 pairs) may feel too trivial | Acceptable as tutorial-only; don't use in main progression after level 15 | All surveyed games start at 4+ pairs |
| No failure animation/feedback details | Each type needs distinct failure feedback (shake, sound, brief flash) | BioShock (electric shock), Simon (buzzer tone) |
| No consideration of audio design | Pattern Lock NEEDS distinct audio tones per button — core to the mechanic | Original Simon (en.wikipedia.org) |

---

## 6. Recommendations

### 6.1 For Pipe Puzzle

| Topic | Recommendation | Rationale |
|-------|---------------|-----------|
| Generation algorithm | Start from valid path → scramble tiles → verify with BFS | Eliminates unsolvable boards (Tembrica, Tatham, Puzzle-Pipes consensus) |
| Connectivity check | Full BFS from source after every rotation | Fixes the "connected group stays highlighted" bug (stackoverflow.com) |
| Grid sizes | 3×3, 4×3, 4×4 as spec'd | Validated by Pipe World (3×3 tutorial), Tembrica (5×5 Easy) |
| Rotatable tiles | 2–3/4–5/6–8 as spec'd | Matches scramble-from-solution approach (rotate random subset of tiles) |
| Timer | No timer on Easy, 30s/20s on Medium/Hard | BioShock proves timed pipe puzzles work; no timer on Easy for learning |
| Hint system | Add 1 free hint on Medium, 2 on Hard (highlights one tile to fix) | Tembrica uses this effectively; prevents stalling |
| Visual feedback | Water flow animation on all connected tiles; green glow on correct path | Pipe World and Tembrica both use live connectivity highlighting |

### 6.2 For Pattern Lock

| Topic | Recommendation | Rationale |
|-------|---------------|-----------|
| Button layout | 4 (2×2) / 5 (cross) / 6 (2×3) as spec'd | Original Simon had 4; 5–6 adds combinatorial difficulty without overloading |
| Sequence length | 4 / 6 / 8 as spec'd | Within working memory limits (yourfacewhen.org: 5–7 average) |
| Playback speed | 1.0s / 0.8s / 0.6s as spec'd | Don't go below 0.5s (becomes reaction test, not memory test) |
| Audio tones | One distinct tone per button | Core mechanic — without it, it's not Simon Says (original Simon patent) |
| Accessibility | Add shapes/icons to buttons | Essential for color-blind players (teachengineering.org) |
| Retry sequence | Always generate NEW sequence | Prevents brute-force by writing down pattern (spec already mandates) |
| Input lock | Block input during playback + 0.3s after | Prevents accidental early taps |

### 6.3 For Memory Flip

| Topic | Recommendation | Rationale |
|-------|---------------|-----------|
| Grid sizes | 3×2 / 4×3 / 4×4 as spec'd | Validates against Starlight Tools (4×4 Easy) and Banjo-Kazooie (4×4 Medium) |
| Move limits | No limit / 12 / 16 as spec'd | Without limits on Easy, brute force is fine for tutorial. 16 moves on Hard = optimal minimum |
| Flip animation | Scale-X method (0.2s each direction) | Simplest to implement, used by most Unity tutorials |
| Flip-back delay | 0.8s after mismatch as spec'd | Standard across all implementations; long enough to memorize, short enough to flow |
| Input lock | Block input during flip animation + flip-back delay | Prevents triple-flip bug (Hamza-Abouelwahab/Memory-Game) |
| Matched card visual | Glow + opacity reduction | Distinguishes from face-up unmatched cards without confusion |
| Card back design | Identical back for all cards | Essential for fair play — any marking on the back is a bug |

### 6.4 Overall Integration

| Topic | Recommendation |
|-------|---------------|
| Failure cost | Zero. Free retry, no penalty, no animation that wastes time. |
| Success reward | Barrier removal animation (boom gate rising). No score bonus — the reward is progress. |
| Scene loading | Additive scenes via `SceneManager.LoadSceneAsync("MiniGame_...", LoadSceneMode.Additive)`. Unload on complete. |
| State preservation | Main game timer/move counter frozen while mini-game is active (as spec'd). |
| Level assignment | Follow the spec's distribution table. Mix types within level ranges. |
| First encounter | Level 1 or 2 should have an Easy Pipe Puzzle with no timer and a visual "connect the water" tutorial overlay. |

---

## Appendix: Primary Source URL Index

| Source | URL |
|--------|-----|
| BioShock Hacking Wiki | bioshock.fandom.com/wiki/Hacking |
| Pipe Mania Wikipedia | en.wikipedia.org/wiki/Pipe_Mania |
| Pipes (puzzle) Wikipedia | en.wikipedia.org/wiki/Pipes_(puzzle) |
| Simon Tatham's Net | chiark.greenend.org.uk/~sgtatham/puzzles/js/net.html |
| Tembrica Pipe Puzzle | tembrica.com/en/pipe-puzzle |
| Puzzle-Pipes.com | puzzle-pipes.com |
| Pipe World Documentation | corovcam.github.io/pipe-world/ |
| Pipe Puzzle Generation Thesis | theses.liacs.nl/2724 |
| Procedural Puzzle Generator Blog | snellman.net/blog/archive/2019-05-14-procedural-puzzle-generator |
| Unity Pipe Connection Question | stackoverflow.com/questions/55222754 |
| Unity Rotating 2D Object | discussions.unity.com/t/rotating-a-2d-object/671265 |
| Unity Card Flip Gist | gist.github.com/GunnarKarlsson/63ce3512e01276f0eba5f9a4bb0e64cb |
| Unity Memory Card Flip Asset | assetstore.unity.com/packages/templates/packs/memory-card-flip-119222 |
| Unity Card Flip Tutorial (YouTube) | youtube.com/watch?v=15Bh5QiScbY |
| Simon (game) Wikipedia | en.wikipedia.org/wiki/Simon_(game) |
| TV Tropes: Simon Says Mini-Game | tvtropes.org/pmwiki/pmwiki.php/Main/SimonSaysMinigame |
| TV Tropes: Memory Match Mini-Game | tvtropes.org/pmwiki/pmwiki.php/Main/MemoryMatchMiniGame |
| All The Tropes: Simon Says | allthetropes.org/wiki/%22Simon_Says%22_Mini-Game |
| RPG Maker MZ Simon Says Plugin | undermax.itch.io/simonsays |
| RPG Maker MV Memory Match Plugin | uwls-software.itch.io/memory-match-2-minigame-for-rpg-maker-mv |
| SBGames 2018 Memory Game Paper | sbgames.org/sbgames2018/files/papers/ArtesDesignFull/188275.pdf |
| Starlight Tools Memory Match | starlighttools.org/games/memory-match |
| Plumber Duck Game Docs | seeles.ai/games/puzzle/plumber-duck-cute-pipe-puzzle-game |
| Working Memory / Simon Stats | yourfacewhen.org/games/memory/ |
| Game Failure Loop Design | joyplayx.com/article/how-to-handle-failure-in-games |
| Game Difficulty Balancing | gamedesigning.org/learn/game-difficulty/ |
| Bee Swarm Simulator Memory Match | bee-swarm-simulator.fandom.com/wiki/Memory_Match |
