using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GameSceneFlowTests : PlayModeTestBase
{
    private const int MaxAttempts = 300;

    private GameManager _gameManager;
    private GridController _gridController;
    private GameLauncher _launcher;
    private LevelSessionStats _session;
    private string _savePath;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _savePath = Path.Combine(Path.GetTempPath(), "parking-jam-scene-" + System.Guid.NewGuid() + ".json");
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
        if (EconomyManager.Instance != null)
            Object.DestroyImmediate(EconomyManager.Instance.gameObject);
        EconomyManager.CustomSavePath = null;
        if (File.Exists(_savePath)) File.Delete(_savePath);
        yield return null;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_LevelOneFromDisk_PlacesEveryVehicleOnTheOccupancyMap()
    {
        bool loaded = LevelLoader.TryLoad(1, out LevelData level, out string error);
        Assert.IsTrue(loaded, "Level 1 loads from disk: " + error);

        Assert.IsTrue(_launcher.LaunchLevel(1), "The seam boots the level");
        Assert.AreEqual(1, _launcher.CurrentLevel.id);
        Assert.IsTrue(_launcher.LoadedOk);

        Assert.AreEqual(level.gridWidth, _gridController.GridWidth, "The grid takes the level width");
        Assert.AreEqual(level.gridHeight, _gridController.GridHeight, "The grid takes the level height");

        Assert.AreEqual(level.vehicles.Length, _launcher.Vehicles.Count, "Every authored vehicle is spawned");
        for (int i = 0; i < level.vehicles.Length; i++)
        {
            var data = level.vehicles[i];
            var vehicle = _launcher.Vehicles[i];
            Assert.AreEqual(data.id, vehicle.VehicleId, "The vehicle keeps its authored id");
            foreach (var tile in data.tiles)
            {
                var cell = new Vector3Int(tile.x, tile.y, 0);
                Assert.IsTrue(_gameManager.OccupancyMap.TryGetOccupant(cell, out var occupant),
                    cell + " is occupied by " + data.id);
                Assert.AreSame(vehicle, occupant, "The occupancy map holds the spawned vehicle");
            }
            var start = new Vector3Int(data.tiles[0].x, data.tiles[0].y, 0);
            Assert.AreEqual(_gridController.CellToWorld(start), vehicle.transform.position,
                data.id + " stands on its authored tile");
        }

        Assert.IsNull(_launcher.BarrierView, "Level 1 has no barrier");
        Assert.IsFalse(_gameManager.Gate.Locked, "A barrier-less level starts with an open gate");
        yield break;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_AnotherLevel_ReplacesTheRig()
    {
        LevelLoader.TryLoad(1, out LevelData firstLevel, out string _);
        LevelLoader.TryLoad(2, out LevelData secondLevel, out string secondError);
        Assert.IsNotNull(secondLevel, "Level 2 loads: " + secondError);

        Assert.IsTrue(_launcher.LaunchLevel(1));
        Assert.AreEqual(firstLevel.vehicles.Length, _launcher.Vehicles.Count);

        Assert.IsTrue(_launcher.LaunchLevel(2));
        Assert.AreEqual(2, _launcher.CurrentLevel.id);
        Assert.AreEqual(secondLevel.vehicles.Length, _launcher.Vehicles.Count,
            "The rig is rebuilt for the new level");

        var occupants = _gameManager.OccupancyMap.GetOccupants();
        foreach (var vehicle in _launcher.Vehicles)
        {
            CollectionAssert.Contains(occupants, vehicle, "Every new-level vehicle sits on the map");
        }
        Assert.AreEqual(secondLevel.vehicles.Length, occupants.Length,
            "No leftover vehicles from the previous level stay on the map");
        yield break;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_AuthoredBarrierLevel_SpawnsTheGateAndLocksIt()
    {
        LevelLoader.TryLoad(7, out LevelData level, out string error);
        Assert.IsNotNull(level, "Level 7 loads: " + error);

        Assert.IsTrue(_launcher.LaunchLevel(7));

        var barrierTile = new Vector3Int(level.barriers[0].tile.x, level.barriers[0].tile.y, 0);
        Assert.IsNotNull(_launcher.BarrierView, "The barrier visual is spawned");
        Assert.AreEqual(_gridController.CellToWorld(barrierTile), _launcher.BarrierView.transform.position,
            "The gate stands on the authored barrier tile");

        Assert.IsTrue(_gameManager.Gate.Locked, "The barrier level starts gated");
        Assert.IsTrue(_gameManager.OccupancyMap.TryGetOccupant(barrierTile, out var occupant),
            "The barrier tile is occupied");
        Assert.IsInstanceOf<Barrier>(occupant, "The gate model sits on the map");

        Assert.AreEqual(level.staticObstacles.Length, _launcher.ObstaclesCount,
            "Every authored static obstacle is spawned");
        foreach (var obstacleData in level.staticObstacles)
        {
            var cell = new Vector3Int(obstacleData.tile.x, obstacleData.tile.y, 0);
            Assert.IsFalse(_gameManager.OccupancyMap.IsTileFree(cell), "Static obstacles block their tiles");
            Assert.IsTrue(_gameManager.OccupancyMap.TryGetOccupant(cell, out var obstacle));
            Assert.IsInstanceOf<StaticObstacle>(obstacle);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_AuthoredPedestrianLevel_WalkersSpawnAndFollowTicks()
    {
        LevelLoader.TryLoad(5, out LevelData level, out string error);
        Assert.IsNotNull(level, "Level 5 loads: " + error);

        Assert.IsTrue(_launcher.LaunchLevel(5));
        Assert.AreEqual(level.pedestrians.Length, _launcher.PedestrianViews.Count,
            "Every authored pedestrian spawns a view");

        var start = new Vector3Int(level.pedestrians[0].route[0].x, level.pedestrians[0].route[0].y, 0);
        var view = _launcher.PedestrianViews[0];
        Assert.AreEqual(_gridController.CellToWorld(start), view.transform.position,
            "The walker starts on its authored route");

        List<SolvableMove> plan = LevelSolver.Solve(level);
        Assert.IsNotNull(plan, "Level 5 has a solver plan");

        bool walkerMoved = false;
        Vector3 walkerStart = view.transform.position;
        for (int attempts = 0; attempts < MaxAttempts && !walkerMoved; attempts++)
        {
            var move = plan[attempts % plan.Count];
            var vehicle = _launcher.Vehicles[move.VehicleIndex];
            if (!vehicle.gameObject.activeSelf) continue;

            bool completed = vehicle.GetComponent<VehicleMovement>().TryMoveDirection(
                new Vector3Int(move.Direction.x, move.Direction.y, 0), _gameManager.OccupancyMap);
            if (!completed) continue;

            while (vehicle.GetComponent<VehicleMovement>().IsAnimating)
                yield return null;

            var modelTile = view.Model.OccupiedTiles[0];
            Assert.AreEqual(_gridController.CellToWorld(modelTile), view.transform.position,
                "The walker view tracks its model after every tick");
            if (view.transform.position != walkerStart)
                walkerMoved = true;
        }

        Assert.IsTrue(walkerMoved, "A completed move advanced the walker along its route");
        yield break;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_LevelOneClearsThroughTheSeam_AndCreditsTheSave()
    {
        LevelLoader.TryLoad(1, out LevelData level, out string error);
        Assert.IsNotNull(level, "Level 1 loads: " + error);

        Assert.IsTrue(_launcher.LaunchLevel(1));
        List<SolvableMove> plan = LevelSolver.Solve(level);
        Assert.IsNotNull(plan);

        bool cleared = false;
        _gameManager.Cleared += () => cleared = true;
        int moveIndex = 0;
        for (int attempts = 0; attempts < MaxAttempts && !cleared; attempts++)
        {
            var move = plan[moveIndex % plan.Count];
            moveIndex++;
            var vehicle = _launcher.Vehicles[move.VehicleIndex];
            if (!vehicle.gameObject.activeSelf) continue;

            bool completed = vehicle.GetComponent<VehicleMovement>().TryMoveDirection(
                new Vector3Int(move.Direction.x, move.Direction.y, 0), _gameManager.OccupancyMap);
            if (!completed) continue;

            while (vehicle.GetComponent<VehicleMovement>().IsAnimating)
                yield return null;
        }

        Assert.IsTrue(cleared, "The level clears through the scene's seams");
        Assert.AreEqual(GameState.Won, _gameManager.State);
        Assert.Greater(_session.MovesIssued, 0, "The moves counter advanced");
        Assert.Greater(_session.ElapsedPlayTime, 0f, "The level timer ran");

        var economy = EconomyManager.Instance;
        int expectedCoins = EconomyConfig.LevelBaseCoins + level.levelUndos * EconomyConfig.CoinPerUndoRemaining;
        Assert.AreEqual(expectedCoins, economy.State.coins, "Clear credits base plus the full undo pool");
        Assert.AreEqual(1, economy.State.lastCompletedLevel, "The save records the completed level");
        Assert.AreEqual(level.levelUndos, economy.State.RecordFor(1).bestUndosRemaining);
        Assert.GreaterOrEqual(economy.State.RecordFor(1).attemptCount, 1, "The attempt was logged");

        Object.DestroyImmediate(economy.gameObject);
        var reloaded = EconomyManager.EnsureInstance();
        Assert.AreEqual(expectedCoins, reloaded.State.coins, "The credited coins persist through the save storage");
        Assert.AreEqual(1, reloaded.State.lastCompletedLevel, "Progress persists through the save storage");
        yield break;
    }

    [UnityTest]
    public IEnumerator LevelTimer_AdvancesWhilePlaying_AndFreezesAfterClear()
    {
        var level = new LevelData
        {
            id = 98,
            gridWidth = 6,
            gridHeight = 6,
            levelUndos = 3,
            exitTiles = new[] { new Vector2Int(5, 1) },
            vehicles = new[]
            {
                new VehicleData { id = "lonely", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) } }
            }
        };
        Assert.IsTrue(_launcher.LaunchLevel(level));
        yield return null;

        float t0 = _session.ElapsedPlayTime;
        yield return new WaitForSeconds(0.25f);
        Assert.Greater(_session.ElapsedPlayTime, t0 + 0.15f, "The timer advances while playing");

        var vehicle = _launcher.Vehicles[0];
        bool completed = vehicle.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(completed, "The only car drives out of the lot");

        float deadline = Time.time + 6f;
        while (_gameManager.State != GameState.Won && Time.time < deadline)
            yield return null;
        Assert.AreEqual(GameState.Won, _gameManager.State);

        float frozenAt = _session.ElapsedPlayTime;
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(frozenAt, _session.ElapsedPlayTime, "The timer stops once the level is cleared");
        Assert.AreEqual(1, _session.MovesIssued, "The moves counter stops once the level is cleared");
        yield break;
    }

    [UnityTest]
    public IEnumerator LaunchSeam_DoesNotPolluteTheSaveWithAnUnAuthoredLevel()
    {
        var economy = EconomyManager.Instance;
        Assert.IsNull(economy.State.RecordFor(0), "No phantom attempt record for an un-authored level id");
        Assert.IsNull(economy.State.RecordFor(-1), "No phantom attempt record for an un-authored level id");

        Assert.IsTrue(_launcher.LaunchLevel(1));
        Assert.GreaterOrEqual(economy.State.RecordFor(1).attemptCount, 1,
            "The launched level still logs its attempt");
        yield break;
    }

    [UnityTest]
    public IEnumerator MoveCounter_MirrorsCompletedMoves_NotCancelledCollisions()
    {
        var level = new LevelData
        {
            id = 99,
            gridWidth = 6,
            gridHeight = 5,
            levelUndos = 3,
            exitTiles = new[] { new Vector2Int(5, 1) },
            vehicles = new[]
            {
                new VehicleData { id = "mover", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) } },
                new VehicleData { id = "blocker", orientation = "horizontal", tiles = new[] { new Vector2Int(4, 1) } }
            }
        };
        Assert.IsTrue(_launcher.LaunchLevel(level));
        Assert.AreEqual(0, _session.MovesIssued);
        Assert.AreEqual(0, _gameManager.Tick);

        var mover = _launcher.Vehicles[0];
        var blocker = _launcher.Vehicles[1];
        bool moved = mover.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsFalse(moved, "The slide into the blocker is cancelled");
        Assert.AreEqual(0, _session.MovesIssued, "A cancelled move counts nothing");
        Assert.AreEqual(0, _gameManager.Tick);
        Assert.AreEqual(2, _gameManager.UndoBalance, "The collision still spends an undo");

        _gameManager.OccupancyMap.Remove(blocker);
        moved = mover.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(moved, "With the lane clear the drag completes");
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(1, _session.MovesIssued, "The completed drag counts one move");
        Assert.AreEqual(1, _gameManager.Tick);
        yield break;
    }
}