using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
        yield return null;

        _gameManager = Object.FindFirstObjectByType<GameManager>();
        _gridController = Object.FindFirstObjectByType<GridController>();
        _inputHandler = Object.FindFirstObjectByType<InputHandler>();
        _camera = Camera.main;

        if (_gameManager == null)
        {
            var go = new GameObject("GameManager");
            _gameManager = go.AddComponent<GameManager>();
        }

        if (_gridController == null)
        {
            var gridGo = new GameObject("GridController");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
            _gridController = gridGo.AddComponent<GridController>();
            _gridController.SetGridSize(8, 8);
        }

        if (_camera == null)
        {
            var camGo = new GameObject("MainCamera");
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            camGo.tag = "MainCamera";
        }
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

        Vector3Int destination = new Vector3Int(3, 0, 0);
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
    }

    [TearDown]
    public void Teardown()
    {
        var objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            if (obj.scene.name != null && obj.scene.name != "DontDestroyOnLoad")
                Object.Destroy(obj);
        }
    }
}
