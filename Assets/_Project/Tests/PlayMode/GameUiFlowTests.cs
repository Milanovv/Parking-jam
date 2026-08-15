using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class GameUiFlowTests : PlayModeTestBase
{
    private GameManager _gameManager;
    private GridController _gridController;
    private GameLauncher _launcher;
    private LevelSessionStats _session;
    private string _savePath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _savePath = Path.Combine(Path.GetTempPath(), "parking-jam-ui-" + System.Guid.NewGuid() + ".json");
        EconomyManager.CustomSavePath = _savePath;
        EconomyManager.EnsureInstance();

        var gmGo = new GameObject("GameManager");
        _gameManager = gmGo.AddComponent<GameManager>();
        typeof(GameManager).GetField("_levelData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_gameManager, new LevelData());

        var gridGo = new GameObject("GridController");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        _gridController = gridGo.AddComponent<GridController>();
        _gridController.SetGridSize(8, 8);

        var camGo = new GameObject("MainCamera");
        var camera = camGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camGo.tag = "MainCamera";

        var sessionGo = new GameObject("GameSession");
        _session = sessionGo.AddComponent<LevelSessionStats>();

        var launcherGo = new GameObject("GameLauncher");
        _launcher = launcherGo.AddComponent<GameLauncher>();
        _launcher.AutoStartOnPlay = false;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDownSave()
    {
        if (MiniGameManager.Instance != null)
            Object.DestroyImmediate(MiniGameManager.Instance.gameObject);
        if (EconomyManager.Instance != null)
            Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        EconomyManager.CustomSavePath = null;
        if (File.Exists(_savePath)) File.Delete(_savePath);
        yield return null;
    }

    private static LevelData RoomForOneCar()
    {
        return new LevelData
        {
            id = 98,
            gridWidth = 8,
            gridHeight = 6,
            levelUndos = 3,
            exitTiles = new[] { new Vector2Int(5, 0) },
            vehicles = new[]
            {
                new VehicleData { id = "car", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 2), new Vector2Int(1, 2) } }
            }
        };
    }

    private static Text SpawnText(string name)
    {
        var parent = new GameObject("TextParent").transform;
        return GameUiFactory.CreateText(parent, name, "", 30, Color.white, TextAnchor.MiddleLeft);
    }

    private static Button SpawnButton(string name)
    {
        var parent = new GameObject("ButtonParent").transform;
        return GameUiFactory.CreateButton(parent, name, name, () => { }, new Vector2(200f, 64f), Color.gray);
    }

    [UnityTest]
    public IEnumerator Hud_ReflectsMovesUndosCoinsAndKeys()
    {
        var movesText = SpawnText("MovesText");
        var timerText = SpawnText("TimerText");
        var undosText = SpawnText("UndosText");
        var coinsText = SpawnText("CoinsText");
        var keysText = SpawnText("KeysText");

        var hudGo = new GameObject("HUD");
        var hud = hudGo.AddComponent<LevelHud>();
        hud.MovesText = movesText;
        hud.TimerText = timerText;
        hud.UndosText = undosText;
        hud.CoinsText = coinsText;
        hud.KeysText = keysText;
        hud.Stats = _session;

        var economy = EconomyManager.Instance;
        economy.State.AddCoins(55);
        economy.State.AddKeys(2);

        LevelData level = RoomForOneCar();
        Assert.IsTrue(_launcher.LaunchLevel(level), "The roomy level boots");
        Assert.AreEqual(level.levelUndos, _gameManager.UndoBalance, "Fresh level holds its full undo pool");

        var vehicle = _launcher.Vehicles[0];
        bool moved = vehicle.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(moved, "The car slides one lane right");
        yield return new WaitForSeconds(0.2f);

        hud.Refresh();

        Assert.AreEqual("Moves: 1", movesText.text, "The moves counter tracks the model");
        Assert.AreEqual("Undos: " + level.levelUndos, undosText.text, "The undo counter tracks the model");
        Assert.AreEqual("55", coinsText.text, "The coin counter tracks the economy");
        Assert.AreEqual("2", keysText.text, "The key counter tracks the economy");
        yield break;
    }

    [UnityTest]
    public IEnumerator Hud_CoinSkipAffordance_TracksTheBarrierLock()
    {
        var coinSkip = SpawnButton("CoinSkipButton");
        var hudGo = new GameObject("HUD");
        var hud = hudGo.AddComponent<LevelHud>();
        hud.CoinSkipButton = coinSkip;
        hud.Stats = _session;

        Assert.IsTrue(_launcher.LaunchLevel(7), "The barrier tutorial level boots");
        Assert.IsTrue(_gameManager.Gate.Locked, "Level 7 starts gated");

        hud.Refresh();
        Assert.IsTrue(coinSkip.gameObject.activeSelf, "The coin-skip affordance shows while the gate is locked");

        _gameManager.UnlockBarrier();
        hud.Refresh();
        Assert.IsFalse(coinSkip.gameObject.activeSelf, "The coin-skip affordance hides once the gate is open");
        yield break;
    }

    [UnityTest]
    public IEnumerator LevelSelect_UnlockState_ComesFromTheSave()
    {
        var selectGo = new GameObject("LevelSelect");
        var select = selectGo.AddComponent<LevelSelectController>();

        select.Build(new SaveData(), new GameObject("FreshGrid").transform);
        Assert.AreEqual(1, select.UnlockedCount, "A fresh save unlocks exactly level one");
        Assert.IsTrue(select.IsUnlocked(1));
        Assert.IsFalse(select.IsUnlocked(2));

        var progressed = new SaveData();
        progressed.CompleteLevel(3, 1);
        select.Build(progressed, new GameObject("ProgressGrid").transform);
        Assert.IsTrue(select.IsUnlocked(1), "Earlier levels stay unlocked");
        Assert.IsTrue(select.IsUnlocked(3), "Completed levels stay replayable");
        Assert.IsTrue(select.IsUnlocked(4), "The next level unlocks behind the last completion");
        Assert.IsFalse(select.IsUnlocked(5), "Two ahead stays locked");
        Assert.AreEqual(4, select.UnlockedCount, "Exactly levels one through four are playable");
        yield break;
    }

    [UnityTest]
    public IEnumerator Pause_BlocksMovement_UntilResumed()
    {
        var inputHandler = new GameObject("InputHandler").AddComponent<InputHandler>();
        typeof(GameManager).GetField("_inputHandler",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(_gameManager, inputHandler);

        Assert.IsTrue(_launcher.LaunchLevel(RoomForOneCar()), "The roomy level boots");
        var vehicle = _launcher.Vehicles[0];
        var movement = vehicle.GetComponent<VehicleMovement>();
        Assert.IsTrue(movement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap),
            "The car can move while playing");
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(1, _gameManager.Tick);

        _gameManager.Pause();
        Assert.IsTrue(_gameManager.Paused);
        Assert.IsFalse(inputHandler.enabled, "Pausing disables the drag handler");

        Vector3Int parked = vehicle.GridPosition;
        int tickWhilePaused = _gameManager.Tick;
        float frozenTime = _session.ElapsedPlayTime;
        bool movedWhilePaused = movement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsFalse(movedWhilePaused, "A drag while paused moves nothing");
        Assert.AreEqual(parked, vehicle.GridPosition, "The vehicle stays parked while paused");
        Assert.AreEqual(tickWhilePaused, _gameManager.Tick, "No move counts while paused");
        yield return new WaitForSeconds(0.25f);
        Assert.AreEqual(frozenTime, _session.ElapsedPlayTime, "The level timer freezes while paused");

        _gameManager.Resume();
        Assert.IsFalse(_gameManager.Paused);
        Assert.IsTrue(inputHandler.enabled, "Resuming re-enables the drag handler");
        Assert.IsTrue(movement.TryMoveDirection(new Vector3Int(-1, 0, 0), _gameManager.OccupancyMap),
            "The car drives again once resumed");
        Assert.AreEqual(tickWhilePaused + 1, _gameManager.Tick, "The resumed move counts");
        yield break;
    }

    [UnityTest]
    public IEnumerator GameUi_Flow_ReachesMenuToLevelSelectToHud()
    {
        var canvasHost = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasHost.GetComponent<Canvas>();

        var hudGo = new GameObject("HUD", typeof(RectTransform));
        hudGo.transform.SetParent(canvas.transform, false);
        var hud = hudGo.AddComponent<LevelHud>();
        hud.MovesText = SpawnText("MovesText");
        hud.TimerText = SpawnText("TimerText");
        hud.UndosText = SpawnText("UndosText");
        hud.CoinsText = SpawnText("CoinsText");
        hud.KeysText = SpawnText("KeysText");
        hud.Stats = _session;

        var uiGo = new GameObject("GameUi");
        var ui = uiGo.AddComponent<GameUiController>();
        yield return null;

        Assert.IsTrue(ui.IsShowingMenu, "The game boots to the main menu");
        Assert.IsFalse(_launcher.AutoStartOnPlay, "The flow takes over the launcher's auto-start");

        ui.ShowLevelSelect();
        Assert.IsTrue(ui.IsShowingLevelSelect, "Menu reaches the level select screen");
        Assert.AreEqual(1, ui.Launcher == null ? 0 : 1, "The launcher is resolved into the flow");

        Button levelTap = null;
        foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (button.name == "Level 1") { levelTap = button; break; }
        }
        Assert.IsNotNull(levelTap, "A tappable level-one button exists on the select grid");
        levelTap.onClick.Invoke();

        Assert.IsTrue(ui.IsShowingHud, "Tapping a level button reveals the HUD");
        Assert.IsTrue(ui.Launcher.LoadedOk, "The level launches through the flow");
        Assert.AreEqual(1, ui.Launcher.CurrentLevel.id);

        ui.ShowPause();
        Assert.IsTrue(ui.IsPauseVisible, "Pause is reachable from the level");
        ui.ResumeGame();
        Assert.IsFalse(ui.IsPauseVisible, "Resume returns to play");
        yield break;
    }

    [UnityTest]
    public IEnumerator GameOverlay_BlocksDuringPauseAndMiniGame()
    {
        var canvasHost = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var hudGo = new GameObject("HUD", typeof(RectTransform));
        hudGo.transform.SetParent(canvasHost.transform, false);
        var hud = hudGo.AddComponent<LevelHud>();

        var uiGo = new GameObject("GameUi");
        var ui = uiGo.AddComponent<GameUiController>();
        yield return null;

        ui.ShowPause();
        ui.RefreshOverlay();
        Assert.IsTrue(ui.OverlayActive, "The overlay covers the grid during pause");
        ui.ResumeGame();
        ui.RefreshOverlay();
        Assert.IsFalse(ui.OverlayActive, "The overlay lifts on resume");

        var manager = MiniGameManager.EnsureInstance();
        manager.LoadMiniGame("MiniGame_Pipes_Easy");
        Assert.IsTrue(manager.IsMiniGameActive, "A mini-game is active");
        ui.RefreshOverlay();
        Assert.IsTrue(ui.OverlayActive, "The overlay covers the grid while a mini-game runs");

        manager.CompleteMiniGame();
        ui.RefreshOverlay();
        Assert.IsFalse(ui.OverlayActive, "The overlay lifts when the mini-game ends");
        yield break;
    }

    [UnityTest]
    public IEnumerator Shop_Cards_ShowOwnedAndEquippedState()
    {
        var shopGo = new GameObject("Shop");
        var shop = shopGo.AddComponent<ShopController>();

        var economy = EconomyManager.Instance;
        economy.State.AddCoins(500);
        economy.State.UnlockVehicleSkin("Blue");
        economy.EquipSkin("Blue");

        shop.Build(economy.State, new GameObject("Cards").transform, _ => { });

        Assert.AreEqual(SkinCatalog.All.Length, shop.CardCount, "The shop lists every catalogued skin");
        Assert.IsTrue(Card(shop, "Red").Owned, "The default paint counts as owned");
        Assert.IsTrue(Card(shop, "Blue").Owned && Card(shop, "Blue").Equipped, "An unlocked equipped skin shows both flags");
        Assert.IsFalse(Card(shop, "Yellow").Owned, "A never-seen skin stays unowned");
        Assert.IsTrue(Card(shop, "Yellow").Exclusive, "The key skin shows its exclusivity");
        yield break;
    }

    [UnityTest]
    public IEnumerator BarrierTap_RequestsUnlockFlow_AndHidesTheTutorialCue()
    {
        var cue = SpawnText("TutorialCue");
        var hudGo = new GameObject("HUD");
        var hud = hudGo.AddComponent<LevelHud>();
        hud.TutorialCue = cue;
        hud.Stats = _session;

        Assert.IsTrue(_launcher.LaunchLevel(7), "The barrier tutorial level boots");
        Assert.IsTrue(_gameManager.Gate.Locked, "Level 7 starts gated");

        hud.Refresh();
        Assert.IsTrue(cue.gameObject.activeSelf, "The tap-to-unlock cue teaches the gate while it is locked");

        _gameManager.RequestBarrierUnlock();
        Assert.IsTrue(MiniGameManager.Instance.IsMiniGameActive, "Tapping the gate requests the unlock flow");
        hud.Refresh();
        Assert.IsFalse(cue.gameObject.activeSelf, "The cue hides after the first tap");

        _gameManager.UnlockBarrier();
        hud.Refresh();
        Assert.IsFalse(cue.gameObject.activeSelf, "The cue stays hidden once the gate is open");

        Assert.IsTrue(_launcher.LaunchLevel(7), "The barrier level relaunches");
        hud.Refresh();
        Assert.IsTrue(cue.gameObject.activeSelf, "A fresh attempt brings the cue back until the next tap");
        yield break;
    }

    [UnityTest]
    public IEnumerator CoinSkip_ReflectsBalance_AndSpendsToClearTheBarrier()
    {
        var coinSkip = SpawnButton("CoinSkipButton");
        var hudGo = new GameObject("HUD");
        hudGo.SetActive(false);
        var hud = hudGo.AddComponent<LevelHud>();
        hud.CoinSkipButton = coinSkip;
        hud.Stats = _session;
        hudGo.SetActive(true);

        var economy = EconomyManager.Instance;
        Assert.IsTrue(_launcher.LaunchLevel(7), "The barrier tutorial level boots");
        hud.Refresh();
        Assert.IsTrue(coinSkip.gameObject.activeSelf, "The coin-skip affordance shows while the gate is locked");
        Assert.IsFalse(coinSkip.interactable, "An empty balance disables the affordance");

        coinSkip.onClick.Invoke();
        Assert.IsTrue(_gameManager.Gate.Locked, "A click with no coins buys nothing");
        Assert.AreEqual(0, economy.State.coins, "No coins are spent on an unaffordable skip");

        economy.State.AddCoins(EconomyConfig.CoinSkipPriceCoins * 5);
        hud.Refresh();
        Assert.IsTrue(coinSkip.interactable, "A funded balance enables the affordance");

        coinSkip.onClick.Invoke();
        Assert.IsFalse(_gameManager.Gate.Locked, "The coin-skip clears the barrier");
        Assert.AreEqual(EconomyConfig.CoinSkipPriceCoins * 4, economy.State.coins,
            "The skip spends exactly the configured price");
        Assert.IsFalse(MiniGameManager.Instance != null && MiniGameManager.Instance.IsMiniGameActive,
            "The coin-skip bypasses the mini-game unlock flow");
        hud.Refresh();
        Assert.IsFalse(coinSkip.gameObject.activeSelf, "The affordance hides once the gate is open");
        yield break;
    }

    [UnityTest]
    public IEnumerator Pause_Respects_GateTapAndCoinSkip()
    {
        var coinSkip = SpawnButton("CoinSkipButton");
        var cue = SpawnText("TutorialCue");
        var hudGo = new GameObject("HUD");
        hudGo.SetActive(false);
        var hud = hudGo.AddComponent<LevelHud>();
        hud.CoinSkipButton = coinSkip;
        hud.TutorialCue = cue;
        hud.Stats = _session;
        hudGo.SetActive(true);

        var economy = EconomyManager.Instance;
        economy.State.AddCoins(EconomyConfig.CoinSkipPriceCoins);
        Assert.IsTrue(_launcher.LaunchLevel(7), "The barrier tutorial level boots");
        hud.Refresh();
        Assert.IsTrue(coinSkip.interactable, "The affordance is enabled while playing");

        _gameManager.Pause();
        hud.Refresh();
        Assert.IsFalse(coinSkip.interactable, "Pausing disables the coin-skip affordance");
        Assert.IsFalse(cue.gameObject.activeSelf, "Pausing hides the tutorial cue");

        bool miniGameBefore = MiniGameManager.Instance != null && MiniGameManager.Instance.IsMiniGameActive;
        _gameManager.RequestBarrierUnlock();
        bool miniGameAfter = MiniGameManager.Instance != null && MiniGameManager.Instance.IsMiniGameActive;
        Assert.AreEqual(miniGameBefore, miniGameAfter, "A paused tap requests no unlock flow");
        Assert.IsFalse(_gameManager.GateTapped, "A paused tap does not count as the first tap");

        _gameManager.Resume();
        hud.Refresh();
        Assert.IsTrue(coinSkip.interactable, "Resuming re-enables the affordance");
        Assert.IsTrue(cue.gameObject.activeSelf, "Resuming brings the cue back until the first tap");

        coinSkip.onClick.Invoke();
        Assert.IsFalse(_gameManager.Gate.Locked, "The affordance works again after resume");
        yield break;
    }

    private static ShopController.CardState Card(ShopController shop, string id)
    {
        foreach (var card in shop.Cards)
        {
            if (card.Id == id) return card;
        }
        Assert.Fail("No shop card for " + id);
        return default;
    }
}
