using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneAssetsTests
{
    [Test]
    public void MainScene_CarriesTheFullPlayableGameRoot()
    {
        string path = GameSceneAssets.MainScenePath;
        Assert.IsTrue(File.Exists(path), path + " must be composed on disk");

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var roots = scene.GetRootGameObjects();

        var gridRoot = RequireRoot(roots, GameSceneAssets.GridRootName);
        Assert.IsNotNull(gridRoot.GetComponent<Grid>(), "The grid root carries the Unity Grid component");
        Assert.AreEqual(new Vector3(1f, 1f, 0f), gridRoot.GetComponent<Grid>().cellSize, "One unit per tile");
        Assert.IsNotNull(gridRoot.GetComponent<GridController>(), "The grid root carries the GridController");

        var gameRoot = RequireRoot(roots, GameSceneAssets.GameRootName);
        var manager = gameRoot.GetComponent<GameManager>();
        Assert.IsNotNull(manager, "GameRoot carries GameManager");
        var managerSo = new SerializedObject(manager);
        Assert.AreEqual(gridRoot.GetComponent<GridController>(), managerSo.FindProperty("_gridController").objectReferenceValue,
            "GameManager is wired to the scene grid");

        var input = RequireRoot(roots, GameSceneAssets.InputRootName).GetComponent<InputHandler>();
        Assert.IsNotNull(input, "Input root carries the drag handler");
        var inputSo = new SerializedObject(input);
        Assert.AreEqual(gridRoot.GetComponent<GridController>(), inputSo.FindProperty("_gridController").objectReferenceValue,
            "The drag handler is wired to the scene grid");

        var launcherRoot = RequireRoot(roots, GameSceneAssets.LauncherRootName);
        var launcher = launcherRoot.GetComponent<GameLauncher>();
        Assert.IsNotNull(launcher, "The launch seam lives in the scene");
        var launcherSo = new SerializedObject(launcher);
        Assert.AreEqual(1, launcherSo.FindProperty("_levelId").intValue, "The scene starts the first authored level");
        Assert.IsTrue(launcherSo.FindProperty("_autoStartOnPlay").boolValue, "Press Play boots the level directly");
        Assert.AreEqual(gridRoot.GetComponent<GridController>(), launcherSo.FindProperty("_gridController").objectReferenceValue);
        Assert.AreEqual(manager, launcherSo.FindProperty("_gameManager").objectReferenceValue);
        Assert.IsNotNull(launcherSo.FindProperty("_sessionStats").objectReferenceValue, "The launcher resets the session stats");
        Assert.IsNotNull(launcherSo.FindProperty("_carPrefab").objectReferenceValue, "The launcher carries the car prefab");
        Assert.IsNotNull(launcherSo.FindProperty("_truckPrefab").objectReferenceValue, "The launcher carries the truck prefab");
        Assert.IsNotNull(launcherSo.FindProperty("_busPrefab").objectReferenceValue, "The launcher carries the bus prefab");
        Assert.IsNotNull(launcherSo.FindProperty("_pedestrianPrefab").objectReferenceValue, "The launcher carries the pedestrian prefab");
        Assert.IsNotNull(launcherSo.FindProperty("_barrierPrefab").objectReferenceValue, "The launcher carries the barrier prefab");

        var session = RequireRoot(roots, GameSceneAssets.SessionRootName).GetComponent<LevelSessionStats>();
        Assert.IsNotNull(session, "The session stats live in the scene");

        var economy = RequireRoot(roots, GameSceneAssets.EconomyRootName).GetComponent<EconomyManager>();
        Assert.IsNotNull(economy, "The save-backed economy lives in the scene");

        var canvasRoot = RequireRoot(roots, GameSceneAssets.CanvasRootName);
        var canvas = canvasRoot.GetComponent<Canvas>();
        Assert.IsNotNull(canvas, "A root canvas exists");
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "The root canvas is overlay");
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        Assert.IsNotNull(scaler, "The root canvas scales with the screen");
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
        Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution, "1080x1920 reference resolution");
        Assert.AreEqual(1f, scaler.matchWidthOrHeight, "Height-based scaling");
        Assert.IsNotNull(canvasRoot.GetComponent<GraphicRaycaster>(), "The root canvas raycasts");

        var hud = canvasRoot.GetComponentInChildren<LevelHud>(true);
        Assert.IsNotNull(hud, "A minimal moves/timer HUD is present");
        Assert.IsNotNull(hud.MovesText, "The moves counter label is wired");
        Assert.IsNotNull(hud.TimerText, "The timer label is wired");
        Assert.AreEqual(session, hud.Stats, "The HUD reads the session stats");

        var eventSystem = RequireRoot(roots, GameSceneAssets.EventSystemRootName).GetComponent<EventSystem>();
        Assert.IsNotNull(eventSystem, "An EventSystem stands in the scene");
        Assert.IsNotNull(eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(),
            "The EventSystem is driven by the Input System");
    }

    [Test]
    public void Ensure_IsIdempotent_SecondPassLeavesTheSceneUntouched()
    {
        string before = File.ReadAllText(GameSceneAssets.MainScenePath);
        GameSceneAssets.Ensure();
        GameSceneAssets.Ensure();
        string after = File.ReadAllText(GameSceneAssets.MainScenePath);

        Assert.AreEqual(before, after, "Repeated Ensure passes make no edits");
        StringAssert.Contains("ParkingJam::GameManager", after, "The scene carries the composed game root");
    }

    [Test]
    public void MainScene_IsTheEnabledHeadSceneInBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotEmpty(scenes);
        Assert.IsTrue(scenes[0].enabled, "Main is the enabled head scene");
        StringAssert.EndsWith("Assets/Scenes/Main.unity", scenes[0].path);
    }

    private static GameObject RequireRoot(GameObject[] roots, string name)
    {
        var root = roots.FirstOrDefault(candidate => candidate.name == name);
        Assert.IsNotNull(root, name + " root exists in the scene");
        return root;
    }
}