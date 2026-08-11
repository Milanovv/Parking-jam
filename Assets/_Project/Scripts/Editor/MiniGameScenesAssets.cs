using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MiniGameScenesAssets
{
    public const int SceneCount = 9;

    private static readonly MiniGameType[] Types =
    {
        MiniGameType.Pipes, MiniGameType.Pattern, MiniGameType.Memory
    };

    private static readonly MiniGameDifficulty[] Difficulties =
    {
        MiniGameDifficulty.Easy, MiniGameDifficulty.Medium, MiniGameDifficulty.Hard
    };

    private static bool _ensuring;

    public static void Ensure()
    {
        if (_ensuring) return;
        _ensuring = true;
        try
        {
            EnsureFolder(MiniGameCatalog.ScenesFolder);
            foreach (var type in Types)
            {
                foreach (var difficulty in Difficulties)
                    EnsureScene(type, difficulty);
            }
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

    private static void EnsureScene(MiniGameType type, MiniGameDifficulty difficulty)
    {
        string path = MiniGameCatalog.ScenePath(type, difficulty);
        string controllerName = MiniGameCatalog.ControllerTypeName(type);
        if (File.Exists(path) && File.ReadAllText(path).Contains(controllerName)) return;

        var original = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var root = new GameObject("MiniGame");
        switch (type)
        {
            case MiniGameType.Pipes:
            {
                var controller = root.AddComponent<PipeMiniGameController>();
                controller.Spec = MiniGameCatalog.Pipes(difficulty);
                break;
            }
            case MiniGameType.Pattern:
            {
                var controller = root.AddComponent<PatternMiniGameController>();
                controller.Spec = MiniGameCatalog.Pattern(difficulty);
                break;
            }
            default:
            {
                var controller = root.AddComponent<MemoryMiniGameController>();
                controller.Spec = MiniGameCatalog.Memory(difficulty);
                break;
            }
        }

        var canvasHost = new GameObject("MiniGameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasHost.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasHost.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        var eventSystemHost = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

        EditorSceneManager.SaveScene(scene, path);
        if (!string.IsNullOrEmpty(original) && File.Exists(original) && EditorSceneManager.GetActiveScene() == scene)
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        EditorSceneManager.CloseScene(scene, true);

        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}