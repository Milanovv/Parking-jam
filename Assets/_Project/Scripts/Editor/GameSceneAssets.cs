using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSceneAssets
{
    public const string MainScenePath = "Assets/Scenes/Main.unity";

    public const string GridRootName = "Grid";
    public const string GameRootName = "GameRoot";
    public const string InputRootName = "Input";
    public const string LauncherRootName = "GameLauncher";
    public const string SessionRootName = "GameSession";
    public const string EconomyRootName = "Economy";
    public const string CanvasRootName = "Canvas";
    public const string EventSystemRootName = "EventSystem";
    public const string GameUiRootName = "GameUi";
    public const string SkinRootName = "SkinController";

    private const string GameManagerMarker = "ParkingJam::GameManager";
    private const string GameUiMarker = "ParkingJam::GameUiController";

    private static bool _ensuring;

    public static void Ensure()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsureScene();
        }
        finally
        {
            _ensuring = false;
        }
    }

    [InitializeOnLoadMethod]
    private static void EnsureOnLoad()
    {
        Ensure();
    }

    public static void EnsureScene()
    {
        if (!File.Exists(MainScenePath)) return;
        string content = File.ReadAllText(MainScenePath);
        bool needsCore = !content.Contains(GameManagerMarker);
        bool needsUi = !content.Contains(GameUiMarker);
        if (!needsCore && !needsUi) return;

        string original = SceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

        if (needsCore)
        {
            GridController grid = EnsureGrid(scene);
            GameObject inputGo = EnsureInput(scene, grid).gameObject;
            GameManager gameManager = EnsureGameManager(scene, grid, inputGo);
            LevelSessionStats session = EnsureSession(scene);
            EnsureLauncher(scene, grid, gameManager, inputGo, session);
            EnsureEconomy(scene);
            EnsureCanvas(scene, session);
            EnsureEventSystem(scene);
            RepositionShowcasePedestrian(scene);
        }

        if (needsUi)
        {
            EnsureGameUi(scene);
            EnsureSkinController(scene);
            EnsureHudElements(scene);
        }

        EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(original) && File.Exists(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    private static void EnsureGameUi(Scene scene)
    {
        GameObject go = FindRoot(scene, GameUiRootName);
        if (go == null)
        {
            go = new GameObject(GameUiRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        if (go.GetComponent<GameUiController>() == null) go.AddComponent<GameUiController>();
    }

    private static void EnsureSkinController(Scene scene)
    {
        GameObject go = FindRoot(scene, SkinRootName);
        if (go == null)
        {
            go = new GameObject(SkinRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var controller = go.GetComponent<SkinController>();
        if (controller == null) controller = go.AddComponent<SkinController>();

        CarPackAssets.Ensure();
        var names = CarPackAssets.PaintNames;
        var slots = new SkinController.PaintSlot[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            slots[i] = new SkinController.PaintSlot
            {
                skinId = names[i],
                material = CarPackAssets.PaintMaterial(names[i])
            };
        }
        controller.Paints = slots;
    }

    private static void EnsureHudElements(Scene scene)
    {
        var canvasRoot = FindRoot(scene, CanvasRootName);
        if (canvasRoot == null) return;
        var sessionRoot = FindRoot(scene, SessionRootName);
        var session = sessionRoot != null ? sessionRoot.GetComponent<LevelSessionStats>() : null;
        EnsureHud(canvasRoot, session);
    }

    private static GridController EnsureGrid(Scene scene)
    {
        GameObject go = FindRoot(scene, GridRootName);
        if (go == null)
        {
            go = new GameObject(GridRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var grid = go.GetComponent<Grid>();
        if (grid == null) grid = go.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var controller = go.GetComponent<GridController>();
        if (controller == null) controller = go.AddComponent<GridController>();
        return controller;
    }

    private static GameManager EnsureGameManager(Scene scene, GridController grid, GameObject inputGo)
    {
        GameObject go = FindRoot(scene, GameRootName);
        if (go == null)
        {
            go = new GameObject(GameRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var manager = go.GetComponent<GameManager>();
        if (manager == null) manager = go.AddComponent<GameManager>();

        var serialized = new SerializedObject(manager);
        serialized.FindProperty("_gridController").objectReferenceValue = grid;
        serialized.FindProperty("_inputHandler").objectReferenceValue = inputGo != null ? inputGo.GetComponent<InputHandler>() : null;
        serialized.ApplyModifiedProperties();
        return manager;
    }

    private static InputHandler EnsureInput(Scene scene, GridController grid)
    {
        GameObject go = FindRoot(scene, InputRootName);
        if (go == null)
        {
            go = new GameObject(InputRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var input = go.GetComponent<InputHandler>();
        if (input == null) input = go.AddComponent<InputHandler>();

        var serialized = new SerializedObject(input);
        serialized.FindProperty("_gridController").objectReferenceValue = grid;
        serialized.ApplyModifiedProperties();
        return input;
    }

    private static LevelSessionStats EnsureSession(Scene scene)
    {
        GameObject go = FindRoot(scene, SessionRootName);
        if (go == null)
        {
            go = new GameObject(SessionRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var stats = go.GetComponent<LevelSessionStats>();
        if (stats == null) stats = go.AddComponent<LevelSessionStats>();
        return stats;
    }

    private static void EnsureLauncher(Scene scene, GridController grid, GameManager gameManager,
        GameObject inputGo, LevelSessionStats session)
    {
        GameObject go = FindRoot(scene, LauncherRootName);
        if (go == null)
        {
            go = new GameObject(LauncherRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var launcher = go.GetComponent<GameLauncher>();
        if (launcher == null) launcher = go.AddComponent<GameLauncher>();

        var rig = Object.FindFirstObjectByType<GameCameraRig>();

        var serialized = new SerializedObject(launcher);
        serialized.FindProperty("_levelId").intValue = 1;
        serialized.FindProperty("_autoStartOnPlay").boolValue = true;
        serialized.FindProperty("_gridController").objectReferenceValue = grid;
        serialized.FindProperty("_gameManager").objectReferenceValue = gameManager;
        serialized.FindProperty("_sessionStats").objectReferenceValue = session;
        serialized.FindProperty("_cameraRig").objectReferenceValue = rig;
        serialized.FindProperty("_carPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.PrefabPath);
        serialized.FindProperty("_truckPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.TruckPrefabPath);
        serialized.FindProperty("_busPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(CarPackAssets.BusPrefabPath);
        serialized.FindProperty("_pedestrianPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(PeoplePackAssets.PedestrianPrefabPath);
        serialized.FindProperty("_barrierPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(BarrierAssets.BarrierPrefabPath);
        serialized.ApplyModifiedProperties();
    }

    private static void EnsureEconomy(Scene scene)
    {
        GameObject go = FindRoot(scene, EconomyRootName);
        if (go == null)
        {
            go = new GameObject(EconomyRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        if (go.GetComponent<EconomyManager>() == null) go.AddComponent<EconomyManager>();
    }

    private static void EnsureCanvas(Scene scene, LevelSessionStats session)
    {
        GameObject go = FindRoot(scene, CanvasRootName);
        if (go == null)
        {
            go = new GameObject(CanvasRootName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        var canvas = go.GetComponent<Canvas>();
        if (canvas == null) canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = go.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();

        EnsureHud(go, session);
    }

    private static void EnsureHud(GameObject canvasRoot, LevelSessionStats session)
    {
        var hudGo = canvasRoot.transform.Find("HUD")?.gameObject;
        if (hudGo == null)
        {
            hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvasRoot.transform, false);
            var rect = hudGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        var hud = hudGo.GetComponent<LevelHud>();
        if (hud == null) hud = hudGo.AddComponent<LevelHud>();

        hud.Stats = session;
        hud.MovesText = EnsureHudText(hudGo, "MovesText", "Moves: 0", new Vector2(0f, 1f), new Vector2(24f, -24f), 36);
        hud.TimerText = EnsureHudText(hudGo, "TimerText", "0:00", new Vector2(1f, 1f), new Vector2(-24f, -24f), 36);
        hud.UndosText = EnsureHudText(hudGo, "UndosText", "Undos: 0", new Vector2(0f, 0f), new Vector2(24f, 24f), 36);
        hud.CoinsText = EnsureHudText(hudGo, "CoinsText", "0", new Vector2(1f, 0f), new Vector2(-24f, 24f), 36);
        hud.KeysText = EnsureHudText(hudGo, "KeysText", "0", new Vector2(1f, 0f), new Vector2(-160f, 24f), 36);
        hud.CoinSkipButton = EnsureHudButton(hudGo, "CoinSkipButton", "Skip", new Vector2(0.5f, 0f), new Vector2(0f, 24f));
        hud.PauseButton = EnsureHudButton(hudGo, "PauseButton", "II", new Vector2(0.5f, 1f), new Vector2(0f, -24f));
    }

    private static Text EnsureHudText(GameObject parent, string name, string initial, Vector2 anchor, Vector2 offset, int fontSize)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.GetComponent<Text>();
        return CreateHudText(name, initial, parent.transform, anchor, offset, fontSize);
    }

    private static Button EnsureHudButton(GameObject parent, string name, string label, Vector2 anchor, Vector2 offset)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.GetComponent<Button>();

        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(200f, 64f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.18f, 0.18f, 0.20f, 0.9f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = BuiltinFont();
        return button;
    }

    private static Text CreateHudText(string name, string initial, Transform parent, Vector2 anchor, Vector2 offset, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(320f, 64f);

        var text = go.GetComponent<Text>();
        text.text = initial;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.font = BuiltinFont();
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static Font BuiltinFont()
    {
        foreach (var name in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(name);
            }
            catch
            {
                // try the next built-in font name
            }
        }
        return null;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        GameObject go = FindRoot(scene, EventSystemRootName);
        if (go == null)
        {
            go = new GameObject(EventSystemRootName);
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        if (go.GetComponent<EventSystem>() == null) go.AddComponent<EventSystem>();
        if (go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private static void RepositionShowcasePedestrian(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "Pedestrian") continue;
            if (root.transform.position == Vector3.zero)
                root.transform.position = new Vector3(-3f, -3f, 0f);
            return;
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }
}