using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MiniGameFlowTests : PlayModeTestBase
{
    private GameManager _gameManager;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        PurgeStaleMiniGameObjects();

        var gmGo = new GameObject("GameManager");
        _gameManager = gmGo.AddComponent<GameManager>();

        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0), miniGameScene = "MiniGame_Pipes_Easy" } }
        });

        MiniGameManager.EnsureInstance();
        yield return null;
    }

    private static void PurgeStaleMiniGameObjects()
    {
        foreach (var controller in Object.FindObjectsByType<MiniGameController>(FindObjectsInactive.Include))
            Object.DestroyImmediate(controller.gameObject);
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            Object.DestroyImmediate(canvas.gameObject);
        foreach (var eventSystem in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include))
            Object.DestroyImmediate(eventSystem.gameObject);
    }

    [UnityTearDown]
    public IEnumerator TearDownFixture()
    {
        foreach (var controller in Object.FindObjectsByType<MiniGameController>(FindObjectsInactive.Include))
            Object.Destroy(controller.gameObject);
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            Object.Destroy(canvas.gameObject);
        foreach (var eventSystem in Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include))
            Object.Destroy(eventSystem.gameObject);
        yield return null;

        if (MiniGameManager.Instance != null)
            Object.Destroy(MiniGameManager.Instance.gameObject);
        while (MiniGameManager.Instance != null)
            yield return null;
    }

    [UnityTest]
    public IEnumerator PipesSolve_UnlocksAndRestores_ThenPatternRetry_Regenerates()
    {
        var inputHandler = new GameObject("InputHandler").AddComponent<InputHandler>();

        _gameManager.RequestBarrierUnlock();
        yield return WaitForMiniGameController();

        Assert.IsTrue(MiniGameManager.Instance.IsMiniGameActive, "A mini-game is active");
        Assert.AreEqual("MiniGame_Pipes_Easy", MiniGameManager.Instance.ActiveSceneName);
        Assert.IsNotNull(Object.FindFirstObjectByType<PipeMiniGameController>(), "The pipes rig is constructed");
        Assert.IsFalse(inputHandler.enabled, "Main input is paused while the mini-game runs");
        Assert.IsTrue(_gameManager.Gate.Locked, "The barrier stays locked while the mini-game runs");

        var controller = Object.FindFirstObjectByType<PipeMiniGameController>();
        SolveByRotation(controller);
        yield return WaitForRigTearDown();

        Assert.IsFalse(_gameManager.Gate.Locked, "Completing the mini-game unlocks the barrier");
        Assert.IsTrue(_gameManager.BarrierTile == null, "The barrier is removed from the grid");
        Assert.IsFalse(MiniGameManager.Instance.IsMiniGameActive, "The mini-game session ends");
        Assert.IsTrue(inputHandler.enabled, "Main input is restored after completion");

        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0), miniGameScene = "MiniGame_Pattern_Easy" } }
        });

        var secondInput = new GameObject("InputHandler").AddComponent<InputHandler>();
        _gameManager.RequestBarrierUnlock();
        yield return WaitForMiniGameController();

        var patternController = Object.FindFirstObjectByType<PatternMiniGameController>();
        Assert.IsNotNull(patternController);
        Assert.IsTrue(patternController.IsPlayingSequence, "Playback runs on entry");
        patternController.SkipPlayback();

        var first = patternController.CopySequence();
        patternController.TapButton((first[0] + 1) % patternController.Spec.ButtonCount);

        Assert.IsTrue(patternController.IsPlayingSequence, "A fresh round plays after the wrong tap");
        patternController.SkipPlayback();
        var second = patternController.CopySequence();

        bool differs = false;
        for (int i = 0; i < first.Length && !differs; i++)
        {
            if (first[i] != second[i]) differs = true;
        }
        Assert.IsTrue(differs, "Retry regenerates a new sequence");
        Assert.IsTrue(patternController.gameObject != null, "The mini-game rig stays alive across retries");
        Assert.IsFalse(secondInput.enabled, "Input stays paused across retries");
    }

    private static IEnumerator WaitForMiniGameController()
    {
        while (Object.FindFirstObjectByType<MiniGameController>() == null)
            yield return null;
    }

    private static IEnumerator WaitForRigTearDown()
    {
        while (Object.FindFirstObjectByType<MiniGameController>() != null)
            yield return null;
    }

    private static void SolveByRotation(PipeMiniGameController controller)
    {
        var board = controller.Board;
        for (int i = 0; i < 64 && !controller.IsSolved; i++)
        {
            for (int x = 0; x < board.Width && !controller.IsSolved; x++)
            {
                for (int y = 0; y < board.Height && !controller.IsSolved; y++)
                {
                    if (board.IsRotatable(x, y)) controller.RotateTileAt(x, y);
                }
            }
        }
        Assert.IsTrue(controller.IsSolved, "Brute-force rotation reaches a connected board");
    }
}