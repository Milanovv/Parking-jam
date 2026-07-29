# Barrier-First Gameplay Flow: Research Findings

Research conducted July 2026. Sources: primary docs, official store pages, wiki entries,
gameplay videos, developer articles, and academic game design papers.

---

## 1. Sliding-Block Puzzles with Locked Exits / Gate Mechanics

No existing sliding-block puzzle implements a **removable barrier that the player must
unlock before any piece can exit**. The closest analogs found:

### Slinger Block (61673 Games)
| Property | Finding |
|----------|---------|
| **Exit** | Golden exit door, locked by default |
| **Barrier** | The door itself is locked. Player must collect **all yellow keys** on the grid to unlock it |
| **Flow** | Lock exists from start → player navigates to collect keys → keys consumed → door unlocks → slide into exit |
| **Motivation** | Clear visual goal + clear requirement. Creates "collect-quest" urgency |
| **Visual** | Golden door icon + yellow key icons visible on board |
| **Resets?** | No, stays open once all keys collected |
| **Source** | 61673.com/games/puzzle/slinger-block.html |

### Free the Key (App Store)
| Property | Finding |
|----------|---------|
| **Exit** | Right edge of grid |
| **Barrier** | No removable gate. The **golden key block** is itself the piece that must slide to the exit |
| **Flow** | Classic Rush Hour: clear path, slide key out |
| **Source** | apps.apple.com/tm/app/free-the-key-unblock-puzzle |

### Color Block Jam / Clear Block Puzzle
| Property | Finding |
|----------|---------|
| **Exit** | Internal color-coded door tiles on the grid |
| **Barrier** | Each door only accepts its matching color. Acts as both destination and gate |
| **Flow** | Slide each colored block to its matching door. Block is consumed on arrival |
| **Motivation** | Color-matching satisfaction; path-order planning |
| **Source** | arrowsgo.org/color-block-jam, play.google.com (Clear Block Puzzle) |

### Kinetic Puzzle (br0wer.com)
| Property | Finding |
|----------|---------|
| **Exit** | Orange edge portals |
| **Barrier** | "Blue dependent" blocks are **locked until their prerequisite block leaves the board** — closest to a dependency-based gate |
| **Flow** | Remove prerequisite → dependent block unlocks → now can be slid to portal |
| **Source** | br0wer.com/kinetic-puzzle |

### Summary for sliding-block genre
**No game surveyed uses an interruptive mini-game to remove a barrier at the exit.** The
dependency gates in Kinetic Puzzle are mechanical (prerequisite must leave first), not
interactive. Slinger Block's key-collection is the closest to "do something before exit
opens," but keys are spatial pickups, not a separate mini-game.

---

## 2. Escape Room / Lock-and-Key Puzzle Games

### Core design pattern (across all sources)
Escape rooms are built on a **puzzle DAG** (Directed Acyclic Graph):

```
[Observe] → [Hypothesize] → [Manipulate] → [Unlock] → [Recontextualize]
```

Every puzzle ends by opening a "box" or "door" that yields the clue/key for the next
puzzle. The **exit door is the final lock** in the chain.

### Key design rules (from Solana Garden, Case Closed Edinburgh, the Codex)

**"Problem-First" beats "Solution-First"** (Case Closed Edinburgh, 2025):
> "It's more interesting to find a lock and know you need a code, than it is to find a
> code and start hunting for a lock."

This directly supports **barrier-first design**: show the locked exit FIRST, then let the
player figure out how to open it.

**Soft gates vs Hard locks** (Solana Garden, 2026):
- **Soft gate**: exit blocked until player solves any 2 of 3 side puzzles — allows pivot.
- **Hard lock**: requires a specific key/item — riskier, needs parallel lanes.
- For mobile puzzle games, a **hard lock** (single barrier at exit) is acceptable because
  the scope is small and levels are short.

**Player motivation** (all sources):
- The locked exit creates **curiosity-driven desire** and a **clear objective**.
- Risk: if the lock is too hard, players stall on a single point. Mitigation: make the
  unlocking mini-game easy/free to retry.
- **Optimal frustration**: "just frustrated enough that you feel the big win" (Sherlocked).

**Key flow in escape rooms**: the exit is ALWAYS the last thing. Players see it early
(visible but denied), work through puzzles, then open it at the end.
- Sources: solana.garden/guides/game-escape-room-design-explained,
  caseclosededinburgh.co.uk/blog/problem-first-game-design,
  thecodex.ca/13-rules-for-escape-room-puzzle-design,
  lockpaperscissors.co/escape-room-design-blueprint

---

## 3. Candy Crush / Match-3 with Locked Exits

### Liquorice Lock (Candy Crush Saga)
| Property | Finding |
|----------|---------|
| **What it locks** | Individual candies (cannot be moved, matched, or removed while locked) |
| **How to unlock** | Match the candy underneath, or hit with special candy explosion |
| **Flow** | Lock visible from start → player must match the trapped candy → candy is released (not removed) → can now be used |
| **Motivation** | Creates immediate sub-goal: "free that candy so I can use it" |
| **Visual** | Licorice cage with X-formation over the candy |
| **Resets?** | No, stays unlocked once freed |
| **Source** | candycrush.fandom.com/wiki/Liquorice_Lock |

### Order Lock (Candy Crush Saga, level 7896+)
| Property | Finding |
|----------|---------|
| **What it locks** | Groups of candies trapped together |
| **How to unlock** | Collect specific candies (e.g., "6 Striped Candies") shown on the lock |
| **Flow** | Lock visible → player must create specific special candies → order counter decrements → at 0, lock breaks → trapped candies freed |
| **Motivation** | Creates clear collect-quest with progress bar (the counter) |
| **Visual** | Laces binding candies together with order icon on top |
| **Resets?** | No |
| **Source** | candycrush.zendesk.com (Order Locks article) |

### Ingredient levels with locked exits (Level 8090, 1917)
- Cherries/ingredients are **locked behind Order Locks**.
- Player must clear blockers/waffles to unlock the first order lock.
- Once unlocked, cherries drop to bottom exits.
- Level 1917: cherries in locked cells, keys must be collected to unlock them.
- Flow: **clear blockers → unlock cherries → drop to exit**.

### Summary for match-3
Candy Crush uses locks as **progressive denial** (lock individual items, not the exit
itself). The "unlock ingredients first, THEN drop them to exit" pattern at levels 8090
and 1917 is the closest match-3 equivalent to a barrier-first flow, but the lock is on
the items, not on the exit.

---

## 4. "Barrier-First" / "Spatial Denial" Design Pattern

### Academic foundation
**SMU Guildhall thesis — Ziyi Hua (2024)**: "Maintaining Player Motivation Using Denial
and Reward"

Defines **Spatial Denial** as:
> "The design technique of presenting a landmark to the player while intentionally
> obstructing the direct path toward it. This 'gatekeeping' creates a psychological
> challenge, stimulating curiosity and establishing a clear gameplay objective."

**Push-and-Pull model**:
- **Pull factors**: visible goals that draw player forward (the locked exit is a pull)
- **Push factors**: obstacles that redirect (the barrier itself is a push)

The locked exit acts as **both pull and push** simultaneously — it pulls (you can see
where you need to go) and pushes (you cannot go there yet, so engage with the puzzle).

Source: scholar.smu.edu (Guildhall level design thesis)

### Game design literature parallels

**Problem-First Game Design** (Case Closed Edinburgh, 2025):
- Show the problem (locked exit) BEFORE the solution method.
- Player knows the objective: "open that gate."
- Contrast with Solution-First: fumbling with items without knowing why.
- This is the central argument for barrier-first design.

**TV Tropes: Toggling Setpiece Puzzle**:
- Levers/switches toggle gates, walls, platforms.
- Player sees the blocked path, finds the switch, toggles it to proceed.
- Donkey Kong '94, Mega Man 8, Baba Is You, Mario Maker 2 all use this.
- Source: tvtropes.org (Toggling Setpiece Puzzle)

### Pros of barrier-first design
| Pro | Detail |
|-----|--------|
| **Clear objective** | "Open the gate" is instantly understandable |
| **Curiosity** | Visible-but-inaccessible exit creates "I want to get there" drive |
| **Satisfaction** | Gate opening animation is a visible reward |
| **Pacing control** | Designer controls when player can exit (cannot speed-run past barrier) |
| **Tutorial-friendly** | Can be the first thing player sees, teaches "interact to unlock" |

### Cons / risks
| Con | Detail |
|-----|--------|
| **Stalling** | If mini-game is too hard, player is stuck at the very start |
| **False affordance** | If barrier looks interactive but isn't (or vice versa), player gets confused |
| **Reversal of genre expectation** | Sliding-block players expect "clear path → exit." Adding a mini-game step may feel like friction |
| **Diminished urgency** | If barrier is trivial to open, it feels like busywork. If too hard, frustration |
| **Cognitive load** | Two-phase flow (unlock → clear) is more complex than pure "clear path" |

---

## 5. Parking Jam 3D (Popcore) — Level Flow Analysis

### Core gameplay (from gameplay videos, app store pages, guides)
- 50M+ installs. Classic sliding-block: swipe cars along their axis to clear them off the
  grid edge.
- **No barrier/mini-game mechanic** in the base level flow.
- Obstacles: static barriers (cones, walls), moving pedestrians (granny, policeman).
- These are **permanent obstacles** — they cannot be removed. Navigate around them.

### Challenge modes and special mechanics
| Mechanic | Detail |
|----------|--------|
| **"Cordoned off burst pipe" challenge** | Player reports a cordon blocking an exit. 1 car always left that cannot exit without hitting the cordon. Suggests a special challenge where a cordon/barrier blocks a specific path |
| **Golden car** | Triggers a pop-up overlay when moved. Players report it as annoying — interrupts flow |
| **Boss levels** | More cars, tighter layouts. Still no removable exit barrier |
| **Obstacles** | Barriers are static/indestructible. No mini-game to remove them |
| **Exit** | Edge-based, no gate. Cars simply drive off the edge |

### Key finding
**Parking Jam 3D does NOT use a barrier-first flow.** Cars are cleared in sequence by
sliding, and the exit is always open. The "cordoned off burst pipe" challenge is the
closest, but it is a restraint (don't hit the cordon) rather than a removable barrier.

Sources: chaptercheats.com (Q&A), gamezebo.com (strategy guide),
apps.apple.com/us/app/parking-jam-3d, omnigames.blog/popcore-parking-jam,
iofreeonline.com/IOS/game/Parking-Jam-3D

---

## 6. Games Where Win Condition Changes After Interaction

### Wheely 8 (kickoutgames.com)
| Property | Finding |
|----------|---------|
| **Genre** | Point-and-click Rube-Goldberg puzzle |
| **Flow** | Player clicks buttons/levers in correct sequence → obstacles clear → player clicks Wheely → Wheely drives to exit |
| **Barrier** | Multiple barriers (bridges, platforms, traps). Each cleared by specific interaction |
| **Win condition change** | Before clicking Wheely: goal is to clear path. After: goal is to reach exit. Two-phase: "setup then execute" |
| **Visual** | Red flag marks exit. Wheely stays stationary until clicked |
| **Resets?** | Level resets on mistake. Barriers return to original state |
| **Source** | kickoutgames.com/game/wheely-8 |

### Hero's Trail (GameMaker tutorial)
| Property | Finding |
|----------|---------|
| **Flow** | Locked chest (needs key) + gate (needs lever pull). Gate is visible but blocked. Player must find lever, pull it, gate opens animation plays, gate destroyed |
| **Key design** | Gate and lever are named instances. Lever stores gate ID to open. Gate animation plays once, then self-destructs |
| **Source** | gamemaker.io/en/tutorials/heros-trail-dnd-3 |

### Donkey Kong '94 / Mario Maker 2 (ON/OFF Switch)
- Levers toggle walls, bridges, conveyor belts between two states.
- Exit door is locked by default; player must find key and open it.
- In Mario Maker 2: ON/OFF Switches toggle all blocks of matching color.
- Source: tvtropes.org (Toggling Setpiece Puzzle)

### Slinger Block (see section 1)
- Exit door locked by default. Collecting ALL yellow keys unlocks it.
- Classic "change win condition" flow: start → collect keys → door unlocked → now exit.

### Pattern summary
All these games share a **two-phase structure**:

```
Phase 1: "Activate/unlock the exit" (pull lever, collect keys, solve mini-game)
Phase 2: "Reach the exit" (navigate the now-open path)
```

The barrier is the **signal that Phase 1 exists**. Once phase 1 is complete, the game
transitions cleanly to phase 2.

---

## Final Summary

| Game | Barrier-first? | Removable barrier? | Barrier type | Unlock method |
|------|:---:|:---:|---|---|
| **Parking Jam 3D (Popcore)** | No | No | Static obstacles only | N/A (exit always open) |
| **Rush Hour / Unblock Me** | No | No | None | N/A (exit always open) |
| **Slinger Block** | Yes | Yes (door) | Locked door | Collect yellow keys |
| **Kinetic Puzzle** | Partial | Yes (dependency) | Blue-dependent blocks | Prerequisite block exits |
| **Color Block Jam** | No | No | Color-matched doors | Slide block to matching door |
| **Candy Crush Liquorice Lock** | No | Yes | Lock on individual candy | Match candy underneath |
| **Candy Crush Order Lock** | Yes (ingredients) | Yes | Lock on ingredient group | Fulfill order (striped candies etc.) |
| **Wheely 8** | Yes | Yes | Multiple obstacles/barriers | Click levers/buttons in sequence |
| **Escape rooms (general)** | Yes | Yes | Exit door locked | Puzzle chain → final key/code |

---

## Recommendation

**The "unlock barrier first, then free cars" flow works** — but it is an innovation for
the sliding-block genre. No existing sliding-block puzzle game uses this pattern, which
means it carries discovery risk. However, the pattern is validated in adjacent genres:
escape rooms (Problem-First design), action-puzzle games (Wheely 8), and even Candy
Crush's ingredient-unlock levels. The academic Spatial Denial framework directly
supports it: show the goal, deny access, reward unlocking.

The key design constraints for success are:
1. The barrier must be **immediately visible and obviously interactive** (boom gate
   visual at the exit tile).
2. The unlocking mini-game must be **quick and free-retry** (no hard stall at level start).
3. The barrier should **stay open permanently** once unlocked (as in the current spec).
4. The first few levels should make the barrier trivial to open (tutorial) so the player
   learns the pattern before harder levels gate progress behind harder mini-games.
