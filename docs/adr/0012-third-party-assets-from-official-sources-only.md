# ADR-0012: Third-party assets come from official sources only — reference repos are not a source

**Status:** Accepted
**Date:** 2026-08-09

**Context:** The reference repo `berkeerdem1/Parking_Jam_3DCase` ships no license (GitHub API `license: null`; `git ls-tree -r HEAD` finds no LICENSE/COPYING/NOTICE; README makes no licensing statement). Its own content — scripts, scenes, prefab wiring, level layouts, the confetti prefab — is therefore all-rights-reserved by default. It does, however, vendor free third-party packs (BrokenVector LowPolyCarPack, Palmov Island houses, concrete textures, audio of unknown provenance), and its README states these come from the Unity Asset Store and are free.

**Decision:** Treat the repository as **reference material only**: never copy its repo-authored files into our project. Re-import the same free packs by re-downloading them from their official Unity Asset Store listings, where the standard Asset Store EULA applies (commercial use permitted, modification permitted, no redistribution of the raw asset, no attribution required; no IP indemnity for free assets — unity.com/legal/as-terms §2.2.1, §11.3.2). The repo clone is a fallback only for packs that have been delisted. Content with unverifiable provenance (the repo's audio) is not used at all; SFX come from Mixkit/CC0 per ADR-0008.

**Rationale:**
- Legally unambiguous chain of title, at zero cost — the packs are free on the Asset Store anyway.
- The standard Asset Store EULA covers exactly what we need (modify meshes, recolor, publish commercially).
- Keeps `ThirdPartyNotices.md` truthful: every entry maps to a source and license we actually hold.

**Considered Options:**
- Copying the free packs out of the repo clone — convenient, but routes the chain of title through an unlicensed repo and risks unverifiable modifications.

**Consequences:**
- No repo-authored file (script, scene, prefab, confetti) ever reaches `Assets/`; such content is design reference for our own implementations.
- Each imported pack gets its EULA audited at import (T1 in the incorporation plan) and recorded in `THIRD_PARTY_NOTICES.md` (repo root).

**Source:** `docs/research/parking-jam-3dcase-incorporation.md` (§2.4, §5.1); Unity Asset Store terms (unity.com/legal/as-terms).