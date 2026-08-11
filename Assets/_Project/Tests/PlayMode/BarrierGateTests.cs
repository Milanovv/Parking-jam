using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BarrierGateTests : PlayModeTestBase
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

        yield return null;
    }

    private BarrierGate SpawnGateView()
    {
        var gateGo = new GameObject("Barrier");
        var pivot = new GameObject("CrossbarPivot");
        pivot.transform.SetParent(gateGo.transform);
        pivot.transform.localPosition = new Vector3(0f, 0f, 1f);
        var gate = gateGo.AddComponent<BarrierGate>();
        gate.CrossbarPivot = pivot.transform;
        return gate;
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
        _gameManager.RegisterVehicleOnMap(vehicle);
        return vehicle;
    }

    [UnityTest]
    public IEnumerator Barrier_StartsLocked_CrossbarDown()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });
        var gate = SpawnGateView();

        yield return null;

        Assert.IsTrue(_gameManager.Gate.Locked, "The gate is locked at level start");
        Assert.IsFalse(gate.IsOpen, "The gate view renders closed while locked");
        Assert.AreEqual(Quaternion.identity, gate.CrossbarPivot.localRotation, "The crossbar lies across the lane");
        yield break;
    }

    [UnityTest]
    public IEnumerator Barrier_MiniGameCompletion_UnlocksGate_AndStaysOpen_ForTheLevel()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });
        var gate = SpawnGateView();
        var first = SpawnVehicle("first_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var second = SpawnVehicle("second_car", Orientation.Horizontal, new Vector3Int(0, 1, 0), 2);

        yield return null;

        bool firstMoved = first.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(firstMoved, "The slide to the locked gate is a completed move");
        Assert.IsFalse(gate.IsOpen, "The gate stays closed until the mini-game completes");
        Assert.AreEqual(new Vector3Int(1, 0, 0), first.GridPosition,
            "While locked the bumper pulls up one short of the gate: the exit lane stays closed");
        yield return new WaitForSeconds(0.2f);

        _gameManager.UnlockBarrier();

        Assert.IsTrue(gate.IsOpen, "Completing the mini-game raises the crossbar");
        Assert.AreNotEqual(Quaternion.identity, gate.CrossbarPivot.localRotation, "The crossbar lifts out of the lane");
        yield return null;

        bool firstExited = first.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(firstExited, "The first vehicle drives out through the open gate");
        yield return new WaitForSeconds(2f);

        bool secondExited = second.GetComponent<VehicleMovement>().TryMoveDirection(
            new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        Assert.IsTrue(secondExited, "A later vehicle still exits without re-unlocking");
        yield return new WaitForSeconds(2f);

        Assert.IsTrue(gate.IsOpen, "The gate stays open for the rest of the level");
        Assert.IsFalse(_gameManager.Gate.Locked, "The gate never re-locks during the level");
        yield break;
    }

    [UnityTest]
    public IEnumerator Barrier_RelocksOnLevelRestart_CrossbarDownAgain()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) },
            barriers = new[] { new BarrierData { tile = new Vector2Int(3, 0) } }
        });
        var gate = SpawnGateView();
        var mover = SpawnVehicle("restart_car", Orientation.Horizontal, new Vector3Int(0, 0, 0), 2);
        var blocker = SpawnVehicle("restart_blocker", Orientation.Horizontal, new Vector3Int(4, 0, 0), 1);
        var movement = mover.GetComponent<VehicleMovement>();

        yield return null;

        _gameManager.UnlockBarrier();
        Assert.IsTrue(gate.IsOpen, "The gate opened after unlock");

        for (int i = 0; i < 4; i++)
        {
            movement.TryMoveDirection(new Vector3Int(1, 0, 0), _gameManager.OccupancyMap);
        }

        Assert.IsTrue(_gameManager.Gate.Locked, "Restart re-locks the gate");
        Assert.IsFalse(gate.IsOpen, "The gate view re-closes on restart");
        Assert.AreEqual(Quaternion.identity, gate.CrossbarPivot.localRotation, "The crossbar is down again");
        yield break;
    }

    [UnityTest]
    public IEnumerator LevelWithoutBarrier_KeepsTheGateOpen()
    {
        _gameManager.InitializeLevel(new LevelData
        {
            exitTiles = new[] { new Vector2Int(7, 0) }
        });
        var gate = SpawnGateView();

        yield return null;

        Assert.IsFalse(_gameManager.Gate.Locked, "A barrier-less level gates nothing");
        Assert.IsTrue(gate.IsOpen, "The gate view renders open when no barrier locks it");
        yield break;
    }
}
