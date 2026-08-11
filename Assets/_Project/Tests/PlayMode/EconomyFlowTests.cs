using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EconomyFlowTests : PlayModeTestBase
{
    private GameManager _gameManager;
    private GridController _gridController;
    private string _savePath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _savePath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "parking-jam-economy-" + System.Guid.NewGuid() + ".json");
        EconomyManager.CustomSavePath = _savePath;
        EconomyManager.EnsureInstance();

        var gmGo = new GameObject("GameManager");
        _gameManager = gmGo.AddComponent<GameManager>();

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

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDownEconomy()
    {
        if (EconomyManager.Instance != null)
            Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        EconomyManager.CustomSavePath = null;
        if (System.IO.File.Exists(_savePath)) System.IO.File.Delete(_savePath);
        yield return null;
    }

    [UnityTest]
    public IEnumerator LevelCompletion_CreditsCoins_AndSurvivesRestart()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            id = 1,
            exitTiles = new[] { new Vector2Int(7, 0), new Vector2Int(7, 1) }
        });
        Assert.AreEqual(1, EconomyManager.Instance.State.RecordFor(1).attemptCount, "Loading the level records an attempt");

        var first = SpawnVehicle("clear_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var second = SpawnVehicle("clear_car2", Orientation.Horizontal, new Vector3Int(0, 1, 0), 2);
        _gameManager.RegisterVehicleOnMap(first);
        _gameManager.RegisterVehicleOnMap(second);

        first.GetComponent<VehicleMovement>().TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        second.GetComponent<VehicleMovement>().TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        yield return new WaitForSeconds(2.5f);

        Assert.AreEqual(GameState.Won, _gameManager.State, "The level clears");
        int earned = EconomyConfig.LevelBaseCoins + 3 * EconomyConfig.CoinPerUndoRemaining;
        Assert.AreEqual(earned, EconomyManager.Instance.State.coins, "Clear credits base plus the full undo pool");
        Assert.AreEqual(1, EconomyManager.Instance.State.lastCompletedLevel);
        Assert.AreEqual(3, EconomyManager.Instance.State.RecordFor(1).bestUndosRemaining);

        Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        var reloaded = EconomyManager.EnsureInstance();
        Assert.AreEqual(earned, reloaded.State.coins, "The save survives an app restart");
        Assert.AreEqual(1, reloaded.State.lastCompletedLevel, "Level progress survives the restart");
        Assert.AreEqual(3, reloaded.State.RecordFor(1).bestUndosRemaining);
    }

    [UnityTest]
    public IEnumerator CoinSkip_DebitsCoins_UnlocksWithoutMiniGame_AndFailsWhenBroke()
    {
        var economy = EconomyManager.Instance;
        economy.State.AddCoins(500);
        economy.Save();

        _gameManager.InitializeLevel(new LevelData
        {
            id = 2,
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });

        Assert.IsTrue(_gameManager.TryCoinSkip(), "A funded skip goes through");
        Assert.AreEqual(400, economy.State.coins, "The skip debits the coin balance");
        Assert.IsFalse(_gameManager.Gate.Locked, "The gate opens on a paid skip");
        Assert.IsTrue(_gameManager.BarrierTile == null, "The barrier leaves the grid");
        Assert.IsTrue(Object.FindFirstObjectByType<MiniGameController>() == null, "No mini-game rig is spawned by a paid skip");

        _gameManager.InitializeLevel(new LevelData
        {
            id = 3,
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });
        Assert.IsTrue(economy.State.TrySpendCoins(350), "Drain the balance below the skip price");
        economy.Save();

        Assert.IsFalse(_gameManager.TryCoinSkip(), "A broke skip is refused");
        Assert.AreEqual(50, economy.State.coins, "The refused skip changes nothing");
        Assert.IsTrue(_gameManager.Gate.Locked, "The barrier stays locked on a refused skip");
        Assert.IsTrue(Object.FindFirstObjectByType<MiniGameController>() == null, "No mini-game rig is spawned by a refused skip");
        yield break;
    }

    [UnityTest]
    public IEnumerator DailyLogin_GrantsPersistableBonusUndos_AndWelcomeSkinOnce()
    {
        var economy = EconomyManager.Instance;
        Assert.AreEqual("Red", economy.State.equippedVehicleSkinId, "A fresh save equips the default paint");

        var first = economy.RegisterDailyLogin(new System.DateTime(2026, 8, 11));
        Assert.IsTrue(first.FirstLogin);
        Assert.IsTrue(first.WelcomeSkinGranted);
        Assert.IsTrue(economy.State.IsSkinUnlocked(EconomyConfig.WelcomeSkinId));
        Assert.IsTrue(economy.State.IsSkinUnlocked(EconomyConfig.WelcomePedestrianSkinId), "The welcome gift covers a pedestrian skin");
        Assert.AreEqual(2, economy.State.bonusUndosRemaining);
        Assert.AreEqual(1, economy.State.dailyLoginStreak);

        var sameDay = economy.RegisterDailyLogin(new System.DateTime(2026, 8, 11));
        Assert.IsFalse(sameDay.BonusGranted, "Same-day login grants nothing new");
        Assert.AreEqual(2, economy.State.bonusUndosRemaining);

        var nextDay = economy.RegisterDailyLogin(new System.DateTime(2026, 8, 12));
        Assert.IsTrue(nextDay.BonusGranted);
        Assert.AreEqual(2, nextDay.Streak);
        Assert.AreEqual(4, economy.State.bonusUndosRemaining);

        Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        var reloaded = EconomyManager.EnsureInstance();
        Assert.AreEqual(4, reloaded.State.bonusUndosRemaining, "Bonus undos persist through restarts");
        Assert.AreEqual(2, reloaded.State.dailyLoginStreak, "The streak persists through restarts");
        Assert.IsTrue(reloaded.State.IsSkinUnlocked(EconomyConfig.WelcomeSkinId), "The welcome skin was not re-granted");
        Assert.IsTrue(reloaded.State.IsSkinUnlocked(EconomyConfig.WelcomePedestrianSkinId), "The welcome pedestrian skin was not re-granted");

        var gapDay = reloaded.RegisterDailyLogin(new System.DateTime(2026, 8, 15));
        Assert.AreEqual(1, gapDay.Streak, "A missed day resets the streak");
        Assert.AreEqual(6, reloaded.State.bonusUndosRemaining);
        yield break;
    }

    [UnityTest]
    public IEnumerator BonusUndos_SeedIntoThePool_AndConsumeFirst()
    {
        var economy = EconomyManager.Instance;
        economy.RegisterDailyLogin(new System.DateTime(2026, 8, 11));

        _gameManager.InitializeLevel(new LevelData
        {
            id = 4,
            exitTiles = new[] { new Vector2Int(7, 0) }
        });

        Assert.AreEqual(2, _gameManager.Resolver.BonusUndos, "The daily bonus tops up the pool on level start");
        Assert.AreEqual(5, _gameManager.UndoBalance);

        var mover = SpawnVehicle("mover_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var blocker = SpawnVehicle("blocker_car", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        _gameManager.RegisterVehicleOnMap(mover);
        _gameManager.RegisterVehicleOnMap(blocker);

        bool moved = mover.GetComponent<VehicleMovement>().TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);

        Assert.IsFalse(moved, "The collision cancels the move");
        Assert.AreEqual(1, _gameManager.Resolver.BonusUndos, "The collision spends a bonus undo first");
        Assert.AreEqual(3, _gameManager.Resolver.AuthoredRemaining, "Authored undos stay untouched while bonuses remain");
        Assert.AreEqual(1, economy.State.bonusUndosRemaining, "The spend drains the persisted bonus bank");

        Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        var reloaded = EconomyManager.EnsureInstance();
        Assert.AreEqual(1, reloaded.State.bonusUndosRemaining, "The consumed bonus undo survives a restart");
        yield break;
    }

    private Vehicle SpawnVehicle(string id, Orientation orientation, Vector3Int position, int length)
    {
        var vehicleGo = new GameObject(id);
        vehicleGo.transform.position = Vector3.zero;
        vehicleGo.AddComponent<SpriteRenderer>();
        var collider = vehicleGo.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        var vehicle = vehicleGo.AddComponent<Vehicle>();
        vehicle.Initialize(id, orientation, position, length);
        var movement = vehicleGo.AddComponent<VehicleMovement>();
        movement.Initialize(_gridController);
        return vehicle;
    }
}