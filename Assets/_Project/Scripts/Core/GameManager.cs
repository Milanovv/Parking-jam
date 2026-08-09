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
    private MoveResolver _moveResolver;

    public static GameManager Instance { get; private set; }

    public OccupancyMap OccupancyMap { get; private set; }
    public GameState State { get; private set; }
    public MoveResolver Resolver => _moveResolver;
    public int Tick => _moveResolver?.Tick ?? 0;
    public int UndoBalance => _moveResolver?.UndoBalance ?? 0;

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
        State = GameState.Playing;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        PlaceVehiclesOnMap();
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
        MoveOutcome outcome = _moveResolver.Resolve(OccupancyMap, mover, direction, gridSize);
        if (outcome.Kind == MoveOutcomeKind.Restarted)
            RestartLevel();
        return outcome;
    }

    private void RestartLevel()
    {
        foreach (var vehicle in _vehicles)
        {
            var movement = vehicle.GetComponent<VehicleMovement>();
            if (movement != null) movement.StopAnimation();

            vehicle.MoveTo(_initialPositions[vehicle]);
            vehicle.transform.position = _gridController.CellToWorld(vehicle.GridPosition);
        }

        OccupancyMap.Clear();
        foreach (var vehicle in _vehicles)
            OccupancyMap.Place(vehicle);
    }
}
