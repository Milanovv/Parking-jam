# Exit Animation Mechanics — Research Report

**Project:** Parking Jam  
**Date:** 2026-07-29  
**Purpose:** Investigate how sliding-block puzzle games and grid-based puzzle games handle the moment a vehicle clears a level — specifically to validate (or challenge) the proposed "car drives to barrier → barrier lifts → car drives off-screen" animation for Parking Jam.

---

## 1. Parking Jam 3D (Popcore / BPTop / Famobi)

| Source | Description |
|---|---|
| gamepix.com (descriptor) | *"When the car hits the road, it will automatically drive out and leave the area."* |
| jil-club.com review | *"Every level ends with a satisfying animation of the cars leaving the parking lot and a coin reward."* |
| mwm.ai / App Store screenshot | Screenshot labelled *"Dynamic Clearance Action — Observe how powerful smash effects clear the path for exiting vehicles."* |
| 1001games.com descriptor | *"Swipe the direction you want them to drive in... Then, they will drive away."* |

**Mechanism:** Per-car auto-exit animation. The player drags a car until it reaches the road edge; the car then autonomously drives off-screen. There is **no final "all-cars-cleared" sequence** — the last car's auto-exit **is** the level completion.  
**Barrier/gate:** None observed.  
**Duration:** ~0.5–1 s per car.  
**Verdict:** The auto-drive-off pattern is already established in this franchise. The proposed animation extends it (barrier lift) but the core auto-drive matches convention.

---

## 2. Rush Hour (ThinkFun / digital ports)

| Source | Description |
|---|---|
| Wikipedia | *"The goal of the game is to get only the red car out through the exit of the board."* |
| Google Play (liamlillian) | Feature bullet: *"Satisfying red car exit animation on puzzle completion."* |
| playbrain.games | *"Your goal is to clear a path so the red car can reach the exit on the right side of the board."* |
| drrajshah.com (web version) | Upon solving, shows *"🎉 You solved it!"* overlay with move stats. |

**Mechanism:** In the **physical board game**, the red car's last slide is a manual move — the player slides it out of the exit slot. In **digital versions**, the red car often auto-slides the final cell or does a brief "drive out" animation after the last obstacle is moved.  
**Barrier/gate:** None. The exit is a permanent opening on the right side of the board.  
**Duration:** ~0.3–0.5 s (brief slide or pop).  
**Verdict:** The satisfaction comes from the player clearing the path, not from an elaborate exit spectacle. Digital versions add a tiny auto-animation at the very end — similar to the per-car auto-drive in Parking Jam but much shorter.

---

## 3. Unblock Me (Kiragames)

| Source | Description |
|---|---|
| App Store (Kiragames) | *"The goal is to unblock the red block out of the board by sliding the other blocks out of its way."* |
| thanassis.space (solver blog) | *"You have to move horizontal blocks left or right, and vertical blocks up or down, in order to free the red block — i.e. allow it to escape from the exit to its right."* |
| YouTube walkthrough (4qmwXEgBx9w) | Player manually drags the red block to the exit on every level. No auto-animation. |

**Mechanism:** The player performs the last move manually — dragging the red block through the exit opening. There is **no auto-drive animation**. The level-complete signal is typically a "level cleared" popup with star rating.  
**Barrier/gate:** None.  
**Duration:** N/A (manual move).  
**Verdict:** No auto-animation at all. Pure player agency for the final move. The proposed Parking Jam animation is the opposite approach.

---

## 4. Block Slide / Block Escape (generic genre)

| Source | Description |
|---|---|
| blockslide.io | *"Strategically slide vibrant colored blocks across the screen to guide them to their designated exits."* |
| 2games.io | *"The goal is to slide each block into the matching colored gate to remove it from the grid."* |
| yoplay.io (1,859-level version) | Describes obstacles: *"arrows, layers, combined blocks, stars, chains, bombs, opening/closing doors, ropes, color paths, barrels, movable locks, frozen exits."* |

**Mechanism:** Each block slides to a **matching coloured gate/exit**. The gate is static; when the block reaches it, the block is consumed and removed. In some variants gates open/close automatically when conditions are met.  
**Barrier/gate:** Yes — coloured gates serve as exits. They are static visuals, not animated barrier arms.  
**Duration:** Instant removal on contact (~0.1 s).  
**Verdict:** The idea of a "gate" that a block/vehicle passes through to be removed is established. However, these gates are purely visual doorways — not animated barrier arms that lift.

---

## 5. Puzzle Games with an Animated Gate/Barrier That Opens

| Game | Description |
|---|---|
| Car Gate Puzzle Runner (itch.io) | *"Driving over a switch will open all gates of the corresponding color."* — gates open/close dynamically as part of gameplay, not as a completion sequence. |
| Euro Truck Simulator 2 mods | *"Animated gates in companies v4.8 — gates and barriers are animated, all have collision."* — barriers open for the player's truck, but as a gameplay mechanic, not a level-complete reward. |

**Mechanism:** Animated barrier arms/gates exist in simulation games, but **no sliding-block puzzle game** was found that uses a barrier-lift as a level-complete animation. The barrier-lift mechanic appears in driving sims as a functional gameplay element (open to let the player pass), not as a celebratory exit sequence.  
**Verdict:** The barrier-lift as a level-completion flourish is **novel** for the grid-puzzle genre. No established convention to follow or break.

---

## 6. General Puzzle Game Level-Complete Patterns

| Pattern | Examples | Description |
|---|---|---|
| **Last move is the exit** | Rush Hour (physical), Unblock Me | Player manually moves the final piece out. No separate animation. |
| **Per-piece auto-exit** | Parking Jam 3D, Car Out Jam | Each piece drives off automatically once reaching the exit boundary. Last piece's exit = level complete. |
| **Piece consumed by gate** | Block Slide | Block reaches a coloured gate and is instantly removed. |
| **Victory popup overlay** | Nearly all | A "Level Complete!" / star-rating overlay appears after the last exit event. |
| **Auto-solve replay** | Some match-3 games | After the player solves, the game replays the solution as a cinematic. Rare in sliding-block genre. |

**Common thread:** The sliding-block genre overwhelmingly prefers **player agency for the final move** over cinematic sequences. Parking Jam 3D's per-car auto-exit is the main exception, and even there the animation is brief (~0.5 s) — the emphasis is on the puzzle, not the spectacle.

---

## Verdict

**The proposed animation (car drives to barrier → barrier lifts → car drives off-screen) extends existing conventions rather than breaking them.** The per-car auto-exit is already standard in Parking Jam 3D; the novelty is the barrier lift (which no sliding-block puzzle uses as a completion flourish) and making the exit trigger point the *farthest* tile instead of the nearest edge. The lift gate is a fitting diegetic reward — it justifies the exit point visually and adds a moment of payoff. Keep the animation short (<1.5 s total) to match genre expectations.
