using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelAgentsFlowTests : PlayModeTestBase
{
    private GameManager _gameManager;
    private GridController _gridController;

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
        var camera = camGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        camGo.tag = "MainCamera";

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_gameManager != null) Object.DestroyImmediate(_gameManager.gameObject);
        if (_gridController != null) Object.DestroyImmediate(_gridController.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StaticObstacle_CancelsTheMove_AndConsumesAnUndo()
    {
        _gameManager.InitializeLevel(LevelWith(levelUndos: 5, obstacles: new[] { new Vector2Int(5, 2) }));
        var car = SpawnVehicle("car", Orientation.Horizontal, new Vector3Int(0, 2, 0), 2);
        _gameManager.RegisterVehicleOnMap(car);

        bool moved = Move(car, new Vector3Int(1, 0, 0));

        Assert.IsFalse(moved, "The obstacle cancels the move");
        Assert.AreEqual(new Vector3Int(0, 2, 0), car.GridPosition, "The vehicle returns to its pre-move tile");
        Assert.AreEqual(4, _gameManager.UndoBalance, "The collision consumed one undo");
        yield break;
    }

    [UnityTest]
    public IEnumerator Pedestrian_AdvancesOneWaypointPerCompletedMove_AndReverses()
    {
        _gameManager.InitializeLevel(LevelWith(
            levelUndos: 5,
            pedestrians: new[] { new[] { new Vector2Int(3, 3), new Vector2Int(3, 4), new Vector2Int(3, 5) } }));

        var a = SpawnVehicle("a", Orientation.Vertical, new Vector3Int(0, 0, 0), 2);
        var b = SpawnVehicle("b", Orientation.Vertical, new Vector3Int(1, 0, 0), 2);
        _gameManager.RegisterVehicleOnMap(a);
        _gameManager.RegisterVehicleOnMap(b);

        Assert.IsTrue(Move(a, Vector3Int.up), "First completed move advances the tick");
        Assert.AreEqual(new Vector3Int(3, 4, 0), _gameManager.Pedestrians[0].OccupiedTiles[0]);
        yield return WaitForIdle(a);
        Assert.IsTrue(Move(b, Vector3Int.up), "Second completed move advances the tick again");
        Assert.AreEqual(new Vector3Int(3, 5, 0), _gameManager.Pedestrians[0].OccupiedTiles[0], "Route end reached");
        yield return WaitForIdle(b);
        Assert.IsTrue(Move(a, Vector3Int.down), "Third completed move");
        Assert.AreEqual(new Vector3Int(3, 5, 0), _gameManager.Pedestrians[0].OccupiedTiles[0],
            "The pedestrian turns at the route end and waits a tick before stepping back");
        yield return WaitForIdle(a);
        Assert.IsTrue(Move(b, Vector3Int.down), "Fourth completed move");
        Assert.AreEqual(new Vector3Int(3, 4, 0), _gameManager.Pedestrians[0].OccupiedTiles[0],
            "The pedestrian reverses on the tick after turning");
        yield break;
    }

    [UnityTest]
    public IEnumerator Pedestrian_BlocksACar_AndStaysPutDuringTheCancelledMove()
    {
        _gameManager.InitializeLevel(LevelWith(
            levelUndos: 5,
            pedestrians: new[] { new[] { new Vector2Int(4, 2), new Vector2Int(4, 3) } }));

        var car = SpawnVehicle("car", Orientation.Horizontal, new Vector3Int(0, 2, 0), 2);
        _gameManager.RegisterVehicleOnMap(car);

        bool moved = Move(car, new Vector3Int(1, 0, 0));

        Assert.IsFalse(moved, "The pedestrian blocks the car mid-slide");
        Assert.AreEqual(new Vector3Int(0, 2, 0), car.GridPosition);
        Assert.AreEqual(new Vector3Int(4, 2, 0), _gameManager.Pedestrians[0].OccupiedTiles[0],
            "A cancelled move does not advance the tick, so the pedestrian does not move");
        yield break;
    }

    [UnityTest]
    public IEnumerator EmptyUndoPool_Restart_ResetsPedestriansToRouteStart()
    {
        _gameManager.InitializeLevel(LevelWith(
            levelUndos: 1,
            pedestrians: new[] { new[] { new Vector2Int(4, 2), new Vector2Int(4, 3) } }));

        var car = SpawnVehicle("car", Orientation.Horizontal, new Vector3Int(0, 2, 0), 2);
        _gameManager.RegisterVehicleOnMap(car);

        Assert.IsFalse(Move(car, new Vector3Int(1, 0, 0)), "First blocked move spends the only undo");
        Assert.IsFalse(Move(car, new Vector3Int(1, 0, 0)), "Second blocked move restarts the level");

        Assert.AreEqual(1, _gameManager.UndoBalance, "The restart refills the authored undo pool");
        Assert.AreEqual(0, _gameManager.Pedestrians[0].RouteIndex, "The restart returns the pedestrian to route start");
        Assert.AreEqual(new Vector3Int(4, 2, 0), _gameManager.Pedestrians[0].OccupiedTiles[0]);
        yield break;
    }

    [UnityTest]
    public IEnumerator ReinitializingALevel_FreshUndoPoolAndTick()
    {
        _gameManager.InitializeLevel(LevelWith(levelUndos: 5, obstacles: new[] { new Vector2Int(5, 2) }));
        var car = SpawnVehicle("car", Orientation.Horizontal, new Vector3Int(0, 2, 0), 2);
        _gameManager.RegisterVehicleOnMap(car);

        Assert.IsFalse(Move(car, new Vector3Int(1, 0, 0)), "The blocked move spends one undo");
        Assert.AreEqual(4, _gameManager.UndoBalance);
        Assert.AreEqual(0, _gameManager.Tick, "A cancelled move does not advance the tick");

        _gameManager.InitializeLevel(LevelWith(levelUndos: 5, obstacles: new[] { new Vector2Int(5, 2) }));
        Assert.AreEqual(5, _gameManager.UndoBalance,
            "A fresh level gets a full undo pool even when it shares the previous level's levelUndos");
        Assert.AreEqual(0, _gameManager.Tick, "The tick counter does not carry between levels");
        yield break;
    }

    private LevelData LevelWith(int levelUndos, Vector2Int[] obstacles = null, Vector2Int[][] pedestrians = null)
    {
        var level = new LevelData
        {
            id = 99,
            name = "Agents",
            gridWidth = 8,
            gridHeight = 8,
            levelUndos = levelUndos,
            exitTiles = new[] { new Vector2Int(7, 2) },
            vehicles = new[]
            {
                new VehicleData { id = "car", orientation = "horizontal", tiles = new[] { new Vector2Int(0, 2), new Vector2Int(1, 2) } }
            }
        };
        if (obstacles != null)
        {
            level.staticObstacles = new StaticObstacleData[obstacles.Length];
            for (int i = 0; i < obstacles.Length; i++)
                level.staticObstacles[i] = new StaticObstacleData { tile = obstacles[i] };
        }
        if (pedestrians != null)
        {
            level.pedestrians = new PedestrianData[pedestrians.Length];
            for (int i = 0; i < pedestrians.Length; i++)
            {
                var route = pedestrians[i];
                level.pedestrians[i] = new PedestrianData
                {
                    route = new Vector2Int[route.Length]
                };
                for (int w = 0; w < route.Length; w++)
                    level.pedestrians[i].route[w] = route[w];
            }
        }
        return level;
    }

    private bool Move(Vehicle vehicle, Vector3Int direction)
    {
        return vehicle.GetComponent<VehicleMovement>().TryMoveDirection(direction, _gameManager.OccupancyMap);
    }

    private IEnumerator WaitForIdle(Vehicle vehicle)
    {
        var movement = vehicle.GetComponent<VehicleMovement>();
        while (movement.IsAnimating)
            yield return null;
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