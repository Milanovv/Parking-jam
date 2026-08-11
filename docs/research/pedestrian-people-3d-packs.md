# Pedestrian 3D People Packs — Sourcing a Free Low-Poly Pack (Decision 6b)

> Research date: 2026-08-08
> Question: **How hard will it be to implement decision 6b — source a free low-poly 3D "people" pack from the Unity Asset Store for pedestrian obstacles?**
> Requirements recap: pedestrian obstacles slide 1 tile per tick on fixed routes — no skeletal animation or walking loops needed (a static or gently-animated humanoid is enough); visual style is low-poly flat-shaded matching "BrokenVector Low Poly Car Pack" and "Low Poly Houses Free Pack"; Unity 6 + URP; PC first, mobile later; **must permit commercial use AND modification/repainting**.

---

## Bottom line

**Difficulty: LOW — roughly half a day to a full day (≈4–8 hours) of integration work, including material conversion and repainting.**

The Asset Store currently lists several **free** low-poly people packs that are a direct fit for a grid puzzle: flat-shaded humanoids, no animations needed, tiny file sizes, all released within the last ~15 months, and all under the **Standard Unity Asset Store EULA** — which explicitly permits **commercial use and modification** (including repainting), requires **no attribution**, and only forbids redistributing the raw asset. The best candidate, **City People FREE Samples** (Denys Almaral), is a modern urban poly-art crowd with a single material palette designed exactly for the "repaint with materials" workflow we already use for cars; it is URP-listed as compatible. The only real work is the one-time import: choose pack, drop 3–6 humanoid meshes into prefabs scaled to a grid tile, convert any standard materials to URP, repaint via material skins — a bounded, low-risk task because every candidate is replaceable in minutes and the primitive-based fallback remains available as plan C. Verdict: **implement 6b; it is easy and non-blocking.**

Rough time estimate (one developer, familiar with Unity + URP material conversion):
| Step | Effort |
|------|--------|
| Download both top candidates + license review | 0.5 h |
| Import, unbroken check, prefab per pedestrian skin | 1–2 h |
| URP material conversion (per pack's material set) | 0.5–1.5 h |
| Recolour/material-palette skins + animation felt (optional idle bob) | 1–3 h |
| **Total** | **≈0.5–1 day** |

---

## Shortlist (ranked)

All facts below verified by **directly fetching each official Asset Store listing page (2026-08-08)**, except where noted. The store is a JS-heavy SPA: the **description / package content sections are rendered client-side and could not be fetched directly**; for those fields I rely on *search-indexed snapshots of the same official listing pages* (marked ⓢ) and, for City People, the in-package ReadMe mirrored in a public student repo (marked ⓡ). Prices, publisher, file size, version, Unity version, render-pipeline compatibility and licence fields come from the fetched page markdown.

| Pack | Publisher | Price | Size / file | Contents (mech count) | Style fit | License | Availability / risk | Confidence |
|------|-----------|-------|-------------|------------------------|-----------|---------|---------------------|------------|
| **City People FREE Samples** [260446](https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446) | Denys Almaral | **Free** | 17.2 MB · v1.4.0 · rel. 2025-12-15 · Unity 2022.3.62 | Fully rigged, poly-art city crowd (subset of the paid 100+ "City People Mega-Pack"); single shared material/texture palette; documentation ReadMe and demo scene; Unity 6 compatible per in-package ReadMe ⬇️ | Style fit: **excellent** — modern urban low-poly poly-art, flat palette, designed for recolouring (same workflow as the car pack) | Standard EULA (per-page) | Low risk: 8 ratings, 1,122 favourites; actively updated (v1.4.0 Dec 2025); only "handful" of characters, exact count not published on the listing | **High** (page fetched; contents corroborated by in-package ReadMe mirrored on gitea.dsv.su.se/RSS; URP "Compatible" tag) |
| **Free Pack - Lowpoly People** [325566](https://assetstore.unity.com/packages/3d/props/free-pack-lowpoly-people-325566) | PolyOne Studio | **Free** | 254.2 KB · v1.0 · rel. 2025-10-07 · Unity 2022.3.3 | 8 very low-poly people, ~300 tris each, ~2.5k tris pack total ⬇️; meshes "separated and named", "easy to modify" ⬇️ | Style: good — flat colour low-poly humanoids, generic (no modern-urban forcing) | Standard EULA (per-page) | Risk: brand-new pack, *not enough ratings* yet; no prefabs advertised — will need to build prefabs from FBX; URP tag "Compatible" | **Medium** (page fetched; descriptions marks ⬇️ only via snapshot — see "verification" note) |
| **PolyPeople Series - City People [Free]** [325204](https://assetstore.unity.com/packages/3d/characters/polypeople-series-city-people-free-325204) | Simply Poly Lab | **Free** | 413.5 KB · v1.1 · rel. 2025-09-03 · Unity 6000.0.32 (Unity 6) | 1 character only (Adult Female); 4 prefab variations; 2 material types (standard & Shader Graph); humanoid-rigged, Mixamo-compatible; **no animations** ⬇️ | Style: good (stylized modern) but **single character** — too limited for a crowd | Standard EULA (per-page) | Risk: URP is "Compatible" but Built-in/HDRP "Not compatible" on listing; Shader-Graph materials may need URP re-target; one costume only | **Low–Medium** (page fetched; description ⬇️ only) |
| **Free Low Poly Cubic Humans - 3D by Shokubutsu** [326752](https://assetstore.unity.com/packages/3d/characters/free-low-poly-cubic-humans-3d-by-shokubutsu-326752) | Shokubutsu Studios | **Free** | 742.8 KB · v1.0 · rel. 2025-08-04 · Unity 2022.3.62 | 8 characters (NPCs, knights, mages, rangers) + 6 weapons + 22 animations ⬇️ | Style: **fantasy** (knights/rangers) — poor fit for a parking-lot setting | Standard EULA (per-page) | URP "Compatible"; but characters are armoured fantasy types | **Medium** (page fetched; description ⬇️ only) |
| **Easy Primitive People** [161846](https://assetstore.unity.com/packages/3d/characters/easy-primitive-people-161846) | Bit Gamey | **Free** | 567.6 KB · v1.95 · rel. 2021-10-11 · Unity 2018.4 | ~20 capsule-primitive modular characters (cop, doctor, robber, Santa, …) with separate primitive accessories; prefabs included per description ⬇️ | Style: capsule-primitive minions — closest to a "build from primitives" aesthetic, but visibly plainer than hand-modelled poly-art | Standard EULA (per-page) | Risk: old pack (2021), matches Unity 2018.4, no recent updates; TextMeshPro dependency for some shirts ⬇️ | **Medium** (verifiable via page fetch + description ⬇️) |
| Low Poly Medieval Peasants - Free [122225](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/low-poly-medieval-peasants-free-lowpoly-medieval-fantasy-series-122225) | Polytope Studio | **Free** | 56.3 MB · v4.2.1 · rel. 2025-01-03 · Unity 2021.3.40 | 7 characters, Mechanica & Mixamo compatible, one 256×256 texture, ~2,882–4,162 tris each ⬇️ | Style: **medieval** — clean low-poly, but historical fantasy theme clash | Standard EULA (per-page) | Low technical risk (42 ratings, 2,787 favourites — the most established free pack), wrong theme | **Medium** (page fetched; description ⬇️) |

_Verification note: pack description text (character counts, "prefab included", rigging) is NOT readable on the official listing page via the store's own HTML — descriptions ship with the Unity client. I could not fetch a rendered copy of sections 3–7 on assetstore.unity.com; the package-contents facts above are sourced from search-engine snapshots of the same page (crawler-rendered) or the in-package ReadMe mirrored on a third-party repo (City People), and are marked ⬇️. Every page's importable fact (currency, size, price, version, compatibility row, EULA link, publisher) was read directly from the fetched page.

**Also evaluated and rejected**: the famous **"Low Poly Animated People"** by PolyPerfect looks ideal (100+ rigged low-poly characters) but is **paid ($30)** — decision 6b is strictly free-packs; the **"Lowpoly People Collection"** (138 characters) by the same PolyOne Studio that made our #2 is **paid (€14.54)** — we checked its page directly; the CC-BY-SA "yancharkin Low Poly People Pack" is **not on the Asset Store** (itch.io, name-your-own-price), carries share-alike obligations, and falls outside decision 6b scope (noting it as a fallback-side option only).

---

## License — applicable to FREE assets on the Unity Asset Store

All five free packs above show **"License agreement: Standard Unity Asset Store EULA"** on their official listing pages (verified directly 2026-08-08). The governing document is the **Asset Store Terms of Service and <EULA>** at https://unity.com/legal/as-terms — "Last updated: December 4, 2024" (quotes below verbatim from that page). No candidate declares a custom per-pack license (no CC-BY or attribute-required license found in any listing).

### (a) Commercial use — allowed for free assets

> **§2.2.1 (EULA, Appendix 1)** – "Subject to the restrictions set forth in this EULA, Licensor hereby grants to the END-USER a non-exclusive, non-transferable, worldwide, and perpetual license to the Asset solely: (a) to incorporate the Asset, together with substantial, original content not obtained through the Unity Asset Store, into an electronic application or digital media that has a purpose, features, and functions beyond the display, performance, distribution, or use of Assets ("Licensed Product") as an embedded component of that Licensed Product, such that the Asset does not comprise a substantial portion of the Licensed Product; (b) to reproduce, publicly display, publicly perform, transmit, and distribute the Asset as incorporated and embedded in that Licensed Product; ... (d) monetize the Asset within and for use within a Licensed Product, including via in-app purchases;"

This clause is identical for paid and **free** assets — the EULA does not distinguish "free" vs "paid" for usage rights (price only affects the indemnification clause, see below). Commercial games, selling the game, and in-app purchases are all explicitly covered.

### (b) Modifications / derivative works — allowed
§2.2.1(e) grants the right to "**except as set forth in 2.2.1.1 below, modify the Assets** in connection with..." (a)…(d).
And independently, §6 (Reverse Engineering, Decompilation, and Disassembly) states: "**END-USER may modify Assets.** END-USER shall not reverse engineer, decompile, or disassemble Services SDKs…" — so repainting, re-materialing, and reskinning the poly people meshes ("paint skins" workflow) is licit under the EULA, for free and paid assets alike.

### (c) Cannot redistribute the asset as‑is / hidden‑resale restriction (§2.2.1.1)
§2.2.1.1 (EULA) — "Limitations on License": the END-USER may **not**: "(b) enable a customer or user of a Licensed Product to sell, transfer, distribute, lease, or lend the Assets ... for commercial gain ...; (d) **use, reproduce, duplicate, publicly display, publicly perform, copy, modify, adapt, translate, prepare derivatives of, distribute, transfer, license, sublicense, rent, lease, lend, sell, trade, resell, or otherwise commercialize or monetize any Asset except as expressly permitted in this EULA**;"
Also Terms §3.5: "...except as set forth in the EULA or a Provider end user license agreement, you agree that you will not ... distribute, transfer, license, sublicense, rent, lease, lend, sell, trade, resell, or otherwise commercialize or monetize any Asset ...".

Practical reading: shipping the raw or modified mesh files *as an unpackable/bundle asset* to end-users is prohibited; shipping them embedded in your game binary (as part of the Licensed Product) is exactly what §2.2.1(b) allows.

### (d) Attribution — none required (default)
The Standard EULA **contains no attribution requirement** for non-restricted assets, and none of the five candidates declares their own license text on the listing page, so **no credits are required**. (For contrast: the itch.io "yancharkin Low Poly People Pack" is CC-BY-SA 4.0 and the itch "CharCrafter" sample requires attribution — neither is the Asset Store path.)

### Indemnity caveat for free assets (important, not a blocker)
EULA §11.3.2 limits who gets defense/indemnity to assets "…licensed for any obligatory fee, charge, or price ("**Paid Assets**"). Free assets carry **no copyright indemnity** from licensor. For a small indie title this is standard practice across the store; residual IP risk is still near-zero for low-poly generic humans of the same genre.

---

## Recommendation section

**Pick "City People FREE Samples" as the primary source** and keep the next two rows of the table as backups:

1. **City People FREE Samples (Denys Almaral)** — the right art direction (modern urban, poly-art, flat-shaded), single shared material palette that matches our paint-skin workflow from the BrokenVector car pack, URP "Compatible" tag, actively updated (Dec 2025), rigged meshes we can simply de-contextualize (no animation needed — we add a subtle idle bob/tilt ourselves). Rating risk (8 ratings) is minor because the fallbacks are one click away. 
2. **Free Pack - Lowpoly People (PolyOne Studio)** — ultra-light (254 KB), true flat-colour low-poly generic humans; good spare if we want even simpler disguises; but no prefabs promised and zero store ratings.
3. **PolyPeople Series - City People [Free]** — keep as a *last* resort (single character only).

**The "if all else fails" path (plan C, fall back to primitives)** is realistic and cheap, and costs more time, not difficulty:
- **Effort**: 1–3 days (Blender: ~1–2 h per humanoid base from cubes/capsules; then texture-less flat materials + palette skins, mirrors the car-pack look). Prefabs/instantiation identical for both paths.
- **Look**: a capsule/cube humanoid with the same palette *can* match the house/car packs — but hand-authored low-poly people almost always read "fuller" (proper proportions, layered clothes silhouette, two-colour pupils) than primitive compositions. For a polished commercial release the pack path has a visibly better ceiling, at a *tenth* of the modelling time.
- **Zero risk** of a store TOS/nightmare; zero download size.

Verdict: go with **City People FREE Samples**, invest ~1 h of the reserved integration time in a two-pack off-test (City People vs Free Pack - Lowpoly People) during the first URP conversion, and only if both disappoint visually in the scene do we fall back to primitives (which double the time budget to ~2–4 days).

---

## Sources

**Primary**
1. [Asset Store Terms of Service and EULA — unity.com/legal (last updated Dec 4, 2024)](https://unity.com/legal/as-terms) — *directly fetched 2026-08-08; clauses quoted verbatim in the License section.*
2. [City People FREE Samples — official listing](https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446) — *directly fetched; price Free, 17.2 MB, v1.4.0, Dec 15 2025, URP Compatible, Standard EULA.*
3. [Free Pack - Lowpoly People — official listing](https://assetstore.unity.com/packages/3d/props/free-pack-lowpoly-people-325566) — *directly fetched; price Free, 254.2 KB, v1.0, Oct 7 2025.*
4. [PolyPeople Series — City People [Free] — official listing](https://assetstore.unity.com/packages/3d/characters/polypeople-series-city-people-free-325204) — *directly fetched; price Free, 413.5 KB.*
5. [Free Low Poly Cubic Humans by Shokubutsu — official listing](https://assetstore.unity.com/packages/3d/characters/free-low-poly-cubic-humans-3d-by-shokubutsu-326752) — *directly fetched; price Free, 742.8 KB.*
6. [Easy Primitive People — official listing](https://assetstore.unity.com/packages/3d/characters/easy-primitive-people-161846) — *directly fetched; price Free, 567.6 KB.*
7. [Low Poly Medieval Peasants - Free — official listing](https://assetstore.unity.com/packages/3d/characters/humanoids/humans/low-poly-medieval-peasants-free-lowpoly-medieval-fantasy-series-122225) — *directly fetched; 56.3 MB, free.*

**Secondary (verified against primary where possible; used only for pack – description text that the SPA does not render)**
- [Lowpoly People Collection — official listing (paid, €14.85) — checked to confirm the free tet-promo split](https://assetstore.unity.com/packages/3d/props/lowpoly-people-collection-304665)
- [City People FREE Samples — in-package ReadMe mirrored at gitea.dsv.su.se (student project assets copy)](https://gitea.dsv.su.se/ExtralityLab/DET25-Psychosis/raw/branch/main/Assets/DenysAlmaral/CityPeople-FREE/Documentation/ReadMe.md) — used for "Unity 6 compatible, single and shared material palette, demo scene" claims of the pack.
- [PolyPeople City People search-snapshot of the official listing](https://assetstore.unity.com/packages/3d/characters/polypeople-series-city-people-free-325204) — usage snapshot of page 325204 (description: 1 adult female, 4 prefab variations, Shader Graph materials, no animations).
- [Free Pack - Lowpoly People search-snapshot (same official page ID)] and the publisher's [CGTrader mirror of the same content](https://www.cgtrader.com/free-3d-models/character/other/free-pack-lowpoly-people) — corroborates "8 models, ~2,5k tris total, ~300 tris average".
- [Denys Almaral, publisher website — City People testimonials page](https://denysalmaral.com/) — corroborates pack scope ("trial set of poly-art city characters"), used only as a support page note.
- [itch.io yancharkin Low Poly People Pack — CC BY-SA 4.0 non-Store alternative, rejected](https://yancharkin.itch.io/low-poly-people-pack)

**Not verified / open items** (stated honestly, per the brief): exact mesh counts of City People FREE Samples ("a handful of fully rigged characters") and the promise that "Free Pack - Lowpoly People" ships prefabs (page description claims meshes only); Shokubutsu animation set (22 animations) is from store-snapshot only; those fields could not be read through the store SPA.