# Where to Find the Needed Sample Packs — Live Asset Store Listings & Download Flow

> Research date: 2026-08-10
> Question: **For the four free Unity Asset Store packs this Unity 6 project imports, what are the exact (still-live?) listing URLs and metadata, and how does an Asset Store download actually work in 2026 (Editor, browser, or CLI)?**
> Method: every listing below was fetched **directly from its official assetstore.unity.com page** (2026-08-10); the store's SPA renders price/size/version/compatibility server-side, so those fields read cleanly. Pack-*description* text (which the SPA does not render) is marked ⓢ when it comes from search-indexed snapshots of the same official page, and ⓡ when from a mirrored in-package ReadMe. Nothing was guessed; items that could not be confirmed are listed as **Unverified**.

---

## Per-pack verification summary

| # | Pack | Official listing URL | Publisher | Price | Size | Version / last update | Unity version | Still listed 2026? |
|---|------|----------------------|-----------|-------|------|------------------------|---------------|--------------------|
| 1 | Low Poly Cars | [assetstore.unity.com/.../low-poly-cars-101798](https://assetstore.unity.com/packages/3d/vehicles/land/low-poly-cars-101798) | Broken Vector | FREE | 2.5 MB | v1.1 · Jul 9, 2018 | 5.6.0 | **Yes — live** |
| 2 | Low Poly Houses Free Pack | [assetstore.unity.com/.../low-poly-houses-free-pack-243926](https://assetstore.unity.com/packages/3d/props/exterior/low-poly-houses-free-pack-243926) | Palmov Island | FREE | 1.5 MB | v1.0.0 · Feb 15, 2023 | 2021.3.2 | **Yes — live** |
| 3 | Yughues Free Concrete Materials | [assetstore.unity.com/.../yughues-free-concrete-materials-12951](https://assetstore.unity.com/packages/2d/textures-materials/concrete/yughues-free-concrete-materials-12951) | Nobiax / Yughues | FREE | 83.3 MB | v1.1 · Mar 28, 2025 | 2022.3.60 | **Yes — live** |
| 4 | City People FREE Samples | [assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446](https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446) | Denys Almaral | FREE | 17.2 MB | v1.4.0 · Dec 15, 2025 | 2022.3.62 | **Yes — live** |

All four are FREE, all licensed under the **Standard Unity Asset Store EULA** ([unity.com/legal/as-terms](https://unity.com/legal/as-terms), per each fetched listing), all fetched directly. None is delisted.

---

## Pack by pack

### 1. Low Poly Cars (Broken Vector) — the "LowPolyCarPack"
- **Listing (fetched directly):** https://assetstore.unity.com/packages/3d/vehicles/land/low-poly-cars-101798 — **live**, FREE, 2.5 MB, v1.1 (Jul 9, 2018), original Unity 5.6.0, 33 ratings, 2,316 favourites, publisher [Broken Vector](/publishers/12124).
- **Naming caveat:** the store title is **"Low Poly Cars"**; "LowPolyCarPack" is Broken Vector's own repo/folder name for the same content (their GitHub project and the local `Assets/BrokenVector/LowPolyCarPack` fallback clone). Content is the classic 10-vehicle set (mini, van, sports car, pickup, coupe, classic, truck ×2, bus, police) ⓢ — matches the local folder's `Models/Materials/Prefabs/Palettes` layout.
- **Companion pack (also already in the project):** the local clone additionally holds `Assets/BrokenVector/LowPolyShaders` — this is Broken Vector's separate free **"Low Poly Shaders"** pack, listing [85262](https://assetstore.unity.com/packages/vfx/shaders/low-poly-shaders-85262) (FREE, 810.1 KB, v1.0.1, Nov 27, 2017) — page content fetched via search snapshot ⓢ; URL pattern verified as the same publisher's page.
- **Publisher's own mirrors:** brokenvector.itch.io/low-poly-cars ⓢ; brokenvector.com/game-assets ("all our models available on the Unity Assetstore, itch.io and cgtrader") ⓢ.

### 2. Low Poly Houses Free Pack (Palmov Island)
- **Listing (fetched directly):** https://assetstore.unity.com/packages/3d/props/exterior/low-poly-houses-free-pack-243926 — **live**, FREE, 1.5 MB, v1.0.0 (Feb 15, 2023), original Unity 2021.3.2, Built-in/URP/HDRP all **Compatible**, 3 ratings, 988 favourites, publisher [Palmov Island](/publishers/52130).
- Local fallback folder `Assets/Palmov Island/Low Poly Houses Free Pack` (Materials/Models/Prefabs/Scenes/Textures + guideline.pdf) matches this exact pack name. Pack contents: 57 prefabs (houses, city cars, props, plants, roads) per listing snapshot ⓢ; supports-mail palmovisland@gmail.com per snapshot ⓢ. Note the publisher also sells a *paid* "Low Poly Houses Mega Pack" ([243784](https://assetstore.unity.com/packages/3d/environments/low-poly-houses-mega-pack-243784), $39.99 ⓢ) — not needed here.

### 3. "Concrete textures pack" = **Yughues Free Concrete Materials** (verified per tutorial lineage)
- **Listing (fetched directly):** https://assetstore.unity.com/packages/2d/textures-materials/concrete/yughues-free-concrete-materials-12951 — **live**, FREE, **83.3 MB**, v1.1 (Mar 28, 2025), original Unity 2022.3.60, Built-in/URP/HDRP all **Compatible**, 354 ratings, 3,520 favourites, publisher [Nobiax / Yughues](/publishers/4986). Contents: 20 concrete materials, 60 textures @1024², albedo/normal/specular ⓢ.
- **Why "Concrete textures pack" is this listing:** the original parking-jam-style tutorial line (Japanese source [miyagame.net obstacle-run tutorial, 2018 ⓢ](https://miyagame.net/obstacle-run-6/)) instructs: *"choose **Yughues Free Concrete Material** … after import the Project window has a **`Concrete textures pack`** folder … `Concrete textures pack` → `pattern 19` → `diffuse`"* — an exact structural match to the local fallback clone (`Concrete textures pack\pattern 03|07|19`, each with `diffuse.tga` + `normal.tga` + `.mat`). The local copy contains only **3 of the 20** patterns (03, 07, 19) — a subset, sized for the tutorial.
- ⚠ Size note: the current listing size (83.3 MB, v1.1, Mar 2025) is much larger than the tutorial-era package; the extra weight is from the pack's update, not from what the project needs.

### 4. City People FREE Samples (Denys Almaral)
- **Listing (fetched directly):** https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446 — **live**, FREE, **17.2 MB**, v1.4.0 (**Dec 15, 2025**), original Unity 2022.3.62, Built-in **Compatible**, URP **Compatible**, HDRP **Not compatible**, 8 ratings, 1,122 favourites, publisher [Denys Almaral](/publishers/56099).
- "Unity 6 compatible, single shared material palette, demo scene" per the in-package ReadMe ⓡ mirrored at https://gitea.dsv.su.se/ExtralityLab/DET25-Psychosis/raw/branch/main/Assets/DenysAlmaral/CityPeople-FREE/Documentation/ReadMe.md. (See sibling doc `pedestrian-people-3d-packs.md` — this pack was previously researched in depth on 2026-08-08.)

---

## How an Asset Store download works in 2026 (official docs, fetched)

**Flow (official, docs.unity.com):** the website **adds** the pack to your library; the **Editor downloads and imports it**.

1. **Claim the pack on the website.** Paid: Buy Now / Checkout. Free: **"Add to My Assets"** → "Open in Unity". No `.unitypackage` file is ever handed out in the browser. — [Purchase or download an Asset Store package](https://docs.unity.com/en-us/asset-store/downloads/purchase-asset-packages) (fetched); manual mirror: [AssetPackagesPurchase.html](https://docs.unity3d.com/6/Documentation/Manual/AssetPackagesPurchase.html).
2. **Download in the Editor:** `Window > Package Manager` → **My Assets** context → select the package → **Download** → **Import #.# to project**. Only packages acquired under your logged-in Unity ID appear. — [Manage Asset Store packages in the Editor](https://docs.unity.com/en-us/asset-store/downloads/asset-store-packages) (fetched, "Last updated 2 months ago") and [Download and import an asset package (upm-ui-import.html)](https://docs.unity3d.com/Manual/upm-ui-import.html) (fetched via search ⓢ, content corroborated).
3. **Where downloaded files land:** Windows `C:\Users\accountName\AppData\Roaming\Unity\Asset Store-5.x` (subfolders per vendor) — same doc as (2), fetched. (This is exactly how the local fallback clone at `%TEMP%\opencode\parking-jam-3dcase\Assets` originated: a copy of an imported project, not a fresh download source.)
4. **Managing ownership on the web:** My Assets page at https://assetstore.unity.com/account/assets (labels/organize/"Open in Unity") — [Manage packages in the Asset Store](https://docs.unity3d.com/Manual/AssetPackagesOrganize.html) ⓢ.
5. **Updates** to already-imported assets also happen via Package Manager (Download update → Import update) — [Update an asset package (upm-ui-update2.html)](https://docs.unity3d.com/Manual/upm-ui-update2.html) ⓢ.

**Browser vs Editor, explicitly:** Unity Support FAQ is unambiguous — "**you cannot download assets purchased from the Unity Asset Store without using the Unity Editor** … Alternative methods for asset management are not available at this time." — [support.unity.com article 30228905678612](https://support.unity.com/hc/en-us/articles/30228905678612-Can-I-download-an-asset-from-the-Unity-Asset-Store-without-using-the-Unity-Editor) (fetched via search ⓢ).

**Headless / CLI — officially:** there is **no official CLI that downloads Asset Store packages**. The only officially documented automation hook is the **Unity Hub deeplink** `https://link.unity.com/hub/package-manager/download/{asset-store-id}` — invokable "from the command line or any scripting environment that can invoke the opening of a URL" — which still opens the Hub/Editor (Package Manager window pre-targeted at the asset for download). — [Unity Hub: Deeplinking support](https://docs.unity.com/en-us/hub/deeplinking-support) ⓢ. The newly shipped [Unity CLI](https://unity.com/blog/meet-the-unity-cli) manages editors/modules/projects/auth and drives a running Editor (`com.unity.pipeline`, Unity 6.0 LTS+), but its documented surface does **not** include Asset Store package downloads ⓢ.

**Unofficial (not Unity-documented):** community tools exist, e.g. the MCP server [Armax/unitystore-mcp](https://github.com/Armax/unitystore-mcp) which logs into the store API, downloads and AES-decrypts `.unitypackage` blobs without an Editor ⓢ. Cite only as a fallback; unsupported by Unity.

**Practical takeaway for this repo:** the four packs are all still listed and free — the reproducible path is: same Unity ID on assetstore.unity.com and in the Editor → "Add to My Assets" ×4 → Package Manager → My Assets → Download → Import. The existing local copies under `%TEMP%\opencode\parking-jam-3dcase\Assets\` are a working fallback for the first three packs (BrokenVector + LowPolyShaders, Palmov Island, Yughues subset); City People is available as a single 17.2 MB editor download.

---

## Unverified / open items

- **Exact identity of the concrete pack** rests on the tutorial lineage + folder-structure match (miyagame.net snapshot ⓢ), not on a Unity-side statement that the local folder is Yughues'; no pack listing in the store is literally *named* "Concrete textures pack". Confidence: high, but not primary-source proof.
- **Pack description texts** (prefab counts, contents) for all four listings come from search-indexed snapshots ⓢ / mirrored ReadMe ⓡ; the store's own HTML does not render them.
- **Broken Vector "Low Poly Shaders" listing content** (85262) fetched only via search snapshot ⓢ.
- **Unity Hub deeplink** and **Unity CLI** pages fetched via search snapshots ⓢ, not direct fetch; both URLs are the official docs/blog domains.
- Exact rating counts/favourites fluctuate; values are as rendered at fetch time 2026-08-10.

## Sources

1. [Low Poly Cars — official listing, fetched](https://assetstore.unity.com/packages/3d/vehicles/land/low-poly-cars-101798)
2. [Low Poly Houses Free Pack — official listing, fetched](https://assetstore.unity.com/packages/3d/props/exterior/low-poly-houses-free-pack-243926)
3. [Yughues Free Concrete Materials — official listing, fetched](https://assetstore.unity.com/packages/2d/textures-materials/concrete/yughues-free-concrete-materials-12951)
4. [City People FREE Samples — official listing, fetched](https://assetstore.unity.com/packages/3d/characters/city-people-free-samples-260446)
5. [Low Poly Shaders — official listing ⓢ](https://assetstore.unity.com/packages/vfx/shaders/low-poly-shaders-85262)
6. [Purchase or download an Asset Store package — docs.unity.com, fetched](https://docs.unity.com/en-us/asset-store/downloads/purchase-asset-packages)
7. [Manage Asset Store packages in the Editor — docs.unity.com, fetched](https://docs.unity.com/en-us/asset-store/downloads/asset-store-packages)
8. [Download and import an asset package — docs.unity3d.com ⓢ](https://docs.unity3d.com/Manual/upm-ui-import.html)
9. [Manage packages in the Asset Store — docs.unity3d.com ⓢ](https://docs.unity3d.com/Manual/AssetPackagesOrganize.html)
10. [Update an asset package — docs.unity3d.com ⓢ](https://docs.unity3d.com/Manual/upm-ui-update2.html)
11. [Unity Support: "Can I download an asset … without using the Unity Editor?" ⓢ](https://support.unity.com/hc/en-us/articles/30228905678612-Can-I-download-an-asset-from-the-Unity-Asset-Store-without-using-the-Unity-Editor)
12. [Unity Hub: Deeplinking support ⓢ](https://docs.unity.com/en-us/hub/deeplinking-support)
13. [Meet the Unity CLI — unity.com blog ⓢ](https://unity.com/blog/meet-the-unity-cli)
14. [miyagame.net obstacle-run tutorial (identifies the "Concrete textures pack" folder as Yughues') ⓢ](https://miyagame.net/obstacle-run-6/)
15. [City People FREE Samples in-package ReadMe mirror ⓡ](https://gitea.dsv.su.se/ExtralityLab/DET25-Psychosis/raw/branch/main/Assets/DenysAlmaral/CityPeople-FREE/Documentation/ReadMe.md)