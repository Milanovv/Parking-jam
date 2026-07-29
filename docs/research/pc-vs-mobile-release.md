# PC-First vs Mobile-First Release Strategy for a Sliding-Block Puzzle Game in Unity

**Researched:** July 2026  
**Game type:** Sliding-block puzzle (e.g., "Parking Jam") — 2D grid, low poly count, minimal animation, simple UI.

---

## 1. Unity Build Settings & Platform Differences

### PC (Windows, macOS, Linux)

Building for standalone PC platforms requires **no extra SDKs** beyond the Unity Editor. The build output is a native executable:

- **Windows:** `.exe` + `_Data` folder (BuildTarget.StandaloneWindows64)
- **macOS:** `.app` bundle (BuildTarget.StandaloneOSX)
- **Linux:** x86_64 executable (BuildTarget.StandaloneLinux64)

No platform-specific signging, provisioning profiles, or SDK installs are required.  
**Source:** [Unity Manual — Build Profiles Window Reference](https://docs.unity3d.com/Manual/build-profiles-reference.html)  
**Source:** [Unity Scripting API — BuildTarget](https://docs.unity3d.com/6000.7/Documentation/ScriptReference/BuildTarget.html)

### Mobile (iOS, Android)

Building for mobile requires **additional platform modules, SDKs, and toolchains**:

- **iOS:** Requires macOS (IL2CPP must build natively on macOS), Xcode, Apple Developer account, provisioning profiles, and code-signing certificates. Unity generates an Xcode project; final `.ipa` build is done from Xcode.  
  **Source:** [Unity Manual — Structure of a Unity Xcode project](https://docs.unity3d.com/6000.5/Documentation/Manual/ios-structure-of-swift-xcode-project.html)  
  **Source:** [Unity Manual — Introduction to IL2CPP](https://docs.unity3d.com/6000.2/Documentation/Manual/il2cpp-introduction.html) ("IL2CPP also requires some systems native to the target platform to generate the C++ code. This means that cross-compilation is generally not supported… to build an IL2CPP Player for a particular target platform you must build from an Editor running on the same platform. The exception is Linux.")

- **Android:** Requires Android SDK, NDK, Gradle, JDK. Unity uses Gradle for all Android builds — it generates a Gradle project, then runs Gradle to produce `.apk` or `.aab`.  
  **Source:** [Unity Manual — Gradle for Android](https://docs.unity3d.com/6000.5/Documentation/Manual/android-gradle-overview.html)  
  **Source:** [Unity Manual — How Unity builds Android applications](https://docs.unity3d.com/6000.0/Documentation/Manual/how-unity-builds-android-applications.html)

**Verdict:** PC builds require zero extra SDKs; mobile requires macOS for iOS + Android SDK/Gradle for Android. This setup time and CI complexity is **strictly higher for mobile**.

---

## 2. Unity Input System — Keyboard/Mouse vs Touch

### PC Input (Keyboard & Mouse)

The new Input System supports `Keyboard` and `Mouse` as first-class devices:

- **Keyboard:** Uses `KeyControl` per key (e.g., `Key.A`), supports `onTextInput` for text entry, IME for East-Asian scripts.  
  **Source:** [Input System — Keyboard support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/manual/Keyboard.html)

- **Mouse:** Provides `Mouse.current.position.ReadValue()`, button presses, scroll wheel.  
  **Source:** [Input System — Migration guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.1/manual/Migration.html)

For a sliding-block puzzle, the interaction is simple: **click to select a vehicle, drag to slide it**. The Input System handles this natively.

### Mobile Input (Touch)

Touch input is supported on Android, iOS, Windows, and UWP via the `Touchscreen` class:

- **Low-level:** `Touchscreen.current` — not recommended for polling.
- **High-level:** `EnhancedTouch.Touch.activeTouches` — similar to `UnityEngine.Input.touches`.
- Multi-touch is supported; each finger is a separate pointer.
- **Touch simulation** from mouse/pen is available for editor testing (`TouchSimulation.Enable()`).  
  **Source:** [Input System — Touch support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.2/manual/Touch.html)

### UI Module Differences

The `InputSystemUIInputModule` can unify pointer input or treat each device separately. The default "Single Mouse or Pen But Multi Touch And Track" works across both.  
**Source:** [Input System — UI support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.16/manual/UISupport.html)

**Verdict:** A sliding-block puzzle with tap/drag is straightforward on both. **No significant extra complexity** — Unity's Input System abstracts the difference, and touch simulation works in the editor. A PC-first approach can use mouse input trivially, and adding touch later is well-supported.

---

## 3. Build Sizes & Asset Pipeline

### PC (Standalone)

- **Default compression:** None on Windows, macOS, Linux. LZ4 or LZ4HC optional.
- **No store-imposed size limit.** The build can be as large as needed.
- Texture compression defaults to platform-appropriate formats but is not constrained by mobile GPU limits.

**Source:** [Unity Manual — Build Profiles](https://docs.unity3d.com/Manual/build-profiles-reference.html) (Compression Method table)

### Mobile

- **Android default compression:** ZIP (slightly better compression than LZ4HC, but slower to decompress).
- **iOS default compression:** None (same as standalone).
- **Google Play size limit:** Base module + asset packs must fit within Google's limits (~4GB for the total AAB, individual packs capped at 1.5GB for install-time).  
  **Source:** [Unity Manual — Asset packs in Unity](https://docs.unity3d.com/6000.4/Documentation/Manual/android-asset-packs-in-unity.html)

- **Apple App Store size limit:** 4GB maximum; cellular download capped at 200MB (user is prompted for larger).
- **Asset pipeline:** Textures, meshes, and audio must be carefully compressed for mobile GPU constraints. The Unity Manual explicitly says "Keeping the file size of the built app to a minimum is important, especially for mobile devices."  
  **Source:** [Unity Manual — Reducing the file size of a build](https://docs.unity3d.com/6000.1/Documentation/Manual/ReducingFilesize.html)

**Verdict:** For a 2D puzzle game with small asset sizes, this is a **minor concern** — but the pressure to minimize size exists only for mobile. PC has no meaningful constraint.

---

## 4. App Store Submission Requirements — Costs & Review Times

### Steam (PC)

| Item | Detail |
|---|---|
| Developer account fee | $100 USD **per app** (one-time, recoupable after $1,000 AGR) |
| Annual fee | None |
| Revenue share | 30% on first $10M lifetime, 25% on $10–50M, 20% above $50M |
| Review time | **3–5 business days** for store page and build review; plan for 7+ business days |
| Update review | **None** — updates go live immediately after upload |
| Developer identity | 30-day waiting period after fee payment for first release; identity verification required |

**Sources:**  
[Steam Direct Fee](https://partner.steamgames.com/doc/gettingstarted/appfee)  
[Steamworks Onboarding](https://partner.steamgames.com/doc/gettingstarted/onboarding?language=english)  
[Steam Review Process](https://partner.steamgames.com/doc/store/review_process?language=english)  
[Steam Revenue Share](https://www.immutable.com/guides/how-much-does-steam-take)

### Apple App Store (iOS)

| Item | Detail |
|---|---|
| Developer account fee | **$99/year** (annual renewal required) |
| Revenue share | 30% standard; 15% for Small Business Program (under $1M/year) |
| Review time | **24–48 hours** standard; up to 3–5 days during peak; expedited available |
| Update review | Each update goes through full review |
| Rejection rate | 5–10%; strict guidelines on design quality and functionality |

**Sources:**  
[Apple Developer Program pricing](https://support.unity.com/hc/en-us/articles/28114350573460-Which-Unity-Editor-license-should-I-use-purchase) (via Apple)  
[App Store vs Google Play Publishing Costs 2026](https://gtstu.com/app-store-google-play-publishing-costs/)  
[App Review Process — ASO Wiki](https://asotxt.com/wiki/app-review-process)  
[Game Platform Distribution Agreements](https://blog.promise.legal/game-platform-distribution-agreements/)

### Google Play Store (Android)

| Item | Detail |
|---|---|
| Developer account fee | **$25 one-time** (no annual renewal) |
| Revenue share | 15% on first $1M/year, 30% above; subscriptions 15% from day one |
| Review time | **Hours–3 days** for new apps (mostly automated); updates usually hours |
| New account gate | **14-day closed test with 12 testers** required for personal accounts created after Nov 2023 |
| Update review | Typically hours |

**Sources:**  
[Google Play Console — Get started](https://support.google.com/googleplay/android-developer/answer/6112435?hl=en)  
[App Store vs Google Play Publishing Costs 2026](https://gtstu.com/app-store-google-play-publishing-costs/)  
[The Complete First-Time App Review Guide for 2026](https://capgo.app/blog/first-time-app-review-guide/)

### Comparison Summary

| Store | Upfront cost | Recurring cost | Review time (new app) | Review time (updates) |
|---|---|---|---|---|
| **Steam** | $100/app | None | 3–5 business days | **None (instant)** |
| **Apple App Store** | None | $99/year | 24–48 hours | 24–48 hours |
| **Google Play** | $25 | None | Hours–3 days (+14-day testing gate) | Hours |

**Verdict:** Steam is the **cheapest and least restrictive** — $100 once, no annual fee, instant updates after release. The 3–5 day initial review is shorter than the potential 3+ week timeline for Google Play (including the 14-day testing gate). Apple's $99/year recurring cost and mandatory review for every update adds ongoing overhead.

---

## 5. Performance Considerations

### PC

- **No battery constraint** — full CPU/GPU performance available.
- **Draw calls:** Less urgent; modern GPUs handle thousands easily.
- **Poly count:** Effectively unlimited for a 2D puzzle game.
- **Frame rate:** 60+ FPS easily achievable even on integrated graphics.
- **Unity optimization page** targets mobile specifically for draw call concerns; desktop recommendations are looser.  
  **Source:** [Unity Manual — Optimizing draw calls](https://docs.unity3d.com/Manual/optimizing-draw-calls.html)  
  **Source:** [Unity Manual — Reduce rendering work](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)

### Mobile

- **Battery & thermal throttling:** Major concern — Unity recommends 30 FPS default for mobile. "Reducing the rendering frame rate prevents unnecessary power usage, prolongs battery life, and prevents device temperature from rising."  
  **Source:** [Unity Manual — Reduce rendering work](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)

- **Draw calls:** Must be aggressively optimized. Use SRP Batcher, GPU instancing, and batching — "Optimizing draw calls reduces the amount of electricity your application needs. For battery-powered devices, this reduces the heat the device produces and the rate at which batteries run out."  
  **Source:** [Unity Manual — Introduction to optimizing draw calls](https://docs.unity3d.com/6000.5/Documentation/Manual/optimizing-draw-calls.html)

- **Shaders:** Use Mobile or Unlit shader categories. "They work on non-mobile platforms as well, but are simplified and approximated versions."  
  **Source:** [Unity Manual — Reduce rendering work](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)

- **Unity blog on mobile optimization:** "Mobile projects must balance frame rates against battery life and thermal throttling. Instead of pushing the limits at 60 fps, consider running at 30 fps."  
  **Source:** [Unity Blog — Optimize your mobile game performance](https://unity.com/blog/games/optimize-your-mobile-game-performance-expert-tips-on-graphics-and-assets)

**Verdict:** For a 2D sliding-block puzzle, performance pressure is **low overall**, but mobile still requires attention to draw calls, shader complexity, and battery impact. PC is effectively worry-free.

---

## 6. Build Tooling — IL2CPP, Gradle, etc.

### PC (Standalone)

- **Scripting backend:** Mono (default) or IL2CPP (optional).
- **No external build tools required.** Build is produced directly by the Unity Editor.
- **No cross-compilation restrictions** — build Windows from Windows, macOS from macOS, Linux from any desktop (including Windows via IL2CPP cross-compiler).  
  **Source:** [Unity Manual — Introduction to IL2CPP](https://docs.unity3d.com/6000.2/Documentation/Manual/il2cpp-introduction.html)

### Mobile

- **iOS:** IL2CPP is required (iOS does not allow JIT compilation). Build must be done on macOS. Unity generates an Xcode project; final `.ipa` requires Xcode command-line tools or Xcode GUI.  
  **Source:** [Unity Manual — IL2CPP Overview](https://docs.unity3d.com/2023.2/Documentation/Manual/IL2CPP.html)

- **Android:** Gradle is required. Unity generates a Gradle project then runs Gradle to produce `.apk`/`.aab`. Exporting to Android Studio is an option for advanced control. JDK, Android SDK, and NDK must be installed.  
  **Source:** [Unity Manual — Gradle for Android](https://docs.unity3d.com/6000.5/Documentation/Manual/android-gradle-overview.html)  
  **Source:** [Unity Manual — How Unity builds Android applications](https://docs.unity3d.com/6000.0/Documentation/Manual/how-unity-builds-android-applications.html)

**Verdict:** PC builds are a single click. iOS builds require macOS + Xcode; Android builds require Gradle + Android SDK. CI/CD setup is **significantly more complex** for mobile.

---

## 7. Testing Complexity — Device Fragmentation

### PC

- **Standardized hardware:** x86/x86-64 CPUs, Windows/macOS/Linux with well-defined APIs.
- **OS versions:** 3 actively supported Windows versions, 2–3 macOS versions. Minimal behavioral differences.
- **Screen resolutions:** Wide but well-understood; letterboxing is trivial.
- **Testing matrix:** 1–2 OS versions per target, one architecture per build.
- **No emulator gap** — tests run on the actual target hardware.

### Mobile

- **Android fragmentation:** 24,000+ distinct device models, 7+ active Android versions, OEM skins (Samsung One UI, Xiaomi MIUI, OnePlus OxygenOS) that change rendering, animation timing, and system behavior.  
  **Source:** [Mobile Device Fragmentation Testing](https://www.drizz.dev/post/mobile-device-fragmentation-testing-strategy)  
  **Source:** [Kobiton — Understanding Mobile Device Fragmentation](https://kobiton.com/blog/understanding-mobile-device-fragmentation/)

- **iOS fragmentation:** 20+ active models running 4–5 concurrent OS versions. Less severe than Android, but still requires testing across screen sizes and chip generations.  
  **Source:** [Mobile Device Fragmentation Testing](https://www.drizz.dev/post/mobile-device-fragmentation-testing-strategy)

- **Emulator limitation:** Emulators miss 15–20% of device-specific bugs. Cannot replicate thermal throttling, OEM-specific hardware behavior, or manufacturer customizations.  
  **Source:** [Mobile App Testing in 2026](https://globalbit.co.il/blog/mobile-app-testing-guide-2026)  
  **Source:** [Why Testing on One Device Is a Risky Strategy](https://www.testdevlab.com/blog/why-testing-one-device-risky-strategy)

- **Real device testing cost:** Cloud farms (AWS Device Farm, Firebase Test Lab) provide access but cost per minute. Maintaining an in-house device pool of 20+ devices can cost thousands upfront and hundreds per year per device. One large-scale device farm cost $1M+ to build and $0.6M/year to maintain.  
  **Source:** [Virtual Device Farms for Mobile App Testing at Scale](https://www.linhao.me/pdf/mobicom23-virtual_device_testing.pdf)

**Verdict:** This is the **largest cost and complexity multiplier** for mobile. A PC puzzle game in 2026 can be tested on 2–3 machines. Android alone requires 15–20 devices for even 80% coverage.

---

## 8. Unity Licensing — Platform Impact on Cost

Unity's licensing is based on **revenue/funding thresholds, not platforms**. There is no per-platform cost:

| Tier | Threshold | Cost | Key details |
|---|---|---|---|
| **Personal** | Under $200K revenue | **Free** | Splash screen required; no dark mode; all platforms supported |
| **Pro** | $200K–$25M | **$2,310/yr** per seat | Removes splash screen; priority support; closed platform access (consoles, Apple Vision Pro) |
| **Enterprise** | Over $25M | Custom (~$4,000–$5,000+/seat) | Dedicated support; source code access; custom terms |

**Sources:**  
[Unity Pricing Updates 2026](https://unity.com/products/pricing-updates)  
[Which Unity Editor License to Use](https://support.unity.com/hc/en-us/articles/28114350573460-Which-Unity-Editor-license-should-I-use-purchase)  
[Unity Plans & Pricing](https://unity.com/products/old-experiment)  
[Unity Editor Software Terms](https://unity.com/legal/editor-terms-of-service/software)

**Important:** Building for **closed platforms** (PlayStation, Xbox, Nintendo Switch — NOT PC or mobile) **does require Unity Pro**. Mobile (iOS/Android) and PC (Windows/macOS/Linux) are **open platforms** available on all tiers.  
**Source:** [Unity Pro page](https://unity.com/products/unity-pro) ("Build and deploy to closed platforms such as Nintendo Switch, PlayStation, and Xbox. An active Unity Pro subscription is required.")

**Verdict:** Platform does not affect Unity license tier or cost. Both PC and mobile are equally accessible on Unity Personal (free).

---

## 9. Overall Cost Comparison Matrix

| Factor | PC-First (Steam) | Mobile-First (iOS + Android) |
|---|---|---|
| **Unity license** | Free (Personal) | Free (Personal) |
| **Developer account** | $100 one-time (Steam) | $99/year (Apple) + $25 one-time (Google) |
| **Build tools setup** | None | macOS required (iOS), Android SDK + Gradle (Android) |
| **CI/CD complexity** | Low | High (macOS runner for iOS, Gradle for Android) |
| **Asset optimization** | Minimal (no file size limit) | Required (store size limits, GPU constraints) |
| **Performance work** | Near zero for 2D puzzle | Moderate (draw calls, shaders, battery) |
| **Input system work** | Mouse click/drag (trivial) | Tap/drag (trivial, well-supported) |
| **Testing cost** | ~2–3 machines | 15–20+ devices or cloud farm ($) |
| **App review time (first)** | 3–5 business days | 1–7 days (Apple) + 14-day testing gate (Google) |
| **App review time (updates)** | **Instant** | 24–48 hours (Apple), hours (Google) |
| **Revenue share** | 30% flat (first $10M) | 15–30% (both tiered) |
| **Ongoing platform cost** | $0 | $99/year (Apple) |

---

## Key Findings

**1. PC-first is significantly cheaper and simpler.** A 2D puzzle game like "Parking Jam" faces negligible performance pressure on desktop, requires no extra SDKs or build toolchains, has zero ongoing platform fees (after $100 Steam Direct), and can be tested on a handful of machines rather than the 20+ devices needed for mobile coverage.

**2. Mobile adds substantial non-recoupable overhead.** iOS requires a macOS build machine and a $99/year developer fee that never disappears; Android's 14-day closed testing gate for new accounts and the device fragmentation tax (24,000+ models) inflate QA cost and timeline before a single dollar is earned.

**3. Unity's Input System and licensing are neutral — they do not favour either platform.** The Input System abstracts keyboard/mouse vs touch uniformly, and Unity Personal (free) supports both PC and mobile equally. The cost difference comes entirely from platform-ecosystem requirements (store fees, build tooling, testing, review gates), not from Unity itself.
