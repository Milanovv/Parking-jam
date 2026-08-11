using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Playing,
    Won
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridController _gridController;
    [SerializeField] private InputHandler _inputHandler;
    [SerializeField] private LevelData _levelData;

    private readonly List<Vehicle> _vehicles = new();
    private readonly Dictionary<Vehicle, Vector3Int> _initialPositions = new();
    private IReadOnlyCollection<Vector3Int> _exitTiles = new List<Vector3Int>();
    private MoveResolver _moveResolver;
    private GateState _gate;
    private Barrier _barrier;
    private BarrierGate _barrierGateView;

    public static GameManager Instance { get; private set; }

    public OccupancyMap OccupancyMap { get; private set; }
    public GameState State { get; private set; }
    public MoveResolver Resolver => _moveResolver;
    public int Tick => _moveResolver?.Tick ?? 0;
    public int UndoBalance => _moveResolver?.UndoBalance ?? 0;
    public GateState Gate => _gate;

    public event Action Cleared;

    public CubicBezier BuildExitCurve(Vector2 start, Vector2 end)
    {
        return ExitCurveFactory.FromLevelData(_levelData, start, end);
    }

    private int _pendingExits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        OccupancyMap = new OccupancyMap();
        int authoredUndos = _levelData != null ? _levelData.levelUndos : 3;
        _moveResolver = new MoveResolver(authoredUndos);
        _gate = new GateState();
        if (_gate.Locked) _gate.Unlock();
        State = GameState.Playing;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (!_initialized) InitializeLevel(_levelData);
        PlaceVehiclesOnMap();
    }

    private bool _initialized;

    public void InitializeLevel(LevelData levelData)
    {
        _initialized = true;
        _levelData = levelData;
        _exitTiles = ToGridTiles(levelData?.exitTiles);
        OccupancyMap.Clear();
        _gate = new GateState();
        SpawnBarrier();
        SyncBarrierVisual();
    }

    private static IReadOnlyCollection<Vector3Int> ToGridTiles(Vector2Int[] tiles)
    {
        var gridTiles = new List<Vector3Int>();
        if (tiles != null)
        {
            foreach (var tile in tiles)
                gridTiles.Add(new Vector3Int(tile.x, tile.y, 0));
        }
        return gridTiles;
    }

    private void SpawnBarrier()
    {
        if (_levelData?.barriers == null || _levelData.barriers.Length == 0)
        {
            _barrier = null;
            if (_gate.Locked) _gate.Unlock();
            return;
        }
        var data = _levelData.barriers[0];
        _barrier = new Barrier(new Vector3Int(data.tile.x, data.tile.y, 0));
        OccupancyMap.Place(_barrier);
    }

    public void UnlockBarrier()
    {
        if (_barrier == null) return;
        _gate.Unlock();
        OccupancyMap.Remove(_barrier);
        SyncBarrierVisual();
    }

    public void RequestBarrierUnlock() => _gate?.RequestUnlock();

    public void RegisterBarrierGate(BarrierGate gate)
    {
        _barrierGateView = gate;
        SyncBarrierVisual();
    }

    public void UnregisterBarrierGate(BarrierGate gate)
    {
        if (_barrierGateView == gate) _barrierGateView = null;
    }

    private void SyncBarrierVisual()
    {
        if (_barrierGateView == null || _gate == null) return;
        _barrierGateView.SetLocked(_gate.Locked);
    }

    private void PlaceVehiclesOnMap()
    {
        var vehicles = FindObjectsByType<Vehicle>();
        foreach (var vehicle in vehicles)
        {
            RegisterVehicleOnMap(vehicle);
        }
    }

    public void RegisterVehicleOnMap(Vehicle vehicle)
    {
        OccupancyMap.Place(vehicle);
        if (!_initialPositions.ContainsKey(vehicle))
        {
            _initialPositions[vehicle] = vehicle.GridPosition;
            _vehicles.Add(vehicle);
        }
    }

    public void UnregisterVehicleOnMap(Vehicle vehicle)
    {
        OccupancyMap.Remove(vehicle);
        _initialPositions.Remove(vehicle);
        _vehicles.Remove(vehicle);
        _pendingExits++;
    }

    public void CompleteExit()
    {
        _pendingExits--;
        if (State == GameState.Playing && _vehicles.Count == 0 && _pendingExits == 0)
            FireClear();
    }

    private void FireClear()
    {
        State = GameState.Won;
        ConfettiEffect.Spawn(BoardCenterWorld());
        Cleared?.Invoke();
    }

    private Vector3 BoardCenterWorld()
    {
        if (_gridController == null) return Vector3.zero;
        return _gridController.CellToWorld(new Vector3Int(_gridController.GridWidth / 2, _gridController.GridHeight / 2, 0));
    }

    public MoveOutcome ResolveMove(Vehicle vehicle, Vector3Int direction)
    {
        if (_gridController == null)
            _gridController = FindObjectOfType<GridController>();
        if (_gridController == null) return null;

        var mover = new Mover
        {
            Position = vehicle.GridPosition,
            Orientation = vehicle.Orientation,
            Length = vehicle.OccupiedTiles.Length
        };
        Vector2Int gridSize = new(_gridController.GridWidth, _gridController.GridHeight);
        var request = new MoveRequest
        {
            Mover = mover,
            Direction = direction,
            GridSize = gridSize,
            ExitTiles = _exitTiles,
            Gate = _gate
        };
        MoveOutcome outcome = _moveResolver.Resolve(OccupancyMap, request);
        if (outcome.Kind == MoveOutcomeKind.Restarted)
            RestartLevel();
        return outcome;
    }

    private void RestartLevel()
    {
        _pendingExits = 0;
        foreach (var vehicle in _vehicles)
        {
            var movement = vehicle.GetComponent<VehicleMovement>();
            if (movement != null) movement.StopAnimation();

            vehicle.MoveTo(_initialPositions[vehicle]);
            vehicle.transform.position = _gridController.CellToWorld(vehicle.GridPosition);
        }

        OccupancyMap.Clear();
        _gate = new GateState();
        SpawnBarrier();
        foreach (var vehicle in _vehicles)
            OccupancyMap.Place(vehicle);
        SyncBarrierVisual();
    }
}
