# Plan Validation — Parking Jam

Each decision below is validated against primary sources with a verdict and risk flag.

---

## 1. Unity 6 + URP for a 2D puzzle game

**Verdict:✅ Confirmed good. No critical risk.**

| Claim | Source | Status |
|-------|--------|--------|
| Built-in RP is deprecated in Unity 6.5; URP is the recommended default | [Unity Discussions — RP strategy for 2026](https://discussions.unity.com/t/render-pipelines-strategy-for-2026/1710004) | Confirmed |
| URP has a dedicated 2D Renderer asset with sprite lighting, sorting layers, sprite masks | [Unity Manual — Set up 2D Renderer in URP](https://docs.unity.cn/6000.2/Documentation/Manual/urp/Setup.html) | Confirmed |
| Unity 6 ships 2D sample projects (Happy Harvest, Gem Hunter Match) built with URP | [Unity Blog — Unity 6 graphics learning resources](https://unity.com/blog/unity-6-graphics-learning-resources) | Confirmed |
| URP 2D Renderer supports 2D lights, shadow, and special effects | [Unity Blog — Unity 6 graphics learning resources](https://unity.com/blog/unity-6-graphics-learning-resources) | Confirmed |

**Risk:** None for this game. URP is the recommended pipeline and the 2D Renderer is purpose-built for 2D games.

---

## 2. Input System package (abstracted for touch later)

**Verdict:✅ Confirmed good. No critical risk.**

| Claim | Source | Status |
|-------|--------|--------|
| Input System supports both mouse and touch via the `Pointer` abstraction | [Input System — Touch support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Touch.html) | Confirmed |
| Touch simulation from mouse is built-in via `TouchSimulation.Enable()` | [Input System — Touch simulation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Touch.html#touch-simulation) | Confirmed |
| Built-in `TapInteraction` and `HoldInteraction` work on any pointer device | [Input System — Interactions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/manual/Interactions.html#tap) | Confirmed |
| `InputSystemUIInputModule` handles unified pointer input across mouse/touch | [Input System — UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.16/manual/UISupport.html) | Confirmed |

**Risk:** The spec currently uses `Input.GetMouseButton()` (Input Manager, legacy). It should migrate to the Input System package for proper abstraction. Easy fix, not a blocker.

---

## 3. GameManager + OccupancyMap + thin views + enum state + Memento undo

**Verdict:✅ Confirmed good. No critical risk.**

| Claim | Source | Status |
|-------|--------|--------|
| "Get as much code as possible out of MonoBehaviours. Separate logic from presentation." | [Unity — How to architect code as your project scales](https://unity.com/how-to/how-architect-code-your-project-scales) | Confirmed |
| "For any tile-based game, do all logical comparisons in your own data storage." | [Unity Discussions — Optimal grid representation](https://discussions.unity.com/t/what-s-the-optimal-way-to-represent-a-9x9-block-puzzle-grid-in-unity-for-solver-logic/1642826) | Confirmed |
| Enum + switch is recommended over abstract FSM for simple games | [Unity Discussions — Simple FSM](https://discussions.unity.com/t/a-right-way-to-do-a-gamemanager-with-fsm-in-unity-6/1581169) | Confirmed |
| Memento pattern (snapshot undo) is the simplest undo approach for small grids | [Unity Discussions — Undo/redo approach](https://discussions.unity.com/t/approach-to-creating-an-undo-redo-system/946942) | Confirmed |

**Risk:** None. Lightweight separation is the recommended architecture for small games.

---

## 4. JSON in StreamingAssets for level data

**Verdict:✅ Confirmed good. Minor platform caveat.**

| Claim | Source | Status |
|-------|--------|--------|
| StreamingAssets works on all target platforms (Windows PC) | [Unity Manual — StreamingAssets](https://docs.unity3d.com/Manual/StreamingAssets.html) | Confirmed |
| JsonUtility is the correct deserializer; supports `[Serializable]` POCOs | [Unity Manual — JSON Serialization](https://docs.unity3d.com/Manual/json-serialization.html) | Confirmed |
| No hard size limits on StreamingAssets files | [Unity Manual — StreamingAssets](https://docs.unity3d.com/Manual/StreamingAssets.html) | Confirmed |
| On Android/WebGL, must use `UnityWebRequest` instead of `File.ReadAllText` | [Unity Manual — StreamingAssets](https://docs.unity3d.com/Manual/StreamingAssets.html) | Confirmed |

**Risk:** The spec loads levels only on PC (itch.io release), so `File.ReadAllText` is fine. If Android support is added later, switch to `UnityWebRequest`. Not a current risk.

---

## 5. 2D sprites in URP

**Verdict:✅ Confirmed good. No critical risk.**

| Claim | Source | Status |
|-------|--------|--------|
| URP 2D Renderer supports sprite lighting, sorting layers, and sprite masks | [Unity Manual — Renderer 2D asset reference for URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/2DRendererData-overview.html) | Confirmed |
| Unity's official 2D sample projects use URP (Happy Harvest, Gem Hunter Match) | [Unity Blog — Unity 6 graphics learning resources](https://unity.com/blog/unity-6-graphics-learning-resources) | Confirmed |
| URP 2D Renderer handles sprite rendering with the 2D Renderer Data asset | [Unity Manual — Set up 2D Renderer in URP](https://docs.unity.cn/6000.2/Documentation/Manual/urp/Setup.html) | Confirmed |

**Risk:** None. Note spec uses 3D models with perspective camera (2.5D approach) — this uses the **URP 3D Renderer**, not the 2D Renderer. The 2D Renderer is for true sprite-based 2D. The spec's 2.5D approach is fine with URP 3D Renderer; just be aware it's not using URP's 2D Renderer features.

---

## 6. uGUI for UI

**Verdict:✅ Confirmed good. No deprecation risk.**

| Claim | Source | Status |
|-------|--------|--------|
| uGUI is the "Recommended" runtime UI system in Unity 6 | [Unity Manual — UI system comparison (Unity 6)](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) | Confirmed |
| UI Toolkit is marked "Alternative" for runtime; uGUI is NOT deprecated | [Unity Manual — UI system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) | Confirmed |
| UI Toolkit lacks Animation Clip / Timeline integration (uGUI has both) | [Unity Manual — UI system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) | Confirmed |
| UI Toolkit lacks serialized events (uGUI has them) | [Unity Manual — UI system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) | Confirmed |

**Risk:** None. uGUI is the correct choice for this game. It is supported and recommended for runtime game UI.

---

## 7. Free asset packs from itch.io / Unity Asset Store

**Verdict:⚠️ Low risk. Must verify per-asset license.**

| Claim | Source | Status |
|-------|--------|--------|
| Asset Store standard EULA allows use in commercial games when embedded into a "Licensed Product" with substantial original content | [Unity Asset Store EULA §2.2](https://unity.com/legal/as-terms) | Confirmed |
| Assets cannot be redistributed standalone or comprise a "substantial portion" of the product | [Unity Asset Store EULA §2.2](https://unity.com/legal/as-terms) | Confirmed |
| Assets with non-standard EULA are labelled on the store page | [Asset Store EULA FAQ](https://assetstore.unity.com/browse/eula-faq) | Confirmed |
| itch.io does NOT take ownership of uploaded content; creator retains all rights | [itch.io Creator FAQ](https://itch.io/docs/creators/faq) | Confirmed |

**Risk:** Each individual asset pack may have different license terms. Free assets on itch.io often use CC0, CC BY, or custom licenses. Check each pack's license file before use. Non-standard Asset Store assets are labelled; avoid them unless you accept the terms.

---

## 8. SFX-only, free packs (freesound.org, Mixkit, Unity Asset Store freebies)

**Verdict:⚠️ Low risk. Must verify per-sound license on freesound.org.**

| Claim | Source | Status |
|-------|--------|--------|
| Mixkit Sound Effects Free License allows commercial use, no attribution required, can be used in video games | [Mixkit License](https://mixkit.co/license/) | Confirmed |
| Freesound sounds use Creative Commons licenses; CC0 allows commercial use without attribution, CC BY requires attribution | [Freesound Terms of Service](https://freesound.org/help/tos_web/) | Confirmed |
| Freesound sounds with CC BY-NC / CC BY-NC-SA cannot be used in commercial games | [Freesound Terms of Service](https://freesound.org/help/tos_web/) | Confirmed |
| Unity Asset Store standard EULA covers free assets under the same terms as paid assets | [Unity Asset Store EULA](https://unity.com/legal/as-terms) | Confirmed |

**Risk:** Freesound is the riskiest source — each sound has a separate CC license. Filter by CC0 for safe commercial use. Mixkit is safest (no attribution, commercial OK). Always keep a record of which license applied to each asset.

---

## 9. Folder-per-type under `_Project/` namespace

**Verdict:✅ Confirmed good. No naming conflict.**

| Claim | Source | Status |
|-------|--------|--------|
| No reserved folder name starts with `_` or matches `_Project` | [Unity Manual — Reserved folder names](https://docs.unity3d.com/Manual/SpecialFolders.html) | Confirmed |
| Reserved names: `Editor`, `Resources`, `StreamingAssets`, `Plugins`, `Gizmos`, `Editor Default Resources`, `Standard Assets` | [Unity Manual — SpecialFolders](https://docs.unity3d.com/Manual/SpecialFolders.html) | Confirmed |
| The Editor automatically converts dot-prefix to underscore to prevent crashes (underscore is safe) | [Unity Manual — Hidden assets](https://docs.unity3d.com/Manual/SpecialFolders.html) | Confirmed |
| Unity recommends keeping internal assets separate from third-party assets | [Unity — Best practices for organizing your project](https://unity.com/how-to/organizing-your-project) | Confirmed |

**Risk:** None. Underscore-prefixed folders like `_Project/` have no special meaning. They sort to the top of the Project window, which is the intended behaviour.

---

## 10. Unity .gitignore pattern

**Verdict:⚠️ Outdated. Missing entries for Unity 6.**

| Source | Details |
|--------|---------|
| [GitHub Unity.gitignore template](https://github.com/github/gitignore/blob/main/Unity.gitignore) | 106-line authoritative template |

**Missing from current `.gitignore`:**

| Entry | Why needed |
|-------|------------|
| `.utmp/` | Unity 6 new temp folder |
| `*.blend1`, `*.blend1.meta` | Blender auto-save files |
| `/[Mm]emoryCaptures/` | Memory snapshot files (can contain sensitive data) |
| `/[Rr]ecordings/` | Timeline recordings (can be large) |
| `/[Aa]ssets/Plugins/Editor/JetBrains*` | JetBrains Rider auto-generated files |
| `*.DotSettings.user` | Rider personal settings |
| `.gradle/` | Gradle cache (Android builds) |
| `*.slnx`, `*.tmp`, `*.svd` | Various auto-generated IDE files |
| `mono_crash.*` | Mono crash dump files |
| `*.unitypackage`, `*.unitypackage.meta` | Exported package archives |
| `crashlytics-build.properties` | Crashlytics generated file |
| `InitTestScene*.unity*` | Test Runner generated scenes |
| `/ServerData`, `/[Aa]ssets/StreamingAssets/aa*`, `/[Aa]ssets/AddressableAssetsData/...` | Addressables build artifacts |
| `sysinfo.txt` | Unity crash system info |
| `/[Aa]ssets/Unity.VisualScripting.Generated/...` | Visual Scripting auto-generated files |

**Risk:** Medium. Missing entries won't break builds, but will clutter the repo with auto-generated files. The `.utmp/` entry is Unity 6-specific. Recommend updating to match the official template.

---

## Overall Verdict

**No critical issues found.** All 10 decisions are validated against primary sources:

| # | Decision | Verdict |
|---|----------|---------|
| 1 | Unity 6 + URP for 2D | ✅ Confirmed |
| 2 | Input System package | ✅ Confirmed (migration needed) |
| 3 | GameManager + thin architecture | ✅ Confirmed |
| 4 | JSON in StreamingAssets | ✅ Confirmed |
| 5 | 2D sprites in URP | ✅ Confirmed |
| 6 | uGUI for UI | ✅ Confirmed |
| 7 | Free asset packs (itch.io/Asset Store) | ⚠️ Check per-asset license |
| 8 | Free SFX packs | ⚠️ Avoid NC-licensed sounds |
| 9 | `_Project/` folder prefix | ✅ Confirmed |
| 10 | Unity .gitignore | ⚠️ Update to match GitHub template |

**Two actionable items:**

1. **Migrate input** from `Input.GetMouseButton()` (legacy) to the Input System package for proper mouse/touch abstraction.
2. **Update `.gitignore`** with the missing entries (especially `.utmp/` for Unity 6 and Addressables artifacts).
