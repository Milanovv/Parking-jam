using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MiniGameScenesAssetsTests
{
    private static readonly MiniGameType[] Types =
    {
        MiniGameType.Pipes, MiniGameType.Pattern, MiniGameType.Memory
    };

    private static readonly MiniGameDifficulty[] Difficulties =
    {
        MiniGameDifficulty.Easy, MiniGameDifficulty.Medium, MiniGameDifficulty.Hard
    };

    [Test]
    public void EveryScene_Exists_WithControllerCanvasAndEventSystem()
    {
        foreach (var type in Types)
        {
            foreach (var difficulty in Difficulties)
            {
                string path = MiniGameCatalog.ScenePath(type, difficulty);
                Assert.IsTrue(File.Exists(path), path + " must be composed on disk");

                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                Assert.AreEqual(3, roots.Length, path + " carries the controller root, canvas and event system");

                string controllerName = MiniGameCatalog.ControllerTypeName(type);
                Assert.IsTrue(roots.Any(root => root.GetComponent(controllerName) != null),
                    path + " carries its " + controllerName);
                Assert.IsTrue(roots.Any(root => root.GetComponent<Canvas>() != null), path + " carries a canvas");
                Assert.IsTrue(roots.Any(root => root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null),
                    path + " carries its own event system");
            }
        }
    }

    [Test]
    public void PipesControllers_AreConfiguredWithTheDesignTable()
    {
        AssertPipe(MiniGameDifficulty.Easy, MiniGameCatalog.Pipes(MiniGameDifficulty.Easy));
        AssertPipe(MiniGameDifficulty.Medium, MiniGameCatalog.Pipes(MiniGameDifficulty.Medium));
        AssertPipe(MiniGameDifficulty.Hard, MiniGameCatalog.Pipes(MiniGameDifficulty.Hard));
    }

    private static void AssertPipe(MiniGameDifficulty difficulty, PipeSpec expected)
    {
        string path = MiniGameCatalog.ScenePath(MiniGameType.Pipes, difficulty);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
        var controller = scene.GetRootGameObjects()
            .Select(root => root.GetComponent<PipeMiniGameController>())
            .FirstOrDefault(c => c != null);

        Assert.IsNotNull(controller);
        Assert.AreEqual(expected.Width, controller.Spec.Width);
        Assert.AreEqual(expected.Height, controller.Spec.Height);
        Assert.AreEqual(expected.RotatableTiles, controller.Spec.RotatableTiles);
        Assert.AreEqual(expected.TimeLimitSeconds, controller.Spec.TimeLimitSeconds);
        Assert.AreEqual(expected.Hints, controller.Spec.Hints);
    }

    [Test]
    public void PatternControllers_AreConfiguredWithTheDesignTable()
    {
        AssertPattern(MiniGameDifficulty.Easy, MiniGameCatalog.Pattern(MiniGameDifficulty.Easy));
        AssertPattern(MiniGameDifficulty.Medium, MiniGameCatalog.Pattern(MiniGameDifficulty.Medium));
        AssertPattern(MiniGameDifficulty.Hard, MiniGameCatalog.Pattern(MiniGameDifficulty.Hard));
    }

    private static void AssertPattern(MiniGameDifficulty difficulty, PatternSpec expected)
    {
        string path = MiniGameCatalog.ScenePath(MiniGameType.Pattern, difficulty);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
        var controller = scene.GetRootGameObjects()
            .Select(root => root.GetComponent<PatternMiniGameController>())
            .FirstOrDefault(c => c != null);

        Assert.IsNotNull(controller);
        Assert.AreEqual(expected.ButtonCount, controller.Spec.ButtonCount);
        Assert.AreEqual(expected.SequenceLength, controller.Spec.SequenceLength);
        Assert.AreEqual(expected.FlashSeconds, controller.Spec.FlashSeconds);
    }

    [Test]
    public void MemoryControllers_AreConfiguredWithTheDesignTable()
    {
        AssertMemory(MiniGameDifficulty.Easy, MiniGameCatalog.Memory(MiniGameDifficulty.Easy));
        AssertMemory(MiniGameDifficulty.Medium, MiniGameCatalog.Memory(MiniGameDifficulty.Medium));
        AssertMemory(MiniGameDifficulty.Hard, MiniGameCatalog.Memory(MiniGameDifficulty.Hard));
    }

    private static void AssertMemory(MiniGameDifficulty difficulty, MemorySpec expected)
    {
        string path = MiniGameCatalog.ScenePath(MiniGameType.Memory, difficulty);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
        var controller = scene.GetRootGameObjects()
            .Select(root => root.GetComponent<MemoryMiniGameController>())
            .FirstOrDefault(c => c != null);

        Assert.IsNotNull(controller);
        Assert.AreEqual(expected.Pairs, controller.Spec.Pairs);
        Assert.AreEqual(expected.Width, controller.Spec.Width);
        Assert.AreEqual(expected.Height, controller.Spec.Height);
        Assert.AreEqual(expected.MoveLimit, controller.Spec.MoveLimit);
    }

    [Test]
    public void Ensure_IsIdempotent_SecondPassLeavesScenesComplete()
    {
        MiniGameScenesAssets.Ensure();
        MiniGameScenesAssets.Ensure();

        foreach (var type in Types)
        {
            foreach (var difficulty in Difficulties)
            {
                string path = MiniGameCatalog.ScenePath(type, difficulty);
                Assert.IsTrue(File.Exists(path), path + " survives repeated Ensure passes");
            }
        }
    }
}