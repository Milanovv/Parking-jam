# Third-Party Notices

Third-party assets incorporated into Parking Jam, their source, license, and audit date (per
ADR-0012 and ticket #3 — License gate).

Assets are always sourced from their official Unity Asset Store listing where available; the
read-only clone of the reference repo (`berkeerdem1/Parking_Jam_3DCase` @ `6e715b6`) is used only
as the fallback for packs that cannot be re-downloaded headlessly. Materials, prefabs and code from
the reference repo are never copied — pack models are re-composed with project-owned components and
recreated materials. Raw pack folders under `Assets/` are the licensed import record; the game
consumes curated copies under `Assets/_Project/Packs/` only.

| Asset | Source | License | Audited |
|---|---|---|---|
| LowPolyCarPack (BrokenVector) — 10 vehicle FBX models + 6 palette textures | Local clone fallback, `Assets/BrokenVector/LowPolyCarPack` (demo scenes, example assets and unlit materials pruned); Asset Store re-download pending a manual Editor login step | Unity Asset Store EULA (free asset) | 2026-08-11 |
| LowPolyCarPack palette (6 paint colours Blue/Green/Purple/Red/Silver/Yellow) | Pack materials (`Materials/PBR`) read for value reference; URP paints recreated in-project | Unity Asset Store EULA (free asset) — values only, no asset files retained | 2026-08-11 |
| City/houses pack (Palmov Island "Low Poly Houses Free Pack") — 49 FBX models (houses, street furniture, fences, lamps, trees, plants, grounds, roads) | Local clone fallback, `Assets/Palmov Island/Low Poly Houses Free Pack`; Asset Store re-download pending a manual Editor login step; demo scene and ferris wheel pruned per D11 | Unity Asset Store EULA (free asset) | 2026-08-11 |
| Concrete textures pack (Yughues Free Concrete Materials) — `Assets/YughuesFreeConcreteMaterials`, 60 TGA textures kept as import record; 2 patterns converted to PNG in-project (`pattern03` + `pattern07` under `Assets/_Project/Packs/ConcreteTextures`) | Local clone fallback (preview scene pruned); Asset Store re-download pending a manual Editor login step | Unity Asset Store EULA (free asset) | 2026-08-11 |
| People pack ("City People FREE Samples", Denys Almaral) — `Assets/DenysAlmaral/CityPeople`, 8 character FBX models (city/downtown/elder/little_kids/professions) + shared palette texture `people_pal.png` | Asset Store listing 260446 (v1.4.0, imported via Editor Package Manager); construction ×2, disabilities (animated variant), PROPS, animations, demo scenes, scripts and the CityPeople component pruned per ticket #9 (no rig/animation, single-material palette, `people_pal.mat` kept as palette record) | Unity Asset Store EULA (free asset); palette material recreated in-project (URP) | 2026-08-11 |

Everything else in this repository is project-authored.