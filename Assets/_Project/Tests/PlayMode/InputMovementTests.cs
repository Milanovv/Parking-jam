using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InputMovementTests
{
    private GameManager _gameManager;
    private GridController _gridController;
    private Vehicle _vehicle;
    private VehicleMovement _vehicleMovement;
    private InputHandler _inputHandler;
    private Camera _camera;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        var gmGo = new GameObject("GameManager");
        _gameManager = gmGo.AddComponent<GameManager>();

        var gridGo = new GameObject("GridController");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        _gridController = gridGo.AddComponent<GridController>();
        _gridController.SetGridSize(8, 8);

        var camGo = new GameObject("MainCamera");
        _camera = camGo.AddComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = 10f;
        camGo.tag = "MainCamera";

        yield return null;
    }

    [UnityTest]
    public IEnumerator Vehicle_SnapsToCellAfterDrag()
    {
        var vehicleGo = new GameObject("TestVehicle");
        vehicleGo.transform.position = Vector3.zero;
        var sprite = vehicleGo.AddComponent<SpriteRenderer>();
        var collider = vehicleGo.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        _vehicle = vehicleGo.AddComponent<Vehicle>();
        _vehicle.Initialize("test_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        _vehicleMovement = vehicleGo.AddComponent<VehicleMovement>();
        _vehicleMovement.Initialize(_gridController);

        _gameManager.RegisterVehicleOnMap(_vehicle);

        Vector3Int destination = new Vector3Int(_gridController.GridWidth - 2, 0, 0);
        bool moved = _vehicleMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsTrue(moved, "Vehicle should move when path is clear");
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(destination, _vehicle.GridPosition);
    }

    [UnityTest]
    public IEnumerator Vehicle_StopsAtGridEdge()
    {
        var vehicleGo = new GameObject("EdgeVehicle");
        vehicleGo.transform.position = Vector3.zero;
        vehicleGo.AddComponent<SpriteRenderer>();
        var collider = vehicleGo.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        _vehicle = vehicleGo.AddComponent<Vehicle>();
        _vehicle.Initialize("edge_car", Orientation.Horizontal, new Vector3Int(6, 0, 0), 2);
        _vehicleMovement = vehicleGo.AddComponent<VehicleMovement>();
        _vehicleMovement.Initialize(_gridController);

        _gameManager.RegisterVehicleOnMap(_vehicle);

        bool moved = _vehicleMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsFalse(moved, "Vehicle should not move past grid edge");
        yield break;
    }

    [UnityTest]
    public IEnumerator Vehicle_FreeDrag_AdvancesTickAndKeepsPool()
    {
        var mover = SpawnVehicle("free_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);

        bool moved = moverMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsTrue(moved, "Vehicle should move when path is clear");
        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual(new Vector3Int(6, 0, 0), mover.GridPosition);
        Assert.AreEqual(1, _gameManager.Tick, "A completed drag advances the tick by one");
        Assert.AreEqual(3, _gameManager.UndoBalance, "A free drag spends no undo");
    }

    [UnityTest]
    public IEnumerator Vehicle_CollidingDrag_CancelsMoveAndSpendsUndo()
    {
        var mover = SpawnVehicle("mover_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        var blocker = SpawnVehicle("blocker_car", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        _gameManager.RegisterVehicleOnMap(mover);
        _gameManager.RegisterVehicleOnMap(blocker);

        bool moved = moverMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsFalse(moved, "A colliding drag applies nothing");
        Assert.AreEqual(new Vector3Int(0, 0, 0), mover.GridPosition, "The vehicle is back where it started");
        Assert.AreEqual(2, _gameManager.UndoBalance, "One undo spent on the collision");
        Assert.AreEqual(0, _gameManager.Tick, "A cancelled move does not advance the tick");
        yield break;
    }

    [UnityTest]
    public IEnumerator Vehicle_EmptyPoolCollisions_RestartLevelAsFreshAttempt()
    {
        var mover = SpawnVehicle("restart_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        var blocker = SpawnVehicle("blocker_car2", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        _gameManager.RegisterVehicleOnMap(mover);
        _gameManager.RegisterVehicleOnMap(blocker);

        for (int i = 0; i < 4; i++)
        {
            moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        }

        Assert.AreEqual(3, _gameManager.UndoBalance, "The fourth collision restarts and refills the pool");
        Assert.AreEqual(0, _gameManager.Tick, "Restart resets the tick");
        Assert.AreEqual(new Vector3Int(0, 0, 0), mover.GridPosition, "Restart returns the vehicle to its initial tile");

        moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.AreEqual(2, _gameManager.UndoBalance, "The fresh attempt spends its own undos");
        yield break;
    }

    [UnityTest]
    public IEnumerator Vehicle_BonusUndos_CollisionsSpendBonusesBeforeAuthored()
    {
        var mover = SpawnVehicle("bonus_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        var blocker = SpawnVehicle("bonus_blocker", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        _gameManager.RegisterVehicleOnMap(mover);
        _gameManager.RegisterVehicleOnMap(blocker);
        _gameManager.Resolver.AddBonusUndos(2);

        moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.AreEqual(1, _gameManager.Resolver.BonusUndos, "First collision spends a bonus undo");
        Assert.AreEqual(4, _gameManager.UndoBalance);

        moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.AreEqual(0, _gameManager.Resolver.BonusUndos);
        Assert.AreEqual(3, _gameManager.UndoBalance, "Authored stock untouched while bonuses remain");

        moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.AreEqual(2, _gameManager.UndoBalance, "Only after bonuses drain do authored undos spend");
        yield break;
    }

    [UnityTest]
    public IEnumerator Vehicle_Restart_ReturnsEveryVehicleToInitialLayout()
    {
        var mover = SpawnVehicle("restart2_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        var blocker = SpawnVehicle("restart2_blocker", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        var wanderer = SpawnVehicle("restart2_wanderer", Orientation.Horizontal, new Vector3Int(0, 1, 0), 1);
        var wandererMovement = wanderer.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);
        _gameManager.RegisterVehicleOnMap(blocker);
        _gameManager.RegisterVehicleOnMap(wanderer);

        bool wandererMoved = wandererMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(wandererMoved);
        yield return new WaitForSeconds(0.2f);
        Assert.AreEqual(new Vector3Int(7, 1, 0), wanderer.GridPosition);
        Assert.AreEqual(1, _gameManager.Tick);

        for (int i = 0; i < 4; i++)
        {
            moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        }

        Vector3 expectedWandererWorld = new Vector3(7f, 1f, 0f);
        Assert.AreEqual(new Vector3Int(0, 1, 0), wanderer.GridPosition, "Grid layout resets");
        Assert.AreEqual(0, _gameManager.Tick, "Restart resets the tick");
        Assert.AreEqual(3, _gameManager.UndoBalance, "Restart refills the pool");
        Assert.AreNotEqual(expectedWandererWorld, wanderer.transform.position, "The wanderer's sprite must not stay where it slid");
        Assert.AreEqual(_gridController.CellToWorld(new Vector3Int(0, 1, 0)), wanderer.transform.position, "Sprite returns to its initial tile");
        yield break;
    }

    [UnityTest]
    public IEnumerator Vehicle_ExitsTheLot_WhenDragCrossesExitEdge()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) }
        });

        var mover = SpawnVehicle("exit_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);

        bool moved = moverMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsTrue(moved, "A drag across the open exit edge leaves the lot");
        yield return new WaitForSeconds(2f);

        Assert.AreEqual(1, _gameManager.Tick, "The exit move advances the tick");
        Assert.IsFalse(mover.gameObject.activeSelf, "The vehicle leaves the stage once off-grid");
        Assert.IsTrue(_gameManager.OccupancyMap.IsTileFree(new Vector3Int(0, 0, 0)), "The vehicle's old tiles are freed");
        Assert.IsTrue(_gameManager.OccupancyMap.IsTileFree(new Vector3Int(1, 0, 0)));
    }

    [UnityTest]
    public IEnumerator Vehicle_LockedBarrier_StopsShortWithoutUndo_ThenUnlockAllowsExit()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });

        var mover = SpawnVehicle("doored_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);

        bool moved = moverMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsTrue(moved, "The slide to the locked barrier is a completed move");
        Assert.AreEqual(new Vector3Int(1, 0, 0), mover.GridPosition, "The bumper pulls up one short of the gate");
        Assert.AreEqual(3, _gameManager.UndoBalance, "A bumper-to-gate stop consumes nothing");
        Assert.AreEqual(1, _gameManager.Tick);
        yield return new WaitForSeconds(0.2f);

        _gameManager.UnlockBarrier();
        bool exited = moverMovement.TryMoveDirection(
            new Vector3Int(1, 0, 0),
            _gameManager.OccupancyMap
        );

        Assert.IsTrue(exited, "After unlock the exit verdict returns");
        yield return new WaitForSeconds(2f);

        Assert.AreEqual(2, _gameManager.Tick);
        Assert.IsFalse(mover.gameObject.activeSelf, "The vehicle leaves the stage through the unlocked gate");
        Assert.IsTrue(_gameManager.OccupancyMap.IsTileFree(new Vector3Int(3, 0, 0)), "The gate tile frees after unlock");
    }

    [UnityTest]
    public IEnumerator Vehicle_AuthoredExitCurve_DrivesAlongItAndDeactivates()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            exitCurve = new[]
            {
                new Vector2Int(7, 0),
                new Vector2Int(9, 0),
                new Vector2Int(11, 1),
                new Vector2Int(14, 1)
            }
        });

        var mover = SpawnVehicle("curve_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);

        bool moved = moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);

        Assert.IsTrue(moved, "The drag across the edge starts the auto-drive");
        yield return new WaitForSeconds(0.35f);
        Assert.That(mover.transform.position.x, Is.GreaterThan(7.5f), "The car travels along the lane past the edge");

        yield return new WaitForSeconds(1.8f);
        Assert.AreEqual(_gridController.CellToWorld(new Vector3Int(14, 1, 0)), mover.transform.position, "The car ends at the authored curve end");
        Assert.IsFalse(mover.gameObject.activeSelf, "The car deactivates once off-screen");
    }

    [UnityTest]
    public IEnumerator Vehicle_DefaultExitCurve_Fallback_DrivesOffScreen()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) }
        });

        var mover = SpawnVehicle("default_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var moverMovement = mover.GetComponent<VehicleMovement>();
        _gameManager.RegisterVehicleOnMap(mover);

        bool moved = moverMovement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);

        Assert.IsTrue(moved);
        yield return new WaitForSeconds(2f);
        Assert.That(mover.transform.position.x, Is.GreaterThan(14.5f), "The default lane drives the car off-screen");
        Assert.That(Mathf.Abs(mover.transform.position.y - 0f), Is.LessThan(0.01f), "The default lane stays straight along the row");
        Assert.IsFalse(mover.gameObject.activeSelf, "The car deactivates once off-screen");
    }

    [UnityTest]
    public IEnumerator Vehicle_Clear_FiresWhenAllExited_AndConfettiSpawns()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0), new Vector2Int(7, 1) }
        });

        var first = SpawnVehicle("clear_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var second = SpawnVehicle("clear_car2", Orientation.Horizontal, new Vector3Int(0, 1, 0), 2);
        _gameManager.RegisterVehicleOnMap(first);
        _gameManager.RegisterVehicleOnMap(second);

        bool clearFired = false;
        _gameManager.Cleared += () => clearFired = true;

        first.GetComponent<VehicleMovement>().TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsFalse(clearFired, "Clear waits for the last vehicle to exit");

        second.GetComponent<VehicleMovement>().TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsFalse(clearFired, "Clear waits for the last exit drive to complete");

        yield return new WaitForSeconds(2.3f);
        Assert.IsTrue(clearFired, "Clear fires once every vehicle has exited");
        Assert.AreEqual(GameState.Won, _gameManager.State, "The game state becomes Won on Clear");

        ParticleSystem[] confetti = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        bool confettiEmitting = false;
        foreach (var ps in confetti)
        {
            if (ps.gameObject.name == "Confetti" && ps.particleCount > 0)
                confettiEmitting = true;
        }
        Assert.IsTrue(confettiEmitting, "Confetti is emitting particles on Clear");
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

    [TearDown]
    public void Teardown()
    {
        var objects = Object.FindObjectsByType<GameObject>();
        foreach (var obj in objects)
        {
            if (obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad")
                Object.DestroyImmediate(obj);
        }
    }
}
