# Unity Requirements Research — Parking Jam

## 1. Tile-Based Grid System

### Grid Component
Unity provides a `Grid` component that acts as a layout guide to align GameObjects based on a selected cell layout. It transforms cell positions to local coordinates. For a parking lot puzzle, the **Rectangle** Cell Layout is appropriate (as opposed to Hexagon or Isometric). The `Cell Size` property defines tile dimensions.

**Source:** [Grid component reference — Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/tilemaps/grid-reference.html)

### Tilemap
`Tilemap` is the companion component that renders tiles on top of a Grid. It supports a `TilemapCollider2D` for per-tile collision. For Parking Jam, the Tilemap can render the parking lot background (static tiles) while vehicles are separate GameObjects snapped to grid positions.

**Source:** [Tilemap component reference — Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/tilemaps/work-with-tilemaps/tilemap-reference.html)

### Discrete Movement Implementation
Vehicles can be implemented as MonoBehaviours that store a `Vector3Int` grid position. Movement works by:
- Converting drag input to a grid-axis direction
- Checking occupancy via a 2D array or dictionary mapping `Vector3Int` to vehicle/obstacle references
- Using `Grid.WorldToCell()` to convert world coordinates to cell coordinates
- Lerping/snapping the vehicle transform between cell positions over a short duration

**Source:** [2D Roguelike tutorial — Unity Learn](https://learn.unity.com/project/2d-roguelike) (reference implementation for tile-based movement)

### Recommendation
Use a **Grid + Tilemap** for the parking lot background. Vehicles are **non-Tilemap GameObjects** (SpriteRenderer-based) with a custom grid-occupancy system for collision detection. The Grid component's `CellToWorld()` and `WorldToCell()` methods provide the coordinate transforms needed.

---

## 2. 2D vs 3D

### Unity Modes
Unity supports 2D and 3D project modes. The choice affects default import settings (textures as sprites vs textures) and default scene setup. You can switch modes at any time.

**Source:** [2D and 3D projects — Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/2Dor3D.html)

### Tradeoffs for a Top-Down Parking Puzzle

| Aspect | 2D Approach | 3D Approach |
|---|---|---|
| **Assets** | Sprite-based (flat images, pixel art). Simpler pipeline. | 3D models with textures. Requires modelling/rigging. |
| **Camera** | Orthographic, fixed top-down. Simple. | Perspective or orthographic. Needs positioning and FOV tuning. |
| **Lighting** | 2D lights (URP 2D Renderer) or no lighting. | Full 3D lighting, shadows — more expensive on mobile. |
| **Collision** | 2D physics (BoxCollider2D). Simpler and cheaper. | 3D physics (BoxCollider). Overkill for a grid puzzle. |
| **Mobile perf** | Lower fill-rate, cheaper rendering. Better battery. | Higher GPU load. Needs LODs, occlusion culling. |
| **Animation** | Sprite swapping or 2D Animation package. | Model animations (Animator). More complex. |
| **Dev time** | Faster. | Slower due to asset pipeline. |

**Source:** [2D and 3D projects — Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/2Dor3D.html)

### Recommendation
**Start in 2D mode.** Parking Jam is a discrete grid puzzle — 3D adds visual complexity without gameplay benefit. Use an **orthographic camera** locked to a top-down view. The 2D approach reduces asset cost, simplifies collision, and improves mobile performance. If a 3D look is desired later, use 3D models in an orthographic camera (the "2.5D" approach described in the Unity Manual), but still restrict gameplay to 2D grid logic.

---

## 3. Unity UI System

### UI Systems Comparison
Unity provides three UI systems: **uGUI (Unity UI)**, **UI Toolkit**, and **IMGUI**. For runtime game UIs, the recommendation is:

| Use Case | Recommendation |
|---|---|
| Multi-resolution menus and HUD | UI Toolkit |
| World space UI and VR | uGUI |
| UI with custom shaders/materials | uGUI |

**Source:** [Comparison of UI systems — Unity Manual](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html)

### uGUI (Canvas-based)
- Based on GameObjects and Components
- Well-established, production-proven
- Easy referencing from MonoBehaviours
- Supports Animation Clips and Timeline integration
- Canvas Scaler for resolution adaptation

### UI Toolkit
- Web-like markup (UXML) and stylesheets (USS)
- Better for data-heavy screens and multi-resolution
- Still lacks some runtime features (no in-scene authoring, no serialized events)
- Intended as the future direction but uGUI is still recommended for runtime

**Source:** [UI system comparison — Unity Manual (Unity 6)](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html)

### HUD Layout for Parking Jam
The design calls for:
- **Top-left**: Settings button
- **Top-right**: Currency display
- **Bottom row** (left to right): Daily Missions, Challenges, Collection, Events

#### uGUI Implementation
- One **Canvas** with **Screen Space - Overlay** render mode
- **Canvas Scaler** set to Scale With Screen Size (reference 1080x1920)
- Use **Anchors** to pin elements to corners and bottom:
  - Settings: Anchor top-left
  - Currency: Anchor top-right
  - Bottom buttons: Anchor bottom-center, distribute with Horizontal Layout Group

### Recommendation
Use **uGUI (Canvas system)** for Parking Jam. It is the official recommendation for runtime UI, it integrates with the Animator for button transitions, and it is easier to prototype for a small team. Use UI Toolkit only if you need complex data-driven menus or if your team prefers web-style development.

---

## 4. Data Persistence

### PlayerPrefs
Unity's built-in key-value store for strings, floats, and ints. Data is stored in the platform registry (Windows registry, iOS NSUserDefaults, Android SharedPreferences). No encryption. Limited to simple data types.

**Best for:** Settings, volume, quality preferences, flags (has received welcome gift).

**Source:** [PlayerPrefs scripting API — Unity Manual](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html)

### JSON Serialization (JsonUtility)
Unity's `JsonUtility` class converts `MonoBehaviour`/`ScriptableObject`/POCO objects to/from JSON. Supports complex nested data. Works with `File.WriteAllText` to save to `Application.persistentDataPath`.

**Best for:** Full game state (coins, keys, unlocked skins, level progress, inventory).

```csharp
[System.Serializable]
public class PlayerSaveData {
    public int coins;
    public int keys;
    public List<string> unlockedVehicleSkins;
    public List<string> unlockedPedestrianSkins;
    public int lastCompletedLevel;
    public int dailyLoginStreak;
}
```

**Source:** [JSON Serialization — Unity Manual](https://docs.unity3d.com/6000.5/Documentation/Manual/json-serialization.html)

### Application.persistentDataPath
Cross-platform path for save files:
- Windows: `%userprofile%\AppData\LocalLow\<Company>\<Product>`
- Android: `/data/data/<package>/files`
- iOS: `/var/mobile/Containers/Data/Application/<GUID>/Documents`

**Source:** [Persistent data — Unity Blog](https://unity.com/blog/games/persistent-data-how-to-save-your-game-states-and-settings)

### Unity Economy Service (Cloud)
Unity's cloud-based Economy service provides player inventories, currency balances, and purchases managed from the Unity Dashboard. Requires internet connection. Useful for server-authoritative data (premium currency).

**Source:** [Economy — Unity Docs](https://docs.unity.com/en-us/economy)

### Recommendation
Use a **hybrid approach**:
- **PlayerPrefs** for settings and first-launch flags (welcome gift flag)
- **JsonUtility + file I/O** for game save data (coins, keys, skins, progress) written to `Application.persistentDataPath`
- **Unity Economy** if cloud save / server-authoritative premium currency is needed later

---

## 5. Mobile Build Settings

### Android Build Requirements

**Source:** [Android build settings reference — Unity Manual](https://docs.unity3d.com/6000.5/Documentation/Manual/android-build-settings.html)

| Setting | Recommendation |
|---|---|
| **Texture Compression** | ASTC (default for modern Android devices) |
| **Minimum API Level** | API 24 (Android 7.0) as minimum, target latest |
| **Scripting Backend** | IL2CPP (better performance, required for 64-bit) |
| **Target Architecture** | ARM64 |
| **Graphics APIs** | Vulkan (primary), OpenGL ES 3.0 (fallback) |
| **Multithreaded Rendering** | Enabled |
| **Optimize Mesh Data** | Enabled |
| **Strip Engine Code** | Enabled (reduces build size) |

Additional requirements:
- **Package Name** set in Player Settings (e.g., `com.company.parkingjam`)
- **Keystore** for signing release builds
- **Android SDK, NDK, JDK** installed via Unity Hub

### iOS Build Requirements

**Source:** [Set up an iOS build configuration — Unity Build Automation](https://docs.unity.com/en-us/build-automation/basic-build-configuration/set-up-an-ios-build-configuration)

| Setting | Recommendation |
|---|---|
| **Scripting Backend** | IL2CPP |
| **Target Architecture** | ARM64 |
| **Target SDK** | Device SDK |
| **Target minimum iOS Version** | 13.0+ |
| **Graphics APIs** | Metal |
| **Camera Description** | Required if using for any reason (privacy manifest) |

Requirements:
- **Mac with Xcode** (required to compile the Xcode project Unity exports)
- **Apple Developer Program** membership for distribution
- **Provisioning Profile** and **Signing Certificate**
- **Privacy Manifest** (`PrivacyInfo.xcprivacy`) required by Apple for App Store submissions

**Source:** [iOS build configuration — Unity Docs](https://docs.unity.com/en-us/build-automation/basic-build-configuration/set-up-an-ios-build-configuration)

### General Mobile Optimization
- Use **URP (Universal Render Pipeline)** for efficient mobile rendering
- Disable HDR and post-processing for low-end devices
- Enable **GPU Skinning** for animated characters
- Use **Sprite Atlases** to batch draw calls
- Set **Quality Settings** to lowest for better battery

---

## 6. Animation (Animated Pedestrians / Mobile Obstacles)

### Unity's Animator Controller
The standard approach: create an **Animator Controller** asset with animation clips (e.g., walk cycle sprites), configure states and transitions. The pedestrian GameObject gets an `Animator` component referencing the controller.

**Source:** [Animator Controller — Unity Manual](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AnimatorController.html)

**Source:** [Create 2D sprite animations — Unity Learn](https://learn.unity.com/tutorial/create-2d-sprite-animations)

### 2D Animation Package (Skeletal Animation)
Unity's `2D Animation` package provides rigging and bone-based skeletal animation for 2D sprites. Supports Inverse Kinematics. Better for characters with multiple body parts that need to move independently (e.g., arm swings during walking).

**Source:** [Getting started with 2D Animation package — Unity Blog](https://unity.com/blog/engine-platform/getting-started-with-2d-animation-package)

### Sprite Sheet / Flipbook Animation
The simplest approach: provide multiple sprites per direction and swap them in sequence using the Animation window. Low overhead, suitable for mobile. Pedestrians need 4-directional walk cycles (up, down, left, right) or simple 2-directional (horizontal patrol).

### Spine (Third-party)
[Spine](http://esotericsoftware.com/) is a popular third-party 2D skeletal animation tool with a Unity runtime. It is more performant than Unity's built-in 2D Animation for complex rigs, but costs a license fee ($299+). Well-established in mobile games.

### Recommendation
For Parking Jam's pedestrians (simple patrol animations), **Unity's built-in Animator Controller with sprite flipbook animation** is sufficient and free. If pedestrians have complex animations (multiple limbs, distinct walk styles), use the **2D Animation package** for skeletal rigging. Avoid Spine unless the budget allows and the complexity justifies it.

---

## 7. Mini-Games Integration

### Scene Management (SceneManager.LoadSceneAsync)
Unity's `SceneManager` supports loading scenes **additively**. The main game scene stays loaded while mini-game scenes are loaded on top. This is the simplest approach — mini-games are just additional `.unity` scene files.

```csharp
SceneManager.LoadSceneAsync("MiniGame_Pipes", LoadSceneMode.Additive);
```

When the mini-game completes, unload the scene:
```csharp
SceneManager.UnloadSceneAsync("MiniGame_Pipes");
```

**Source:** [SceneManager.LoadSceneAsync — Unity Scripting API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)

### Addressables
Unity's **Addressables** system is designed for managing content at scale. Mini-game scenes can be marked as Addressable, loaded on-demand, and unloaded. Benefits:
- Mini-games can be downloaded separately (reduce initial install size)
- Content can be updated without a full app update
- Better memory management

```csharp
Addressables.LoadSceneAsync("MiniGame_Pipes", LoadSceneMode.Additive);
```

**Source:** [Load a scene — Addressables 1.21](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/LoadingScenes.html)

**Source:** [Addressables: Planning and best practices — Unity Blog](https://unity.com/blog/engine-platform/addressables-planning-and-best-practices)

### Recommendation
Start with **SceneManager additive loading** for simplicity. The mini-game pool is fixed and small (5-10 mini-games), so there is no need for Addressables initially. **Migrate to Addressables** later if:
- The mini-game library grows beyond ~15 scenes
- You need over-the-air content updates
- Install size becomes a concern

---

## 8. Economy System

### Unity Economy Service
Unity's cloud-based Economy service provides:
- **Virtual currencies** (coins, keys) with balances managed server-side
- **Inventory items** (skins, power-ups) per player
- **Purchases** (buying items with currency)
- **Currency conversion** (real money → premium currency)
- Dashboard configuration without code changes

**Source:** [Economy — Unity Docs](https://docs.unity.com/en-us/economy/implementation)

### Local Implementation (Recommended for MVP)
For a simple game, implement the economy locally using the data persistence approach from Section 4:

```csharp
[System.Serializable]
public class EconomyData {
    public int coins;
    public int keys;
    public Dictionary<string, bool> ownedVehicleSkins;
    public Dictionary<string, bool> ownedPedestrianSkins;
}
```

- **Coins**: earned from level completion, daily missions, challenges. Stored in save file.
- **Keys**: purchased via IAP (in-app purchase). Store as integer in save file (client-side, or better: validate server-side).

### In-App Purchases (IAP)
Unity's **Unity IAP** package handles store transactions for both Android (Google Play) and iOS (App Store). Use it to sell Key packs.

**Source:** [Unity IAP — Unity Manual](https://docs.unity3d.com/Manual/UnityIAP.html)

### Economy Design for Parking Jam

| Resource | Type | Source | Sink |
|---|---|---|---|
| Coins | Soft currency | Level completion, daily missions, challenges | Skin purchases, barrier skips |
| Keys | Hard currency | IAP purchase | Exclusive skins |

### Recommendation
Start with a **local economy** (JSON-serialized `EconomyData` class) for the MVP. Add **Unity IAP** for Key purchases when monetization is needed. Migrate to **Unity Economy Service** if server-authoritative data is required for anti-cheat or cross-device sync.

---

## Summary

| Topic | Recommended Approach | Primary Source |
|---|---|---|
| Grid System | Grid + Tilemap (Rectangle layout), vehicles as separate GameObjects with grid-occupancy logic | [Grid component reference](https://docs.unity3d.com/6000.3/Documentation/Manual/tilemaps/grid-reference.html) |
| 2D vs 3D | 2D mode, orthographic camera, top-down | [2D and 3D projects](https://docs.unity3d.com/6000.3/Documentation/Manual/2Dor3D.html) |
| UI System | uGUI (Canvas) with anchors and Canvas Scaler | [UI system comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html) |
| Data Persistence | PlayerPrefs (settings) + JsonUtility to file (game data) | [PlayerPrefs API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html), [JSON Serialization](https://docs.unity3d.com/6000.5/Documentation/Manual/json-serialization.html) |
| Mobile Build | IL2CPP, ARM64, ASTC (Android), Metal (iOS), URP pipeline | [Android build settings](https://docs.unity3d.com/6000.5/Documentation/Manual/android-build-settings.html) |
| Animation | Animator Controller with sprite flipbook or 2D Animation package | [Animator Controller](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AnimatorController.html) |
| Mini-games | SceneManager additive loading (start), Addressables (scale) | [Addressables scene loading](https://docs.unity3d.com/Packages/com.unity.addressables@1.21/manual/LoadingScenes.html) |
| Economy | Local JSON (MVP), Unity IAP (Keys), Unity Economy (scale) | [Economy docs](https://docs.unity.com/en-us/economy) |
