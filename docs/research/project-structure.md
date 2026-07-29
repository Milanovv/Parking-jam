# Unity Project Folder Structure Research — Parking Jam

**Solo dev · 2D sliding-block puzzle · 25 levels · PC / itch.io**

---

## 1. Unity's Official Stance

Unity does **not** prescribe a single folder structure. The Unity Manual and Best Practices guides say: *"There is no single way to organize a Unity project"* and advise to *"Pick what works for your team, document it, and stick to it."*

- Unity.com "Best practices for organizing your Unity project": https://unity.com/how-to/organizing-your-project
- Unity Manual "Best practice guides": https://docs.unity3d.com/6000.2/Documentation/Manual/best-practice-guides.html

Unity's own template projects (2D UFO, Ruby's Adventure, 2D Roguelike, MegaMan) all use a **folder-per-type** structure at the top level (`Scripts/`, `Prefabs/`, `Sprites/`, `Animations/`, `Scenes/`, `Materials/`, `Audio/`), matching what the Unity Hub templates generate by default. This is the de facto "official example" pattern even though it's not enforced.

---

## 2. Reserved (Special) Folder Names

The following folder names have **special meaning** when placed under `Assets/` and must not be used for general content:

| Folder | Behaviour | Docs |
|---|---|---|
| `Editor` | Editor-only scripts; not in builds. Unlimited, anywhere. | https://docs.unity3d.com/6000.7/Documentation/Manual/SpecialFolders.html |
| `Editor Default Resources` | EditorGUIUtility.Load path. One, root of `Assets/`. | same |
| `Resources` | Runtime on-demand loading via `Resources.Load()`. Increases build size. Unlimited, anywhere. | same |
| `StreamingAssets` | Raw files copied as-is into build. One, root of `Assets/`. | same |
| `Plugins` | Third-party native plugins. | same |
| `Gizmos` | Gizmo icons for `Gizmos.DrawIcon`. One, root of `Assets/`. | same |
| `Standard Assets` | Legacy; deprecated in Unity 6 but still recognised. | same |

Folders ending with `~` are **hidden** from the Unity Project window. Folders starting with `.` are **ignored** by the importer (except under `StreamingAssets`). Hidden folders and `.tmp` files are also ignored.

Files/folders matching `cvs` (case-insensitive) are also ignored during import.

---

## 3. "Folder per Feature" vs "Folder per Type"

This is the central debate in the community. Both approaches are valid.

### Folder-per-type (by asset category)

```
Assets/
├── Scripts/
├── Prefabs/
├── Sprites/
├── Audio/
├── Animations/
├── Scenes/
├── Materials/
├── Fonts/
└── Plugins/
```

**Pros:**
- Matches Unity's own template projects and tutorials (2D UFO, Ruby's Adventure)
- Familiar to every Unity developer
- Easy to find "all scripts" or "all sprites"
- Unity's search-by-type filter works naturally

**Cons:**
- A single feature (e.g., the Player car) spreads across 5+ folders — Scripts/Player, Prefabs/Player, Sprites/Player, etc.
- Deleting a feature requires hunting through every type folder
- No natural boundary for Assembly Definitions later

### Folder-per-feature (by gameplay system)

```
Assets/
├── _Project/
│   ├── Car/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   ├── Sprites/
│   │   └── Animations/
│   ├── Grid/
│   ├── Levels/
│   ├── UI/
│   └── Core/
├── Scenes/
├── Plugins/
└── Sandbox/
```

**Pros:**
- All files for one feature live together
- Deleting a feature = delete one folder
- Maps cleanly to Assembly Definitions
- Works well with Unity's search system (filter by type in a selected folder)

**Cons:**
- Shared assets (a font used everywhere) need a home — typically `_Project/_Shared/`
- Less familiar to new Unity devs

### Community consensus

- **Solo devs / small projects**: folder-per-type is simpler and perfectly adequate.
- **Medium+ projects / teams**: folder-per-feature scales better.
- The strongest advice from Unity's own e-book: *"Document your conventions and be consistent."*

Sources:
- Unity Discussions thread (2010–2022): https://discussions.unity.com/t/best-practices-folder-structure/426959
- Unity Discussions (2024): https://discussions.unity.com/t/how-do-you-decide-what-your-file-structure-should-be/1573333
- UnityEngineering Substack analysis: https://unityengineering.substack.com/p/the-folder-structure-mistake-that
- Game Dev Beginner guide: https://gamedevbeginner.com/how-to-structure-your-unity-project-best-practice-tips/
- Anchorpoint guide (Unity 6): https://www.anchorpoint.app/blog/unity-folder-structure

---

## 4. What Popular Tutorial Creators Use

| Creator | Structure | Source |
|---|---|---|
| **Brackeys** (RPG Tutorial) | Folder-per-type: `Scripts/`, `Prefabs/`, `Animations/`, `Audio/`, etc. | https://github.com/Brackeys/RPG-Tutorial |
| **Code Monkey** | Folder-per-type in tutorials; folder-per-feature in his more advanced projects (e.g., "Complete Platformer") | Various YouTube videos |
| **Sebastian Lague** | Flat folder-per-type with `Scripts/`, `Resources/`, `Prefabs/` | GitHub repos; his "Creating a game in Unity" series |
| **timdhoffmann/style-guide** | Recommends folder-per-feature under `_Project/` with `Core/`, `Characters/`, `Environment/`, `UI/`, etc. | https://github.com/timdhoffmann/unity-project-style-guide |

All tutorial creators use folder-per-type for teaching because it's immediately clear. More experienced creators tend to add an `_Project/` wrapper and shift toward feature grouping as complexity grows.

---

## 5. Addressables/Content Management

Addressables does **not** impose a folder structure convention. It works with any folder layout via labels and `AddressableAssetGroup` assets. Best practices:

- Put addressable assets in a folder that clearly marks them (e.g., `Assets/_Project/Content/`)
- Use labels for logical grouping rather than folder hierarchy
- Keep the `AddressableAssetsData/` folder committed to version control (except `*.bin` files)

Addressables docs: https://docs.unity3d.com/Packages/com.unity.addressables@latest

For a 25-level puzzle game, Addressables is overkill — direct references in scenes and `Resources.Load()` for level data (if needed) is sufficient.

---

## 6. Version Control Implications

### What to commit

| Path | Commit? | Reason |
|---|---|---|
| `Assets/` | **Yes** | All your game content |
| `Packages/manifest.json` + `packages-lock.json` | **Yes** | Package dependencies |
| `ProjectSettings/` | **Yes** | Project configuration |
| `*.meta` files | **Yes** | GUIDs and import settings — essential for team sync |
| `UserSettings/` | **No** | Local preferences only |

### What to ignore

| Path | Ignore? | Reason |
|---|---|---|
| `Library/` | **Yes** | Local cache; regenerated on import |
| `Temp/` | **Yes** | Cleared on editor close |
| `obj/` | **Yes** | Mono compilation intermediates |
| `Build/` or `Builds/` | **Yes** | Player executables |
| `Logs/` | **Yes** | Editor logs |
| `*.csproj`, `*.sln` | **Yes** | Generated by Unity; varies by IDE |

### Recommended .gitignore

Use the official GitHub Unity template: https://github.com/github/gitignore/blob/main/Unity.gitignore

Sources:
- Unity Version Control ignore docs: https://docs.unity.com/en-us/unity-version-control/ignore-files.md
- Unity Manual on default directories: https://docs.unity3d.com/6000.4/Documentation/Manual/default-directories.html

---

## 7. Simplest Viable Structure for a Solo Dev

For Parking Jam — a small 2D puzzle game with 25 levels, one dev, no team — **folder-per-type with an `_Project/` wrapper** is the best balance of simplicity, discoverability, and future-proofing.

```
Assets/
├── _Project/                  ← All YOUR work (keeps 3rd-party separate)
│   ├── Scripts/
│   │   ├── GameManager.cs
│   │   ├── LevelLoader.cs
│   │   ├── OccupancyMap.cs
│   │   ├── CarController.cs
│   │   ├── InputHandler.cs
│   │   └── UI/
│   │       ├── LevelSelectScreen.cs
│   │       ├── WinScreen.cs
│   │       └── MoveCounter.cs
│   ├── Prefabs/
│   │   ├── Car.prefab
│   │   ├── GridCell.prefab
│   │   ├── LevelButton.prefab
│   │   └── UI/
│   │       └── (UI prefabs)
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── LevelSelect.unity
│   │   └── Levels/
│   │       ├── Level_01.unity
│   │       ├── Level_02.unity
│   │       └── ... (25)
│   ├── Sprites/
│   │   ├── Cars/
│   │   ├── Grid/
│   │   ├── UI/
│   │   └── Backgrounds/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Fonts/
│   ├── Animations/
│   └── Settings/
│       └── GameSettings.asset
├── Plugins/                   ← 3rd-party assets (TextMeshPro, etc.)
├── Scenes/                    ← (optional, if tutorials place a scene here)
└── StreamingAssets/           ← (if needed for level data files)
```

### Rationale

1. **`_Project/` prefix** — Sorts to top in the Project window. Unity's own guide recommends keeping your assets separate from third-party ones.
2. **Folder-per-type** — For a 2-puzzle game with only 4-6 script files, feature folders add structure without benefit. You'll never have 20+ files per feature.
3. **`Levels/` subfolder in Scenes/** — Clean separation for 25 scenes vs ~2 UI scenes.
4. **No `Resources/`** — Avoids the build-bloat trap unless truly needed. Prefer direct references via prefabs and scenes.
5. **No empty folders** — Only create subfolders (e.g., `Sprites/UI/`) when you actually have assets to put in them.
6. **Namespaces match folders** — `Project.CarController`, `Project.UI.LevelSelectScreen` — Unity's recommended pattern.

### If the project grows

If later you find yourself needing to split code into assemblies for compile performance, convert to folder-per-feature at that point. The `_Project/` shell makes this a local rename, not a repo-wide shuffle.

---

## Recommendation

**Use folder-per-type under a single `_Project/` namespace folder.** It matches Unity's own tutorial structure, is immediately navigable for a solo dev, requires zero subfolder ceremony for a 25-level puzzle game, and the `_Project/` wrapper cleanly isolates your code from third-party assets. Document the structure in `AGENTS.md` and be consistent.

---

## Sources Index

1. Unity Manual — Reserved folder names: https://docs.unity3d.com/6000.7/Documentation/Manual/SpecialFolders.html
2. Unity Manual — Default project directories: https://docs.unity3d.com/6000.4/Documentation/Manual/default-directories.html
3. Unity Manual — Assembly definitions: https://docs.unity3d.com/6000.3/Documentation/Manual/assembly-definitions-intro.html
4. Unity Manual — Package layout (UPM): https://docs.unity3d.com/6000.3/Documentation/Manual/cus-layout.html
5. Unity.com — Best practices for organizing your project: https://unity.com/how-to/organizing-your-project
6. Unity.com — Best practices for project organization (Unity 6): https://unity.com/resources/best-practices-version-control-unity-6
7. Unity Version Control — Ignore files: https://docs.unity.com/en-us/unity-version-control/ignore-files.md
8. GitHub Unity.gitignore template: https://github.com/github/gitignore/blob/main/Unity.gitignore
9. Unity Discussions — Best practices folder structure: https://discussions.unity.com/t/best-practices-folder-structure/426959
10. Unity Discussions — Ordering scripts by type or system: https://discussions.unity.com/t/ordering-scripts-by-type-or-by-system/924973
11. UnityEngineering Substack — Folder structure mistake: https://unityengineering.substack.com/p/the-folder-structure-mistake-that
12. Anchorpoint — Guide to folder structures for Unity 6: https://www.anchorpoint.app/blog/unity-folder-structure
13. Game Dev Beginner — How to structure your Unity project: https://gamedevbeginner.com/how-to-structure-your-unity-project-best-practice-tips/
14. timdhoffmann Unity project style guide: https://github.com/timdhoffmann/unity-project-style-guide
15. Brackeys RPG Tutorial (folder structure example): https://github.com/Brackeys/RPG-Tutorial
16. Unity Learn — 2D UFO Tutorial (project structure example): https://learn.unity.com/pathway/mobile-ar-development/unit/ar-experience-design/tutorial/ufo-project-overview
