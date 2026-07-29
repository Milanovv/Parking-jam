# Level Authoring Approaches for Parking Jam — Easiest Path

A comparison of six ways to author 25–30 puzzle levels for a 2D sliding-block game where data lives as JSON in `StreamingAssets`.

---

## 1. Hand-authoring JSON

Just type the `.json` file. Any text editor works.

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~5 min (copy the schema, fill in values) |
| **Learning curve** | None if you know JSON syntax. The schema is flat and small. |
| **QoL for 25 levels** | Repetitive — no grid preview, no drag-to-place. Easy to mis-count tile coordinates. |
| **Tweaking/balancing** | Open file, tweak numbers, re-run. Fast iteration for numeric changes (move limit, undos). Awkward for spatial layout changes. |
| **Reusability** | Best-in-class — the file **is** the runtime format. No conversion step, no build step. Drop a `.json` in `StreamingAssets/` and it loads. |

**Primary sources:**
- Unity Manual — StreamingAssets: https://docs.unity3d.com/Manual/StreamingAssets.html
- Unity Scripting API — `JsonUtility.FromJson`: https://docs.unity3d.com/ScriptReference/JsonUtility.FromJson.html

---

## 2. Unity EditorWindow (IMGUI)

A custom `EditorWindow` with `OnGUI()` and `OnSceneGUI()` for a grid-based level editor.

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~4–6 h to write a functional grid editor (draw grid, click to place, export JSON). The manual's "Create custom Editor Windows with IMGUI" entry provides a ~30-line sample to get a window on screen (Unity Docs: [editor-EditorWindows](https://docs.unity3d.com/Manual/editor-EditorWindows.html)). |
| **Learning curve** | Moderate. Must learn `EditorWindow`, `OnGUI`/`GUILayout`, `Handles` for scene drawing, `SceneView.duringSceneGui` delegate. The manual walks through the basics but a drag-to-place grid with multi-cell vehicles is non-trivial. Community examples (e.g., GridPaintEditor gist) show it's ~200–300 lines of IMGUI code for a functional version. |
| **QoL for 25 levels** | High once built — click-to-place vehicles on a visible grid, instant preview. But you're writing and debugging an editor, not just levels. |
| **Tweaking/balancing** | Good — visual feedback catches layout errors immediately. |
| **Reusability** | Good — the editor can be reused for future content packs. However, the output should still be JSON (or ScriptableObject) so the runtime format stays decoupled. |

**Why Unity now steers away from it:** The manual says in bold: *"It's strongly recommended to use the UI Toolkit to extend the Unity Editor"* (Unity Docs: [editor-EditorWindows](https://docs.unity3d.com/Manual/editor-EditorWindows.html)). IMGUI is in maintenance mode.

**Primary sources:**
- Unity Manual — Create custom Editor Windows with IMGUI: https://docs.unity3d.com/Manual/editor-EditorWindows.html
- Unity Scripting API — EditorWindow: https://docs.unity3d.com/ScriptReference/EditorWindow.html
- Community example (GridPaintEditor gist): https://gist.github.com/fangzhangmnm/bdb16f3970c2158c3bb829bf2685bb94

---

## 3. Unity Custom Inspector (IMGUI) on a ScriptableObject

Create a `LevelData : ScriptableObject` with `[CreateAssetMenu]`, then write a custom `Editor` that overrides `OnInspectorGUI()` to show a grid preview.

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~3–4 h. You get the default Inspector for free (all fields editable), then add a grid preview on top. The custom Editor pattern is simple: `[CustomEditor(typeof(LevelData))]` + override `OnInspectorGUI()` (Unity Docs: [editor-CustomEditors](https://docs.unity3d.com/Manual/editor-CustomEditors.html)). |
| **Learning curve** | Low-to-moderate. The `Editor` class is straightforward. Adding a grid preview in `OnInspectorGUI()` is tricky because the Inspector is cramped — you'd typically draw a preview via `GUILayoutUtility.GetRect` + `GUI.BeginGroup` or switch to `OnPreviewGUI()`. |
| **QoL for 25 levels** | Medium. You can edit fields and see the grid in the Inspector, but the Inspector is small (~300px wide). You can't drag in the Scene view from here — you'd need a companion EditorWindow anyway. |
| **Tweaking/balancing** | Decent — numeric fields are right there, but spatial layout changes are still coordinate-entry. |
| **Reusability** | Locked to the Unity Editor. The `.asset` file can't be edited outside Unity, which prevents community tools or designer workflows. |

**Primary sources:**
- Unity Manual — Create custom Editors with IMGUI: https://docs.unity3d.com/Manual/editor-CustomEditors.html
- Unity Manual — Use Property Drawers with IMGUI: https://docs.unity3d.com/Manual/editor-PropertyDrawers.html
- Unity Manual — ScriptableObject: https://docs.unity3d.com/Manual/class-ScriptableObject.html

---

## 4. Unity UI Toolkit (Editor Window)

Build the level editor window using UI Toolkit (UXML + USS + C# events).

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~6–8 h. The UI Toolkit Editor Window tutorial shows a working window in 5 min (Unity Docs: [UIE-HowTo-CreateEditorWindow](https://docs.unity3d.com/Manual/UIE-HowTo-CreateEditorWindow.html)). But a grid editor needs custom `VisualElement` drawing (via `IMGUIContainer` or `MeshGenerationContext`) or embedding an `IMGUI` panel, which adds complexity. |
| **Learning curve** | Higher than IMGUI. You need UXML, USS, `VisualElement` tree, event callbacks, and the UI Builder tool. Unity's own comparison says IMGUI is still the alternative for *"unrestricted access to editor extensible capabilities"* and *"light API to quickly render"* (Unity Docs: [UI-system-compare](https://docs.unity3d.com/Manual/UI-system-compare.html)). |
| **QoL for 25 levels** | High once built — polished, resizable, modern UI. UI Toolkit supports `TwoPaneSplitView`, `ListView`, drag-and-drop (`PointerManipulator`). |
| **Tweaking/balancing** | Best-in-class — flexible layout, style sheets, responsive. But the upfront cost is steep. |
| **Reusability** | Good — the UXML/USS assets are reusable and UI Toolkit is Unity's strategic direction. |

**Why NOT to choose this for 25 levels:** Unity's recommendation is *"UI Toolkit for complex editor tools"*. A 25-level puzzle game is not a complex editor — it's a small data-entry task. The invest-to-save ratio is poor.

**Primary sources:**
- Unity Manual — Create a custom Editor window with UI Toolkit: https://docs.unity3d.com/Manual/UIE-HowTo-CreateEditorWindow.html
- Unity Manual — Create a drag-and-drop UI: https://docs.unity3d.com/Manual/UIE-create-drag-and-drop-ui.html
- Unity Manual — UI system comparison: https://docs.unity3d.com/Manual/UI-system-compare.html

---

## 5. Excel/CSV → Export to JSON

Write levels in a spreadsheet, export to CSV, convert to JSON via a custom script or tool like BakingSheet.

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~1 h to set up the spreadsheet + a C# converter script. BakingSheet's AssetPostProcessor example auto-converts Excel files to JSON whenever they change (GitHub: [cathei/BakingSheet](https://github.com/cathei/BakingSheet)). |
| **Learning curve** | Low. Spreadsheets are familiar. The hard part is representing multi-cell vehicles and obstacle routes in a flat table. You'd need one row per vehicle and encode the tiles/tile arrays in a single cell (e.g., `[[0,3],[1,3]]` as a string). |
| **QoL for 25 levels** | Medium — spreadsheet formulas can help validate data (e.g., no overlapping tiles). But you don't see the grid. |
| **Tweaking/balancing** | Good for numeric values (move limit, undos) — batch-editing across rows is a spreadsheet superpower. Awkward for spatial layout. |
| **Reusability** | Good. The source is an `.xlsx` file (human-readable, widely editable). An `AssetPostprocessor` auto-generates the JSON for `StreamingAssets`. |

**Primary sources:**
- Unity Manual — Text assets (CSV import): https://docs.unity3d.com/Manual/class-TextAsset.html
- Unity Manual — StreamingAssets: https://docs.unity3d.com/Manual/StreamingAssets.html
- BakingSheet (Excel → JSON for Unity): https://github.com/cathei/BakingSheet

---

## 6. Tiled Map Editor → JSON

Use the free [Tiled Map Editor](https://www.mapeditor.org/) to paint tile layers, place objects on an object layer, then export to JSON.

| Criterion | Assessment |
|-----------|------------|
| **Hours to first level** | ~2 h. Tiled has a rich UI and exports to JSON natively (File → Export As → JSON). The JSON format is well-documented (Tiled Docs: [JSON Map Format](https://doc.mapeditor.org/en/stable/reference/json-map-format/)). |
| **Learning curve** | Low. Tiled is purpose-built for 2D tile maps. You paint tiles for static obstacles, place objects for vehicles with custom properties (type, orientation, length, goal). However, Tiled's JSON is verbose — a ~6x6 grid level exports ~2 KB of tileset/layer metadata you don't need. You must write a Unity-side converter to map Tiled's JSON into your level schema. |
| **QoL for 25 levels** | Medium-high. Visual tile painting is fast. Tiled supports custom properties per object (add "orientation", "length", "isGoal" as custom fields). But you're fighting Tiled's tile-centric model for a game that doesn't use tiles visually — only the grid coordinates matter. |
| **Tweaking/balancing** | Excellent for spatial layout — drag and resize vehicles visually. Weak for metadata (move limits, undo counts) — those go in the map's custom properties, one level per file. |
| **Reusability** | Good — Tiled files are `.tmx` (XML) and export to `.json`. The Unity converter is a one-time cost. But you now maintain two formats: the source `.tmx` and the exported `.json`. |

**Primary sources:**
- Tiled Manual — JSON Map Format: https://doc.mapeditor.org/en/stable/reference/json-map-format/
- Tiled Manual — Export: https://doc.mapeditor.org/en/stable/manual/export/
- Tiled Manual — Custom Properties: https://doc.mapeditor.org/en/stable/manual/custom-properties/

---

## Comparison Matrix

| Approach | Hours to L1 | Learning curve | QoL 25 levels | Tweaking | Reusable | Runtime format |
|---|---|---|---|---|---|---|
| **Hand-author JSON** | **0.1 h** | None | ★☆☆ Repetitive | ★☆☆ Coordinate math | ★★★ Direct | JSON ✓ |
| EditorWindow (IMGUI) | 4–6 h | Moderate | ★★★ Visual | ★★★ Visual | ★★☆ Editors only | JSON output |
| Custom Inspector (SO) | 3–4 h | Low-mod | ★★☆ Cramped | ★★☆ Mixed | ★☆☆ Editor-locked | .asset |
| UI Toolkit Editor | 6–8 h | Higher | ★★★ Polished | ★★★ Visual | ★★☆ Editors only | JSON output |
| Excel/CSV → JSON | 1 h | Low | ★★☆ No grid | ★★☆ Spreadsheet | ★★★ Excel+JSON | JSON ✓ |
| Tiled → JSON | 2 h | Low | ★★★ Visual paint | ★★★ Tile editor | ★★☆ Two formats | JSON (needs convert) |

---

## Recommendation

**Hand-author JSON is the absolute easiest approach.** For 25 levels of a 6×6 sliding-block puzzle, you can type the first level in 5 minutes, no installs, no code, no conversion. Once the JSON schema is stable, copying and editing a file takes 2–3 minutes per level. The spatial coordinates are the only friction — but for a puzzle game where levels are designed on paper first, transcribing coordinates to JSON is faster than building a custom editor.

If coordinate-entry becomes tedious after ~10 levels, the second-easiest upgrade is **Tiled + a lightweight Unity converter script** — Tiled provides visual tile painting for free, and a ~50-line C# converter maps its JSON export to your schema.
