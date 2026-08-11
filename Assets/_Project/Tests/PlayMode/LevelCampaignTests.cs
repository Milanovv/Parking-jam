using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelCampaignTests : PlayModeTestBase
{
    private const int MaxAttempts = 300;

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
    public IEnumerator FirstLevel_LoadFromDisk_PlaySolverPlan_Clears()
    {
        yield return PlayFromDiskToClear(1);
    }

    [UnityTest]
    public IEnumerator LastLevel_LoadFromDisk_PlaySolverPlan_Clears()
    {
        yield return PlayFromDiskToClear(25);
    }

    private IEnumerator PlayFromDiskToClear(int levelId)
    {
        bool loaded = LevelLoader.TryLoad(levelId, out LevelData level, out string error);
        Assert.IsTrue(loaded, $"Level {levelId} file loads and validates: {error}");
        Assert.IsNotNull(level);

        _gridController.SetGridSize(level.gridWidth, level.gridHeight);
        _gameManager.InitializeLevel(level);
        if (level.barriers != null && level.barriers.Length > 0)
            _gameManager.UnlockBarrier();
        yield return null;

        var vehicles = new List<Vehicle>();
        foreach (var data in level.vehicles)
        {
            Orientation orientation = data.orientation == "vertical"
                ? Orientation.Vertical
                : Orientation.Horizontal;
            var vehicle = SpawnVehicle(data.id, orientation,
                new Vector3Int(data.tiles[0].x, data.tiles[0].y, 0), data.tiles.Length);
            _gameManager.RegisterVehicleOnMap(vehicle);
            vehicles.Add(vehicle);
        }

        List<SolvableMove> plan = LevelSolver.Solve(level);
        Assert.IsNotNull(plan, $"Level {levelId} has a solver solution");
        Assert.Greater(plan.Count, 0);

        bool cleared = false;
        _gameManager.Cleared += () => cleared = true;

        int moveIndex = 0;
        int attempts = 0;
        while (!cleared && attempts < MaxAttempts)
        {
            attempts++;
            var move = plan[moveIndex % plan.Count];
            moveIndex++;

            var vehicle = vehicles[move.VehicleIndex];
            if (!vehicle.gameObject.activeSelf) continue;

            bool moved = vehicle.GetComponent<VehicleMovement>().TryMoveDirection(
                new Vector3Int(move.Direction.x, move.Direction.y, 0), _gameManager.OccupancyMap);
            if (!moved) continue;

            var movement = vehicle.GetComponent<VehicleMovement>();
            while (movement.IsAnimating)
                yield return null;
        }

        Assert.IsTrue(cleared,
            $"Level {levelId} did not clear after {attempts} attempts; the plan lanes may be blocked by pedestrians");
        Assert.AreEqual(GameState.Won, _gameManager.State);
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
