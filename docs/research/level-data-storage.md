# Level Data Storage Approaches for Parking Jam

A comparison of strategies for storing 25–30 puzzle levels in a Unity 2D sliding-block game.

---

## 1. StreamingAssets + JSON

Store hand-authored `.json` files under `Assets/StreamingAssets/`, read them at runtime with `UnityWebRequest` (or `System.IO.File` on non-Android/WebGL platforms), and deserialize with `JsonUtility.FromJson<T>()`.

| Criterion | Assessment |
|-----------|------------|
| **Authoring** | A JSON file is plain text — any text editor works. A non-programmer can edit values after seeing one example file. No Unity license required. |
| **Runtime perf** | ~1–2 KB per level file, parsed in <1 ms with `JsonUtility`. Negligible impact. |
| **Cross-platform** | `Application.streamingAssetsPath` works on all platforms. Android & WebGL require `UnityWebRequest` instead of `File.ReadAllText`. The manual explicitly warns about this: *"You cannot use synchronous filesystem APIs … on the WebGL and Android platforms"* (Unity Docs: [StreamingAssets](https://docs.unity3d.com/Manual/StreamingAssets.html)). After loading, the in-memory representation is identical across platforms. |
| **Build size** | JSON files are copied verbatim, uncompressed — ~5–10 KB total for 30 levels. |
| **Version control** | Text files diff cleanly. Each level change is a single-line diff. |
| **Solo dev** | Trivial setup. No package installs. No build step for data. |

**Primary sources:**
- Unity Manual — Include additional files in a build (StreamingAssets): https://docs.unity3d.com/Manual/StreamingAssets.html
- Scripting API — `Application.streamingAssetsPath`: https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html
- Scripting API — `JsonUtility.FromJson`: https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html

---

## 2. Resources Folder + JSON

Place `.json` files (or `.txt` loaded as `TextAsset`) in any folder named `Resources`. Access via `Resources.Load<TextAsset>("path")`.

| Criterion | Assessment |
|-----------|------------|
| **Authoring** | Same as StreamingAssets — plain text files. But must stay inside a `Resources` folder. |
| **Runtime perf** | Acceptable for 30 levels (< 100 KB total data). However, *"initializing a Resources system containing 10 thousand assets takes several seconds on low-end mobile devices"* (Unity Manual: [Introduction to the Resources system](https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html)). At 25–30 assets we are far below that threshold. |
| **Cross-platform** | Uniform — no path differences. But the Resources system is built into the player. |
| **Build size** | **Always included** — *"Assets in Resources folders are always included in the Player build, even if they're not referenced by anything"* (ibid). Minor for a few files, but scales poorly. |
| **Version control** | Same as StreamingAssets — text files diff cleanly. |
| **Solo dev** | Simple, but requires knowing the `Resources` API. |

**Why Unity discourages it:** The manual is explicit: *"overall use of this feature is discouraged"* (ibid). The root problem is that Resources bundles everything into a single serialized file that grows with the project, hurting startup time and bloating builds. An older Unity best-practices article calls the Resources folder *"a common source of many problems in Unity projects"* that can *"bloat the size of a project's build"* (Unity Manual: [The Resources folder — BestPracticeUnderstandingPerformanceInUnity6](https://docs.unity3d.com/2021.2/Documentation/Manual/BestPracticeUnderstandingPerformanceInUnity6.html)). For 30 tiny JSON files the practical impact is near-zero, but the architectural guidance is to avoid the pattern.

**Primary sources:**
- Unity Manual — Introduction to the Resources system: https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html
- Unity Manual — The Resources folder (Best Practice): https://docs.unity3d.com/2021.2/Documentation/Manual/BestPracticeUnderstandingPerformanceInUnity6.html

---

## 3. Addressables

Install the `com.unity.addressables` package, mark level assets as Addressable, group them, and load via `Addressables.LoadAssetAsync<T>()`.

| Criterion | Assessment |
|-----------|------------|
| **Authoring** | Requires Unity Editor + Addressables Groups window to manage. A non-programmer would need training. |
| **Runtime perf** | Excellent for large projects — async loading, dependency tracking, reference counting. But for 30 levels it adds unnecessary async overhead and catalog lookups. |
| **Cross-platform** | Full support; can switch between local and remote delivery without code changes. |
| **Build size** | Only includes what is marked Addressable, with optional compression. |
| **Version control** | `.meta` files and `addressables_content_state.bin` binary artifacts must be committed/changed. |
| **Solo dev** | Overkill. The Unity Blog on Addressables best practices targets *"thousands of successful live games"* and scenarios with CDN delivery (Unity Blog: [Addressables: Planning and best practices](https://unity.com/blog/engine-platform/addressables-planning-and-best-practices)). For a 30-level puzzle game with no remote content plans, the complexity-to-benefit ratio is poor. The Addressables package itself acknowledges it builds on AssetBundles — *"If you want to use AssetBundles in your projects without writing your own detailed management code, you should use Addressables"* (Unity Docs: [Addressables package](https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/index.html)). For a solo developer shipping all content at install time, this is a heavyweight solution. |

**Primary sources:**
- Unity Addressables package manual: https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/index.html
- Unity Blog — Addressables: Planning and best practices: https://unity.com/blog/engine-platform/addressables-planning-and-best-practices
- Unity Blog — Learn to save memory usage with AssetBundles: https://unity.com/blog/engine-platform/learn-to-save-memory-usage-by-improving-the-way-you-use-assetbundles

---

## 4. ScriptableObject Level Assets

Create a `LevelDefinition : ScriptableObject` C# class with `[CreateAssetMenu]`, then create one `.asset` file per level in the project.

| Criterion | Assessment |
|-----------|------------|
| **Authoring** | Must be created inside the Unity Editor via Assets > Create menu. A non-programmer can edit fields in the Inspector, but cannot author levels without Unity open. The manual states: *"ScriptableObject is a serializable Unity type … you create instances of those custom classes, usually through the Assets menu in the Unity Editor"* (Unity Manual: [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html)). |
| **Runtime perf** | Best-in-class — direct references, no parsing, no allocation at load time. |
| **Cross-platform** | `.asset` files use Unity's native YAML-based serialization, which Unity handles identically on all platforms. |
| **Build size** | `.asset` files are included and serialized into the build automatically. Each file is a few hundred bytes to a few KB. |
| **Version control** | YAML-based `.asset` files are text and diff cleanly, though they contain GUIDs that change on rename. |
| **Solo dev** | Convenient for a solo dev who works in the Editor. But locked to the Editor — cannot be edited outside Unity, which prevents community level editors or external tooling. |

*Note: In a standalone Player build, ScriptableObject assets are read-only at runtime (Unity Manual: [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html)).*

**Primary source:**
- Unity Manual — ScriptableObject: https://docs.unity3d.com/Manual/class-ScriptableObject.html
- Unity Learn — Introduction to Scriptable Objects: https://learn.unity.com/tutorial/introduction-to-scriptable-objects

---

## 5. Tilemap Prefabs

Build each level as a Tilemap GameObject, save it as a prefab, load at runtime via `Instantiate`.

| Criterion | Assessment |
|-----------|------------|
| **Authoring** | Visual — paint tiles in the Scene view using the Tile Palette. A non-programmer can build levels. But level metadata (move limit, undo count, goal vehicle) has no natural place in a tilemap; it must be stored in an accompanying MonoBehaviour or ScriptableObject anyway. |
| **Runtime perf** | Tilemaps are designed for rendering static grids — excellent for display. But the vehicle and obstacle data still needs a separate data layer for game logic queries. |
| **Cross-platform** | Works on all platforms. |
| **Build size** | Each prefab includes the full tilemap data. For 30 levels, minor. |
| **Version control** | Prefab files are YAML — large, noisy diffs. |
| **Solo dev** | Practical for the visual layer but not for the logical level definition. Needs a companion data structure anyway. |

The Unity Learn tutorial on Tilemaps describes the system as *"perfect for 2D projects that contain gameplay levels"* for rapid visual prototyping (Unity Learn: [Introduction to Tilemaps](https://learn.unity.com/tutorial/introduction-to-tilemaps)), but a forum discussion about storing data in tiles highlights the limitation: `Tilemap.GetTile` returns a `TileBase` reference, and custom tile subclasses only hold authoring-time data — not instance-level game state (Unity Discussions: [storing data in tiles](https://discussions.unity.com/t/storing-data-in-tiles/938720)).

**Primary sources:**
- Unity Manual — Tilemap component reference: https://docs.unity3d.com/Manual/tilemaps/work-with-tilemaps/tilemap-reference.html
- Unity Learn — Introduction to Tilemaps: https://learn.unity.com/tutorial/introduction-to-tilemaps

---

## Comparison Matrix

| Approach | Non-programmer authoring | Runtime perf | Cross-platform | Build size | VC friendly | Solo dev fit |
|---|---|---|---|---|---|---|
| StreamingAssets + JSON | ★★★ Plain text | ★★★ ~1 ms parse | ★★★ (UWR on Android) | ★★★ ~5–10 KB | ★★★ Clean diffs | ★★★ Best |
| Resources + JSON | ★★★ Plain text | ★★☆ Fine for 30 assets | ★★★ Uniform | ★★☆ Always included | ★★★ Clean diffs | ★★☆ Discouraged pattern |
| Addressables | ★☆☆ Editor-only | ★★★ Great but async overhead | ★★★ Full | ★★★ Compressed | ★★☆ Binary artifacts | ★☆☆ Overkill |
| ScriptableObject | ★★☆ Editor-only | ★★★ No parse | ★★★ Native | ★★★ Small files | ★★☆ GUID noise | ★★☆ Editor lock-in |
| Tilemap prefab | ★★★ Visual paint | ★★☆ Visual only | ★★★ All platforms | ★★☆ Larger files | ★☆☆ Noisy YAML | ★★☆ Needs companion data |

---

## Recommendation

**Use StreamingAssets + JSON.**

For a 25–30 level puzzle game, JSON in StreamingAssets offers the best balance: plain-text authoring (any text editor), cross-platform compatibility (with a `UnityWebRequest` fallback for Android/WebGL), zero setup cost, clean version-control diffs, and runtime performance that is well within the bounds of a 2D sliding-block puzzle. The Unity manual explicitly lists *"configuration files in JSON"* as a primary use case for StreamingAssets. ScriptableObjects are a close second if you never need external tooling, but JSON ties you to nothing — you can later add a level editor, crowd-sourced levels, or a web-based designer without touching Unity.
